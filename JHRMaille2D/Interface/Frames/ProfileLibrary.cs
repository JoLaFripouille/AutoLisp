using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace JhrMailleUi.Frames;

internal static class ProfileLibrary
{
    internal static string Cache=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"JHR","Cadres","profiles.json");
    internal static string Sources=>Path.Combine(Path.GetDirectoryName(Cache)!,"jansen-directory.txt");
    internal static List<FrameChoice> Choices(){var choices=Load().Select(p=>new FrameChoice(p.Name,p.Family,p)).ToList();if(File.Exists(Sources)&&Directory.Exists(File.ReadAllText(Sources)))choices.AddRange(Discover(File.ReadAllText(Sources),false));return choices;}
    internal static List<FrameChoice> Discover(string path,bool remember=true)
    {
        if(!Directory.Exists(path))throw new ArgumentException("Bibliothèque introuvable.");string root=Directory.Exists(Path.Combine(path,"Art_Bibl"))?Path.Combine(path,"Art_Bibl"):path;
        var choices=Directory.EnumerateFiles(root,"*.dwg",SearchOption.AllDirectories).Where(f=>!Path.GetRelativePath(root,f).Split(Path.DirectorySeparatorChar).Any(p=>p.Equals("BESCHLAG",StringComparison.OrdinalIgnoreCase)||p.Equals("DIVE",StringComparison.OrdinalIgnoreCase))).Select(f=>new FrameChoice(Path.GetRelativePath(root,f),"Jansen",null,f)).OrderBy(c=>c.Name).ToList();
        if(remember){Directory.CreateDirectory(Path.GetDirectoryName(Sources)!);File.WriteAllText(Sources,path);}return choices;
    }
    internal static List<FrameProfile> Load(){try{return (JsonSerializer.Deserialize<List<FrameProfile>>(File.ReadAllText(Cache),FrameDesign.Json)??[]).Where(MatchesName).ToList();}catch{return [];}}
    internal static bool MatchesName(FrameProfile profile)
    {
        if(profile.Family=="Jansen")return true;string state=profile.Name.Split('/').Last().Trim();var size=System.Text.RegularExpressions.Regex.Match(state,@"(\d+(?:[.,]\d+)?)\s*x\s*(\d+(?:[.,]\d+)?)",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        double Number(string s)=>double.Parse(s.Replace(',','.'),System.Globalization.CultureInfo.InvariantCulture);
        if(size.Success){double a=Number(size.Groups[1].Value),b=Number(size.Groups[2].Value);return Math.Abs(profile.Width-a)<.05&&Math.Abs(profile.Depth-b)<.05||Math.Abs(profile.Width-b)<.05&&Math.Abs(profile.Depth-a)<.05;}
        var height=System.Text.RegularExpressions.Regex.Match(state,@"\d+");return !height.Success||Math.Abs(Math.Max(profile.Width,profile.Depth)-Number(height.Value))<.05;
    }
    internal static void Save(IEnumerable<FrameProfile> values){var all=Load().ToDictionary(p=>p.Id);foreach(var p in values){foreach(var old in all.Values.Where(v=>v.Family==p.Family&&v.Name==p.Name).Select(v=>v.Id).ToArray())all.Remove(old);all[p.Id]=p;}Directory.CreateDirectory(Path.GetDirectoryName(Cache)!);string tmp=Cache+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(all.Values.OrderBy(p=>p.Family).ThenBy(p=>p.Name),FrameDesign.Json));File.Move(tmp,Cache,true);}
    static FrameProfile Make(string family,string name,string source,List<FramePath> paths)
    {
        if(paths.Count==0)throw new ArgumentException("Aucune géométrie de section trouvée.");var pts=paths.SelectMany(p=>p.Sample()).ToArray();double x=pts.Min(p=>p.X),y=pts.Min(p=>p.Y),w=pts.Max(p=>p.X)-x,h=pts.Max(p=>p.Y)-y;
        if(w<.1||h<.1||w>600||h>600||paths.Count>3000)throw new ArgumentException("Ce fichier ne semble pas contenir une section de profil isolée ("+w.ToString("0.#")+" × "+h.ToString("0.#")+").");
        paths=paths.Select(p=>p.Map(q=>new(q.X-x,q.Y-y))).ToList();
        string id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(family+"|"+name+"|"+JsonSerializer.Serialize(paths,FrameDesign.Json))))[..20];var profile=new FrameProfile(id,name,family,paths.ToArray(),source);if(!MatchesName(profile))throw new ArgumentException("L’état de visibilité ne correspond pas aux dimensions de la section.");return profile;
    }
    internal static void Paths(Entity entity,List<FramePath> paths,int depth=0)
    {
        if(depth>8||!entity.Visible)return;if(entity.Layer.Contains("AXE",StringComparison.OrdinalIgnoreCase)||entity.Linetype.Contains("CENTER",StringComparison.OrdinalIgnoreCase))return;
        P2 Point(Point3d p)=>new(p.X,p.Y);
        switch(entity){
            case Line l:paths.Add(new([Point(l.StartPoint),Point(l.EndPoint)]));break;
            case Polyline p when p.NumberOfVertices>1:paths.Add(new(Enumerable.Range(0,p.NumberOfVertices).Select(i=>new P2(p.GetPoint2dAt(i).X,p.GetPoint2dAt(i).Y)).ToArray(),Enumerable.Range(0,p.NumberOfVertices).Select(p.GetBulgeAt).ToArray(),p.Closed));break;
            case Circle c:double r=c.Radius;double b=Math.Tan(Math.PI/8);paths.Add(new([new(c.Center.X+r,c.Center.Y),new(c.Center.X,c.Center.Y+r),new(c.Center.X-r,c.Center.Y),new(c.Center.X,c.Center.Y-r)],[b,b,b,b],true));break;
            case Arc a:double sweep=a.EndAngle-a.StartAngle;if(sweep<=0)sweep+=2*Math.PI;int steps=Math.Max(1,(int)Math.Ceiling(sweep/Math.PI));var points=Enumerable.Range(0,steps+1).Select(i=>Point(a.GetPointAtParameter(a.StartParam+(a.EndParam-a.StartParam)*i/steps))).ToArray();paths.Add(new(points,Enumerable.Repeat(Math.Tan(sweep/steps/4)*(a.Normal.Z<0?-1:1),steps).Append(0).ToArray()));break;
            case BlockReference block:var exploded=new DBObjectCollection();block.Explode(exploded);foreach(DBObject item in exploded){using(item)if(item is Entity child)Paths(child,paths,depth+1);}break;
            case Ellipse e:var samples=Enumerable.Range(0,129).Select(i=>Point(e.GetPointAtParameter(e.StartParam+(e.EndParam-e.StartParam)*i/128))).ToArray();paths.Add(new(samples,null,e.Closed));break;
            case Polyline2d p2:case Polyline3d p3:var pieces=new DBObjectCollection();entity.Explode(pieces);foreach(DBObject item in pieces){using(item)if(item is Entity child)Paths(child,paths,depth+1);}break;
        }
    }
    internal static FrameProfile ImportFile(string path,string family="Jansen")
    {
        using var db=new Database(false,true);db.ReadDwgFile(path,FileOpenMode.OpenForReadAndAllShare,true,"");db.CloseInput(true);using var tx=db.TransactionManager.StartTransaction();var model=(BlockTableRecord)tx.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);var paths=new List<FramePath>();
        foreach(ObjectId id in model)if(tx.GetObject(id,OpenMode.ForRead) is Entity e)Paths(e,paths);return Make(family,Path.GetFileNameWithoutExtension(path),path,paths);
    }
    internal static List<FrameProfile> ImportTemplate(string path)
    {
        using var db=new Database(false,true);db.ReadDwgFile(path,FileOpenMode.OpenForReadAndAllShare,true,"");db.CloseInput(true);var result=new List<FrameProfile>();
        using var tx=db.TransactionManager.StartTransaction();var blocks=(BlockTable)tx.GetObject(db.BlockTableId,OpenMode.ForRead);var model=(BlockTableRecord)tx.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
        string[] known=["Tube soudé","Tubes soudés","Tubes soudés.","Cornière égale","Cornière inégale","Té égal","Poutrelles UPN","UPE","IPE","Poutrelles IPN","Poutrelles HEA","Poutrelles HEB","TUBE L"];
        foreach(var name in known){if(!blocks.Has(name))continue;ObjectId definition=blocks[name];using var probe=new BlockReference(Point3d.Origin,definition);model.AppendEntity(probe);tx.AddNewlyCreatedDBObject(probe,true);
            var property=probe.IsDynamicBlock?probe.DynamicBlockReferencePropertyCollection.Cast<DynamicBlockReferenceProperty>().Where(p=>!p.ReadOnly&&p.GetAllowedValues().Length>1&&p.GetAllowedValues().All(v=>v is string)).OrderByDescending(p=>p.GetAllowedValues().Length).FirstOrDefault():null;
            object[] states=property?.GetAllowedValues()??[""];
            foreach(object state in states){using var reference=new BlockReference(Point3d.Origin,definition);model.AppendEntity(reference);tx.AddNewlyCreatedDBObject(reference,true);
                if(property!=null){var target=reference.DynamicBlockReferencePropertyCollection.Cast<DynamicBlockReferenceProperty>().Single(p=>p.PropertyName==property.PropertyName);target.Value=state;reference.RecordGraphicsModified(true);}
                var paths=new List<FramePath>();Paths(reference,paths);try{result.Add(Make(name.StartsWith("Tube",StringComparison.OrdinalIgnoreCase)?"Tubes":"Laminés",name+" / "+state,path,paths));}catch(ArgumentException){}reference.Erase();
            }probe.Erase();
        }
        // The source database is discarded; no change is written to the template.
        if(result.Count==0)throw new ArgumentException("Aucune section connue trouvée dans ce gabarit.");return result;
    }
}
