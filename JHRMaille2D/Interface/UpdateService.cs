using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace JhrMailleUi;

internal sealed record UpdateRelease(int Schema,string Version,string Package,string Sha256,long Size,string AutoCadSeries,string Notes)
{
    internal System.Version ParsedVersion=>System.Version.Parse(Version);
}

/// <summary>Only the published JHR bundle is fetched. No drawings, handles or settings leave the PC.</summary>
internal sealed class UpdateService : IDisposable
{
    internal const string Feed="https://raw.githubusercontent.com/JoLaFripouille/AutoLisp/main/JHRMaille2D/Updates/latest.json";
    const string PackageRoot="https://raw.githubusercontent.com/JoLaFripouille/AutoLisp/main/JHRMaille2D/Updates/";
    const long Limit=20*1024*1024;
    internal static Version CurrentVersion=>typeof(UpdateService).Assembly.GetName().Version??new Version(1,4,4);
    static readonly SemaphoreSlim InstallLock=new(1,1);
    readonly HttpClient client;readonly bool ownsClient;readonly string destination;
    internal UpdateService(HttpClient? transport=null,string? bundlePath=null)
    {
        ownsClient=transport==null;client=transport??new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(30)};
        if(!client.DefaultRequestHeaders.UserAgent.Any())client.DefaultRequestHeaders.UserAgent.ParseAdd("JHR-Panneaux/"+CurrentVersion.ToString(3));
        destination=bundlePath??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Autodesk","ApplicationPlugins","JHR.Panneaux.bundle");
    }
    public void Dispose(){if(ownsClient)client.Dispose();}
    internal async Task<UpdateRelease> CheckAsync(CancellationToken token=default)
    {
        var bytes=await DownloadAsync(new Uri(Feed),64*1024,token);
        var release=JsonSerializer.Deserialize<UpdateRelease>(bytes,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidDataException("Annonce de version vide.");
        Validate(release);return release;
    }
    internal static void Validate(UpdateRelease r)
    {
        if(r.Schema!=1||!Version.TryParse(r.Version,out var v)||v.Major!=1||v.Minor<0||v.Build<0||v.Revision>0||r.AutoCadSeries!="R26.0")throw new InvalidDataException("Cette version n’est pas compatible avec ce plugin AutoCAD 2027.");
        if(r.Package!="JHR.Panneaux-"+v.ToString(3)+".zip"||r.Sha256?.Length!=64||!r.Sha256.All(Uri.IsHexDigit)||r.Size<=0||r.Size>Limit||r.Notes==null||r.Notes.Length>4000)throw new InvalidDataException("Annonce de mise à jour invalide.");
    }
    async Task<byte[]> DownloadAsync(Uri uri,long maximum,CancellationToken token)
    {
        using var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>maximum)throw new InvalidDataException("Téléchargement trop volumineux.");
        await using var stream=await response.Content.ReadAsStreamAsync(token);using var memory=new MemoryStream();byte[] buffer=new byte[8192];int count;
        while((count=await stream.ReadAsync(buffer,token))>0){if(memory.Length+count>maximum)throw new InvalidDataException("Téléchargement trop volumineux.");await memory.WriteAsync(buffer.AsMemory(0,count),token);}
        return memory.ToArray();
    }
    internal Version? InstalledVersion()
    {
        try{return Version.Parse(XDocument.Load(Path.Combine(destination,"PackageContents.xml")).Root!.Attribute("AppVersion")!.Value);}catch{return null;}
    }
    internal async Task<string> InstallAsync(UpdateRelease release,CancellationToken token=default)
    {
        Validate(release);if(release.ParsedVersion<=CurrentVersion)throw new InvalidOperationException("Cette version n’est pas plus récente que la version chargée.");
        await InstallLock.WaitAsync(token);
        try{
            var installed=InstalledVersion();if(installed!=null&&installed>=release.ParsedVersion)return "Version déjà installée. Elle sera chargée au prochain démarrage d’AutoCAD.";
            byte[] archive=await DownloadAsync(new Uri(PackageRoot+release.Package),Limit,token);
            if(archive.LongLength!=release.Size||!Convert.ToHexString(SHA256.HashData(archive)).Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Le fichier téléchargé ne correspond pas à la version annoncée. Installation annulée.");
            return await Task.Run(()=>InstallPackage(archive,release,token),token);
        }finally{InstallLock.Release();}
    }
    internal string InstallPackage(byte[] archive,UpdateRelease release,CancellationToken token)
    {
        Validate(release);
        string module="JhrMaillePlugin_v"+release.ParsedVersion.ToString(3).Replace('.','_')+"_final.dll";
        string xmlName="JHR.Panneaux.bundle/PackageContents.xml",dllName="JHR.Panneaux.bundle/Contents/Windows/"+module;
        using var memory=new MemoryStream(archive);using var zip=new ZipArchive(memory,ZipArchiveMode.Read);
        // Exact allow-list also rejects path traversal, extra executables and duplicate entries.
        if(zip.Entries.Count!=2||zip.Entries.Count(e=>e.FullName==xmlName)!=1||zip.Entries.Count(e=>e.FullName==dllName)!=1||zip.Entries.Any(e=>e.Length>Limit)||zip.Entries.Sum(e=>e.Length)>Limit)throw new InvalidDataException("Le paquet contient des fichiers inattendus.");
        byte[] ReadEntry(string name){using var source=zip.GetEntry(name)!.Open();using var output=new MemoryStream();source.CopyTo(output);if(output.Length>Limit)throw new InvalidDataException("Paquet trop volumineux.");return output.ToArray();}
        byte[] xml=ReadEntry(xmlName),dll=ReadEntry(dllName);if(xml.Length>64*1024)throw new InvalidDataException("Descriptif de paquet trop volumineux.");
        using var reader=System.Xml.XmlReader.Create(new MemoryStream(xml),new System.Xml.XmlReaderSettings{DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=64*1024});
        var document=XDocument.Load(reader);var root=document.Root;var entries=root?.Descendants("ComponentEntry").ToArray()??[];
        if(root?.Name!="ApplicationPackage"||root.Attribute("Name")?.Value!="JHR Panneaux"||root.Attribute("AppVersion")?.Value!=release.ParsedVersion.ToString(3)||entries.Length!=1||entries[0].Attribute("ModuleName")?.Value!="./Contents/Windows/"+module||entries[0].Attribute("Version")?.Value!=release.ParsedVersion.ToString(3))throw new InvalidDataException("Le descriptif du plugin est incohérent.");
        var requirements=root.Descendants("RuntimeRequirements").ToArray();
        if(requirements.Length==0||requirements.Any(r=>r.Attribute("SeriesMin")?.Value!="R26.0"||r.Attribute("SeriesMax")?.Value!="R26.0"||r.Attribute("OS")?.Value!="Win64"||r.Attribute("Platform")?.Value!="AutoCAD*"))throw new InvalidDataException("Le paquet est prévu pour une autre version d’AutoCAD.");
        string stage=Path.Combine(Path.GetTempPath(),"JHR-Panneaux-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        string manifest=Path.Combine(destination,"PackageContents.xml"),dllPath=Path.Combine(destination,"Contents","Windows",module),tempManifest=Path.Combine(destination,"PackageContents."+Guid.NewGuid().ToString("N")+".tmp");bool newDll=false;
        try{
            string testDll=Path.Combine(stage,module);File.WriteAllBytes(testDll,dll);var identity=AssemblyName.GetAssemblyName(testDll);
            if(identity.Name!=Path.GetFileNameWithoutExtension(module)||identity.Version!=new Version(release.ParsedVersion.Major,release.ParsedVersion.Minor,release.ParsedVersion.Build,0))throw new InvalidDataException("La bibliothèque ne correspond pas à la version annoncée.");
            token.ThrowIfCancellationRequested();
            if(Directory.Exists(destination)){
                var backup=Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(destination))!,"JHR-Panneaux-Backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
                foreach(string file in Directory.EnumerateFiles(destination,"*",SearchOption.AllDirectories)){string target=Path.Combine(backup,Path.GetRelativePath(destination,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);}
            }
            token.ThrowIfCancellationRequested();Directory.CreateDirectory(Path.GetDirectoryName(dllPath)!);
            // The current DLL is never overwritten or unloaded. The manifest switches last.
            if(File.Exists(dllPath)){if(!SHA256.HashData(File.ReadAllBytes(dllPath)).SequenceEqual(SHA256.HashData(dll)))throw new InvalidDataException("Une bibliothèque différente porte déjà ce nom. Installation annulée.");}
            else{File.Copy(testDll,dllPath);newDll=true;}
            File.WriteAllBytes(tempManifest,xml);File.Move(tempManifest,manifest,true);
            return "Version "+release.ParsedVersion.ToString(3)+" installée. Enregistre tes plans puis redémarre AutoCAD pour l’utiliser.";
        }catch{if(newDll&&File.Exists(dllPath))File.Delete(dllPath);throw;}
        finally{if(File.Exists(tempManifest))File.Delete(tempManifest);Directory.Delete(stage,true);}
    }
}
