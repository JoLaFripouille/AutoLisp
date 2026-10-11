using System.Text.Json;
using System.Text.Json.Nodes;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
namespace JhrMailleUi;
public static class PanelApi
{
    static JsonObject Object(JsonNode? n)=>n as JsonObject??throw new ArgumentException("Objet JSON attendu.");
    static void Merge(JsonObject destination,JsonObject patch){foreach(var p in patch){if(p.Value is JsonObject child && destination[p.Key] is JsonObject next)Merge(next,child);else {if(!destination.ContainsKey(p.Key))throw new ArgumentException("Paramètre inconnu : "+p.Key);destination[p.Key]=p.Value?.DeepClone();}}}
    static string Handle(JsonObject r)=>r["handle"]?.GetValue<string>()??throw new ArgumentException("Handle du panneau attendu.");
    static string Revision(BlockReference b,PanelData data)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{data,definition=b.BlockTableRecord.Handle.ToString(),transform=b.BlockTransform.ToArray(),b.Layer},PanelData.Json))))[..16];
    internal static object Brief(BlockReference b,PanelData data,Transaction tx)=>new{handle=b.Handle.ToString(),name=((BlockTableRecord)tx.GetObject(b.BlockTableRecord,OpenMode.ForRead)).Name,tool=(int)data.Settings.Tool,layer=b.Layer,engine=data.Engine,revision=Revision(b,data)};
    // A single call does a full transaction and returns compact JSON. No GUI or Lisp engine.
    public static string Execute(string request)
    {
        try{
            if(request.Length>2_000_000)throw new ArgumentException("Requête trop volumineuse.");var r=Object(JsonNode.Parse(request));string op=r["op"]?.GetValue<string>()??"help";
            var doc=AcApp.DocumentManager.MdiActiveDocument??throw new ArgumentException("Aucun dessin actif.");var db=doc.Database;
            if(r["drawing"] is JsonValue expected&&!string.Equals(expected.GetValue<string>(),doc.Name,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Le dessin actif ne correspond pas à la requête.");
            if(PanelEditor.Active!=null&&op is "create" or "update")throw new ArgumentException("Fermer l’éditeur de panneau avant une modification par API.");
            object payload;
            using(doc.LockDocument())switch(op){
            case "batch":{var commands=r["requests"] as JsonArray??throw new ArgumentException("Liste de requêtes attendue.");if(commands.Count>20||commands.Any(x=>x?["op"]?.GetValue<string>()=="batch"))throw new ArgumentException("20 opérations maximum, sans batch imbriqué.");using var transaction=db.TransactionManager.StartTransaction();var outputs=new List<JsonNode?>();foreach(var command in commands){var output=JsonNode.Parse(Execute(command!.ToJsonString()));outputs.Add(output);if(output?["ok"]?.GetValue<bool>()!=true)throw new ArgumentException("Batch annulé : "+output?["error"]?.GetValue<string>());}transaction.Commit();payload=outputs;break;}
            case "help":payload=new{version="1.4.4",schema=1,tools=new[]{"0=filet inox losange","1=grillage carré","2=métal déployé","3=tôle perforée"},operations=new[]{"list","get","create","update","check","catalogue","batch"},defaults=new FillSettings(),units="mm",sourceContours="Polylignes fermées XY ; handles ou sommets locaux.",readOnly=new[]{"help","list","get","check","catalogue"}};break;
            case "catalogue":{var q=JakobCatalogue.Items.AsEnumerable();if(r["family"]!=null)q=q.Where(p=>p.Family==r["family"]!.GetValue<string>());if(r["diameter"]!=null)q=q.Where(p=>p.Wire==r["diameter"]!.GetValue<double>());int offset=Math.Max(0,r["offset"]?.GetValue<int>()??0),limit=Math.Clamp(r["limit"]?.GetValue<int>()??50,1,200);var rows=q.ToArray();payload=new{total=rows.Length,offset,items=rows.Skip(offset).Take(limit).Select(p=>new{p.Reference,p.Family,p.Wire,p.Ml,p.Mw,p.Woven})};break;}
            case "list":{using var tx=db.TransactionManager.StartTransaction();var space=(BlockTableRecord)tx.GetObject(db.CurrentSpaceId,OpenMode.ForRead);int offset=Math.Max(0,r["offset"]?.GetValue<int>()??0),limit=Math.Clamp(r["limit"]?.GetValue<int>()??50,1,200);var rows=new List<object>();int total=0;foreach(ObjectId id in space){if(tx.GetObject(id,OpenMode.ForRead) is BlockReference b&&PanelStore.IsPanel(b,tx)){if(total>=offset&&rows.Count<limit)rows.Add(Brief(b,PanelStore.Read(b,tx),tx));total++;}}payload=new{total,offset,items=rows};break;}
            case "get":{using var tx=db.TransactionManager.StartTransaction();var b=tx.GetObject(PanelStore.Resolve(db,Handle(r)),OpenMode.ForRead) as BlockReference??throw new ArgumentException("Bloc attendu.");var data=PanelStore.Read(b,tx);payload=r["contours"]?.GetValue<bool>()==true?new{panel=Brief(b,data,tx),data}:new{panel=Brief(b,data,tx),settings=data.Settings,sourceHandles=data.SourceHandles,hasOuter=data.Outer!=null,data.LegacyDefaults};break;}
            case "create":case "check":case "update":{
                PanelData data;Matrix3d matrix=Matrix3d.Identity;ObjectId target=ObjectId.Null;
                if(op=="update" || r["handle"]!=null){target=PanelStore.Resolve(db,Handle(r));using var tx=db.TransactionManager.StartTransaction();var b=tx.GetObject(target,OpenMode.ForRead) as BlockReference??throw new ArgumentException("Bloc attendu.");data=PanelStore.Read(b,tx);if(r["expectedRevision"]!=null&&r["expectedRevision"]!.GetValue<string>()!=Revision(b,data))throw new ArgumentException("Le panneau a changé depuis sa lecture. Relire ses paramètres.");matrix=b.BlockTransform;if(r["expectedEngine"]!=null&&r["expectedEngine"]!.GetValue<string>()!=data.Engine)throw new ArgumentException("Le panneau a changé de version.");}
                else if(r["innerHandle"]!=null){var inside=PanelStore.Resolve(db,r["innerHandle"]!.GetValue<string>());var outside=r["outerHandle"]==null?ObjectId.Null:PanelStore.Resolve(db,r["outerHandle"]!.GetValue<string>());(data,matrix)=PanelStore.FromContours(db,inside,outside,new());}
                else {data=r["data"]?.Deserialize<PanelData>(PanelData.Json)??throw new ArgumentException("Fournir les handles de contours ou data.inner/data.outer.");if(r["position"]!=null){var p=r["position"]!.Deserialize<double[]>()!;if(p.Length!=3||p.Any(v=>!double.IsFinite(v)))throw new ArgumentException("Position XYZ attendue.");matrix=Matrix3d.Displacement(new(p[0],p[1],p[2]));}}
                if(r["settings"] is JsonObject patch){var json=Object(JsonNode.Parse(JsonSerializer.Serialize(data.Settings,PanelData.Json)));Merge(json,patch);var next=json.Deserialize<FillSettings>(PanelData.Json)!;if(patch["mesh"]?["productReference"] is JsonValue reference){string name=reference.GetValue<string>();if(name.Length>0){var product=JakobCatalogue.Find(name)??throw new ArgumentException("Référence Jakob inconnue.");next=next with{Mesh=product.Apply(next.Mesh)};}}data=data with{Settings=next};}
                if(op is "update" or "check" && !target.IsNull && r["outerHandle"] is JsonValue outerHandle){using var tx=db.TransactionManager.StartTransaction();var outer=(Polyline)tx.GetObject(PanelStore.Resolve(db,outerHandle.GetValue<string>()),OpenMode.ForRead);if(Math.Abs(outer.Elevation-matrix.CoordinateSystem3d.Origin.Z)>.01)throw new ArgumentException("Les contours doivent être coplanaires.");data=data with{Outer=PanelStore.Vertices(outer,matrix.Inverse()),SourceHandles=data.SourceHandles.Take(1).Append(outer.Handle.ToString()).ToArray()};}
                data=data with{Engine="1.4.4"};var watch=System.Diagnostics.Stopwatch.StartNew();
                if(op=="check"){var result=PanelGeometry.Generate(data);payload=new{valid=true,objects=GeometryRuntime.Items(result.Shapes).Count(),result.Cells,result.Nodes,result.Boundary,width=result.Width,height=result.Height,milliseconds=watch.ElapsedMilliseconds};}
                else{var id=PanelStore.Commit(db,data,matrix,op=="update"?target:ObjectId.Null);using var tx=db.TransactionManager.StartTransaction();var b=(BlockReference)tx.GetObject(id,OpenMode.ForRead);payload=new{panel=Brief(b,PanelStore.Read(b,tx),tx),milliseconds=watch.ElapsedMilliseconds};doc.Editor.Regen();}
                break;
            }
            default:throw new ArgumentException("Opération inconnue : "+op);
            }
            return JsonSerializer.Serialize(new{ok=true,result=payload},PanelData.Json);
        }catch(System.Exception e){return JsonSerializer.Serialize(new{ok=false,error=e.Message,type=e.GetType().Name},PanelData.Json);}
    }
}
