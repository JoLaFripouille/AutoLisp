using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JhrMailleUi;

static class UpdateTests
{
    sealed class Transport(string manifest,byte[] zip,bool offline=false):HttpMessageHandler
    {
        internal int Downloads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();if(offline)throw new HttpRequestException("Offline");
            if(request.RequestUri!.AbsoluteUri!=UpdateService.Feed)Downloads++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri.AbsoluteUri==UpdateService.Feed?new StringContent(manifest):new ByteArrayContent(zip)});
        }
    }
    static void Assert(bool valid,string label){if(!valid)throw new Exception(label);Console.WriteLine("OK update "+label);}
    static async Task Reject(Func<Task> operation,string label){bool rejected=false;try{await operation();}catch{rejected=true;}Assert(rejected,label);}
    static byte[] Zip(string xml,byte[] dll,string extra="")
    {
        using var memory=new MemoryStream();using(var archive=new ZipArchive(memory,ZipArchiveMode.Create,true)){
            using(var stream=archive.CreateEntry("JHR.Panneaux.bundle/PackageContents.xml").Open())stream.Write(Encoding.UTF8.GetBytes(xml));
            using(var stream=archive.CreateEntry("JHR.Panneaux.bundle/Contents/Windows/JhrMaillePlugin_v1_5_1_final.dll").Open())stream.Write(dll);
            if(extra!=""){using var stream=archive.CreateEntry(extra).Open();stream.WriteByte(1);}
        }return memory.ToArray();
    }
    internal static async Task Run(string output)
    {
        string source=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        byte[] dll=File.ReadAllBytes(Path.Combine(source,"dist/JhrMaillePlugin_v1_5_1_final.dll"));
        string xml=File.ReadAllText(Path.Combine(source,"JHR.Panneaux.bundle/PackageContents.xml")).Replace("1.4.4","1.5.1").Replace("v1_4_4_final","v1_5_1_final");
        byte[] package=Zip(xml,dll);var release=new UpdateRelease(1,"1.5.1","JHR.Panneaux-1.5.1.zip",Convert.ToHexString(SHA256.HashData(package)),package.Length,"R26.0","Test");
        string root=Path.Combine(output,"updater-tests-"+Guid.NewGuid().ToString("N")),bundle=Path.Combine(root,"Autodesk/ApplicationPlugins/JHR.Panneaux.bundle");Directory.CreateDirectory(bundle);
        string manifest=Path.Combine(bundle,"PackageContents.xml");File.WriteAllText(manifest,xml.Replace("1.5.1","1.4.4").Replace("v1_5_1_final","v1_4_4_final"));string old=Path.Combine(bundle,"Contents/Windows/old.dll");Directory.CreateDirectory(Path.GetDirectoryName(old)!);File.WriteAllText(old,"loaded old DLL");
        using var transport=new Transport(JsonSerializer.Serialize(release),package);using var client=new HttpClient(transport);using var service=new UpdateService(client,bundle);
        var found=await service.CheckAsync();Assert(found==release,"manifest GitHub lu");
        await Reject(()=>service.InstallAsync(release with{Sha256=new string('0',64)}),"hash incorrect refuse avant ecriture");Assert(service.InstalledVersion()==new Version(1,4,4),"ancienne installation conservee apres refus");
        foreach(string bad in new[]{"../escape.dll","JHR.Panneaux.bundle/PackageContents.xml","JHR.Panneaux.bundle/evil.exe"})await Reject(()=>Task.Run(()=>service.InstallPackage(Zip(xml,dll,bad),release,default)),"entree inattendue/dupliquee refusee "+bad);
        await Reject(()=>Task.Run(()=>service.InstallPackage(Zip(xml.Replace("./Contents/Windows/","../"),dll),release,default)),"DLL hors du bundle refusee");
        await Reject(()=>Task.Run(()=>service.InstallPackage(Zip(xml,new byte[]{1,2,3}),release,default)),"DLL invalide refusee");
        await Reject(()=>Task.Run(()=>UpdateService.Validate(release with{AutoCadSeries="R25.0"})),"mauvaise version AutoCAD refusee");
        await Reject(()=>Task.Run(()=>UpdateService.Validate(release with{Package="https://other.example/evil.zip"})),"URL de paquet arbitraire refusee");
        using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();await Reject(()=>service.InstallAsync(release,cancelled.Token),"annulation avant installation");}
        // An in-use manifest must not leave a half-installed DLL or replace the old manifest.
        using(var locked=new FileStream(manifest,FileMode.Open,FileAccess.Read,FileShare.Read))await Reject(()=>service.InstallAsync(release),"manifest verrouille : rollback");
        Assert(service.InstalledVersion()==new Version(1,4,4)&&!File.Exists(Path.Combine(bundle,"Contents/Windows/JhrMaillePlugin_v1_5_1_final.dll")),"rollback garde la version precedente");
        string message=await service.InstallAsync(release);Assert(message.Contains("redémarre")&&service.InstalledVersion()==new Version(1,5,1),"installe la nouvelle version pour le prochain demarrage");
        Assert(File.ReadAllText(old)=="loaded old DLL","bibliotheque chargee jamais remplacee");
        Assert(Directory.EnumerateFiles(Path.Combine(root,"Autodesk/JHR-Panneaux-Backups"),"PackageContents.xml",SearchOption.AllDirectories).Any(),"sauvegarde de l'ancienne installation");
        int downloads=transport.Downloads;await service.InstallAsync(release);Assert(transport.Downloads==downloads,"version deja installee sans telechargement");
        using var offTransport=new Transport("",[],true);using var offClient=new HttpClient(offTransport);using var offline=new UpdateService(offClient,bundle);await Reject(()=>offline.CheckAsync(),"hors connexion gere");
        Console.WriteLine("UPDATE TESTS DONE");
    }
}
