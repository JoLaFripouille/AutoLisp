using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
namespace JhrMailleUi.Frames;
internal static class FrameApi
{
    static ObjectId Resolve(Database db,string? handle)=>db.GetObjectId(false,new Handle(Convert.ToInt64(handle??throw new ArgumentException("Handle attendu."),16)),0);
    static string Revision(BlockReference b,FrameRecord record)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{record.Revision,definition=b.BlockTableRecord.Handle.ToString(),transform=b.BlockTransform.ToArray(),b.Layer},FrameDesign.Json))))[..20];
    static object Brief(BlockReference b,FrameRecord record,Transaction tx)=>new{handle=b.Handle.ToString(),name=((BlockTableRecord)tx.GetObject(b.BlockTableRecord,OpenMode.ForRead)).Name,revision=Revision(b,record),tool=record.Design.Tool,width=record.Design.Width,height=record.Design.Height,profileId=record.Design.Profile?.Id,profile=record.Design.Profile?.Name,cuts=record.Design.Cuts.Length,views=record.Views,layer=b.Layer};
    internal static string Execute(string request)
    {
        try{
            if(request.Length>2_000_000)throw new ArgumentException("Requête trop volumineuse.");var r=JsonNode.Parse(request) as JsonObject??throw new ArgumentException("Objet JSON attendu.");string op=r["op"]?.GetValue<string>()??"help";
            var doc=AcApp.DocumentManager.MdiActiveDocument??throw new ArgumentException("Aucun dessin actif.");var db=doc.Database;
            if(r["drawing"] is JsonValue expected&&!string.Equals(expected.GetValue<string>(),doc.Name,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Le dessin actif ne correspond pas à la requête.");
            if(op is "create" or "update"&&(FrameEditor.Active||PanelEditor.Active!=null))throw new ArgumentException("Fermer l’éditeur avant une modification par API.");
            object payload;using(doc.LockDocument())switch(op){
                case "help":payload=new{version="1.5.1",schema=1,units="mm",tools=Enum.GetNames<FrameTool>(),operations=new[]{"catalogue","importTemplate","importProfile","discover","list","get","check","create","update"},limits="Cadres rectangulaires ; ouvrants Jansen et quincaillerie non assemblés automatiquement. Coupes d’angles à 45° à déplacer hors de l’angle.",defaults=new FrameDesign()};break;
                case "catalogue":{var all=ProfileLibrary.Load().Where(p=>r["family"]==null||p.Family==r["family"]!.GetValue<string>()).Where(p=>r["query"]==null||p.Name.Contains(r["query"]!.GetValue<string>(),StringComparison.OrdinalIgnoreCase)).ToArray();int offset=Math.Max(0,r["offset"]?.GetValue<int>()??0),limit=Math.Clamp(r["limit"]?.GetValue<int>()??50,1,200);payload=new{total=all.Length,offset,items=all.Skip(offset).Take(limit).Select(p=>new{p.Id,p.Name,p.Family,width=p.Width,depth=p.Depth,paths=p.Paths.Length})};break;}
                case "importTemplate":case "importProfile":{string path=r["path"]?.GetValue<string>()??throw new ArgumentException("Chemin de bibliothèque attendu.");var values=op=="importTemplate"?ProfileLibrary.ImportTemplate(path):new List<FrameProfile>{ProfileLibrary.ImportFile(path)};ProfileLibrary.Save(values);payload=new{count=values.Count};break;}
                case "discover":{var values=ProfileLibrary.Discover(r["path"]?.GetValue<string>()??throw new ArgumentException("Dossier attendu."));payload=new{files=values.Count,mode="Lecture des sections à la demande ; fichiers du fabricant conservés localement."};break;}
                case "list":{using var tx=db.TransactionManager.StartOpenCloseTransaction();var model=(BlockTableRecord)tx.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);var all=model.Cast<ObjectId>().Select(i=>tx.GetObject(i,OpenMode.ForRead)).OfType<BlockReference>().Where(b=>FrameStore.IsFrame(b,tx)).ToArray();int offset=Math.Max(0,r["offset"]?.GetValue<int>()??0),limit=Math.Clamp(r["limit"]?.GetValue<int>()??50,1,200);payload=new{total=all.Length,offset,items=all.Skip(offset).Take(limit).Select(b=>Brief(b,FrameStore.Read(b,tx),tx)).ToArray()};break;}
                case "get":{using var tx=db.TransactionManager.StartOpenCloseTransaction();var b=tx.GetObject(Resolve(db,r["handle"]?.GetValue<string>()),OpenMode.ForRead) as BlockReference??throw new ArgumentException("Cadre attendu.");var record=FrameStore.Read(b,tx);var settings=JsonSerializer.SerializeToNode(record.Design.Portable(),FrameDesign.Json)!.AsObject();settings.Remove("profile");settings.Remove("leafProfile");settings["profileId"]=record.Design.Profile?.Id;settings["leafProfileId"]=record.Design.LeafProfile?.Id;payload=new{frame=Brief(b,record,tx),settings};break;}
                case "check":case "create":case "update":{
                    FrameDesign design=new();FrameRecord? old=null;Matrix3d transform=Matrix3d.Identity;ObjectId target=ObjectId.Null;
                    if(r["handle"]!=null){target=Resolve(db,r["handle"]!.GetValue<string>());using var tx=db.TransactionManager.StartOpenCloseTransaction();var b=tx.GetObject(target,OpenMode.ForRead) as BlockReference??throw new ArgumentException("Cadre attendu.");FrameEditor.TransformCheck(b);old=FrameStore.Read(b,tx);design=old.Design;transform=b.BlockTransform;if(r["expectedRevision"]!=null&&r["expectedRevision"]!.GetValue<string>()!=Revision(b,old))throw new ArgumentException("Le cadre a changé depuis sa lecture. Relire sa révision.");}
                    if(op=="update"&&(target.IsNull||r["expectedRevision"]==null))throw new ArgumentException("Une mise à jour requiert handle et expectedRevision.");
                    if(op=="create"&&r["drawing"]==null)throw new ArgumentException("Créer requiert le nom exact du dessin : drawing.");
                    var json=JsonSerializer.SerializeToNode(design,FrameDesign.Json)!.AsObject();if(r["settings"] is JsonObject settings)foreach(var pair in settings){if(pair.Key is "profileId" or "leafProfileId"){string? id=pair.Value?.GetValue<string>();FrameProfile? p=id==null?null:ProfileLibrary.Load().FirstOrDefault(p=>p.Id==id)??(id==design.Profile?.Id?design.Profile:id==design.LeafProfile?.Id?design.LeafProfile:null)??throw new ArgumentException("Section inconnue : importer le gabarit ou le DWG.");json[pair.Key=="profileId"?"profile":"leafProfile"]=JsonSerializer.SerializeToNode(p,FrameDesign.Json);}else{if(!json.ContainsKey(pair.Key)||pair.Key is "profile" or "leafProfile" or "schema")throw new ArgumentException("Paramètre inconnu ou protégé : "+pair.Key);json[pair.Key]=pair.Value?.DeepClone();}}
                    design=json.Deserialize<FrameDesign>(FrameDesign.Json)!;string? error=design.Validate();if(error!=null)throw new ArgumentException(error);var elevation=FrameGeometry.Elevation(design);var sections=design.Cuts.Select(c=>FrameGeometry.Section(design,c)).ToArray();
                    if(op=="check"){payload=new{valid=true,elevationPaths=elevation.Length,sectionPaths=sections.Select(p=>p.Length).ToArray()};break;}
                    if(op=="create"&&r["position"]!=null){double[] p=r["position"]!.Deserialize<double[]>()!;if(p.Length!=3||p.Any(x=>!double.IsFinite(x)))throw new ArgumentException("Position XYZ invalide.");transform=Matrix3d.Displacement(new(p[0],p[1],p[2]));}
                    var result=FrameStore.Commit(db,design,transform,op=="update"?target:ObjectId.Null,old?.Revision);using(var tx=db.TransactionManager.StartOpenCloseTransaction()){var b=(BlockReference)tx.GetObject(result,OpenMode.ForRead);payload=Brief(b,FrameStore.Read(b,tx),tx);}doc.Editor.Regen();break;
                }
                default:throw new ArgumentException("Opération inconnue : "+op);
            }
            return JsonSerializer.Serialize(new{ok=true,result=payload},FrameDesign.Json);
        }catch(Exception e){return JsonSerializer.Serialize(new{ok=false,error=e.Message},FrameDesign.Json);}
    }
}
