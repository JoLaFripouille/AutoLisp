using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace JhrMailleUi.Frames;
internal sealed record FrameRecord(FrameDesign Design,string Revision,Dictionary<string,string> Views);
internal static class FrameStore
{
    const string Key="JHR_FRAME_V1",Owner="JHR_FRAME_OWNER";
    internal static bool IsFrame(BlockReference reference,Transaction tx)=>!reference.ExtensionDictionary.IsNull&&((DBDictionary)tx.GetObject(reference.ExtensionDictionary,OpenMode.ForRead)).Contains(Key);
    static string? Text(Entity e,Transaction tx,string key){if(e.ExtensionDictionary.IsNull)return null;var dict=(DBDictionary)tx.GetObject(e.ExtensionDictionary,OpenMode.ForRead);if(!dict.Contains(key))return null;return string.Concat(((Xrecord)tx.GetObject(dict.GetAt(key),OpenMode.ForRead)).Data.AsArray().Select(v=>v.Value?.ToString()));}
    static void Text(Entity e,Transaction tx,string key,string value){if(e.ExtensionDictionary.IsNull)e.CreateExtensionDictionary();var dict=(DBDictionary)tx.GetObject(e.ExtensionDictionary,OpenMode.ForWrite);Xrecord record;if(dict.Contains(key))record=(Xrecord)tx.GetObject(dict.GetAt(key),OpenMode.ForWrite);else{record=new Xrecord();dict.SetAt(key,record);tx.AddNewlyCreatedDBObject(record,true);}record.Data=new ResultBuffer(Enumerable.Range(0,(value.Length+199)/200).Select(i=>new TypedValue((int)DxfCode.Text,value.Substring(i*200,Math.Min(200,value.Length-i*200)))).ToArray());}
    internal static FrameRecord Read(BlockReference reference,Transaction tx)=>JsonSerializer.Deserialize<FrameRecord>(Text(reference,tx,Key)??throw new ArgumentException("Ce bloc n’est pas un cadre JHR."),FrameDesign.Json)??throw new ArgumentException("Données du cadre absentes.");
    internal static Entity[] Entities(IEnumerable<FramePath> paths)=>paths.Select(path=>{var p=new Polyline();for(int i=0;i<path.Points.Length;i++)p.AddVertexAt(i,new Point2d(path.Points[i].X,path.Points[i].Y),path.Bulges!=null&&i<path.Bulges.Length?path.Bulges[i]:0,0,0);p.Closed=path.Closed;p.Layer="0";p.ColorIndex=256;p.LineWeight=LineWeight.ByLayer;return (Entity)p;}).ToArray();
    static ObjectId Definition(Database db,Transaction tx,string name,IEnumerable<Entity> geometry)
    {
        var table=(BlockTable)tx.GetObject(db.BlockTableId,OpenMode.ForWrite);var block=new BlockTableRecord{Name=name+"_"+Guid.NewGuid().ToString("N")[..8],Units=UnitsValue.Millimeters};var id=table.Add(block);tx.AddNewlyCreatedDBObject(block,true);foreach(var e in geometry){e.Layer="0";e.ColorIndex=256;e.LineWeight=LineWeight.ByLayer;if(e is DBText text)text.TextStyleId=db.Textstyle;block.AppendEntity(e);tx.AddNewlyCreatedDBObject(e,true);}return id;
    }
    internal static ObjectId Commit(Database db,FrameDesign design,Matrix3d transform,ObjectId target=default,string? expectedRevision=null)
    {
        string? error=design.Validate();if(error!=null)throw new ArgumentException(error);var elevation=FrameGeometry.Elevation(design);var sections=design.Cuts.ToDictionary(c=>c.Id,c=>FrameGeometry.Section(design,c));
        using var tx=db.TransactionManager.StartTransaction();var model=(BlockTableRecord)tx.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
        string name="CADRE_"+design.Tool.ToString().ToUpperInvariant()+"_"+design.Width.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"x"+design.Height.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
        var elevationDefinition=Definition(db,tx,name,Entities(elevation));FrameRecord? old=null;BlockReference root;
        if(target.IsNull){root=new BlockReference(Point3d.Origin,elevationDefinition);root.SetDatabaseDefaults(db);model.AppendEntity(root);tx.AddNewlyCreatedDBObject(root,true);}else{root=tx.GetObject(target,OpenMode.ForWrite) as BlockReference??throw new ArgumentException("Choisir un cadre.");old=Read(root,tx);if(expectedRevision!=null&&old.Revision!=expectedRevision)throw new ArgumentException("Le cadre a changé. Relire sa révision.");}
        root.BlockTableRecord=elevationDefinition;if(target.IsNull)root.TransformBy(transform);root.ColorIndex=256;root.LineWeight=LineWeight.ByLayer;
        string owner=root.Handle.ToString();var views=new Dictionary<string,string>();
        BlockReference? Owned(string handle){try{var id=db.GetObjectId(false,new Handle(Convert.ToInt64(handle,16)),0);return tx.GetObject(id,OpenMode.ForWrite,false) is BlockReference b&&Text(b,tx,Owner)==owner?b:null;}catch{return null;}}
        void View(string key,ObjectId definition,Point3d location){BlockReference? b=old?.Views.TryGetValue(key,out var handle)==true?Owned(handle):null;if(b==null){b=new BlockReference(Point3d.Origin,definition);b.SetDatabaseDefaults(db);model.AppendEntity(b);tx.AddNewlyCreatedDBObject(b,true);b.TransformBy(transform*Matrix3d.Displacement(location.GetAsVector()));}else b.BlockTableRecord=definition;Text(b,tx,Owner,owner);views[key]=b.Handle.ToString();}
        int vertical=0,horizontal=0;foreach(var cut in design.Cuts){var entities=Entities(sections[cut.Id]).ToList();double textHeight=design.LabelHeight;var title=new DBText{TextString="COUPE "+cut.Id+"–"+cut.Id,Height=textHeight,Position=cut.Vertical?new Point3d(0,design.Height+textHeight*1.5,0):new Point3d(0,-textHeight*2,0)};entities.Add(title);var id=Definition(db,tx,"COUPE_CADRE_"+cut.Id,entities);double depth=design.Profile!.Orient(design.Turns,design.Mirror).Depth;
            depth=Math.Max(depth,design.LeafProfile?.Orient(design.Turns,design.Mirror).Depth??0);var points=sections[cut.Id].SelectMany(p=>p.Sample()).ToArray();
            View(cut.Id,id,cut.Vertical?new(design.Width+150+(vertical++)*(depth+150)-points.Min(p=>p.X),0,0):new(0,-150-(horizontal++)*(depth+150)-points.Max(p=>p.Y),0));}
        var markers=new List<Entity>();foreach(var cut in design.Cuts){double h=design.LabelHeight;var paths=new List<FramePath>();if(cut.Vertical){paths.Add(FramePath.Line(cut.Station,-h,cut.Station,design.Height+h));double sign=cut.Reverse?-1:1;foreach(double y in new[]{-h,design.Height+h}){paths.Add(FramePath.Line(cut.Station,y,cut.Station+sign*h,y));paths.Add(new([new(cut.Station+sign*h,y),new(cut.Station+sign*h*.65,y+h*.15),new(cut.Station+sign*h*.65,y-h*.15)],null,true));markers.Add(new DBText{TextString=cut.Id,Height=h,Position=new(cut.Station-h*.3,y+h*.35,0)});}}else{paths.Add(FramePath.Line(-h,cut.Station,design.Width+h,cut.Station));double sign=cut.Reverse?-1:1;foreach(double x in new[]{-h,design.Width+h}){paths.Add(FramePath.Line(x,cut.Station,x,cut.Station+sign*h));paths.Add(new([new(x,cut.Station+sign*h),new(x+h*.15,cut.Station+sign*h*.65),new(x-h*.15,cut.Station+sign*h*.65)],null,true));markers.Add(new DBText{TextString=cut.Id,Height=h,Position=new(x+h*.35,cut.Station,0)});}}markers.AddRange(Entities(paths));}
        if(markers.Count>0)View("__markers",Definition(db,tx,"REPERE_CADRE",markers),Point3d.Origin);
        if(old!=null)foreach(var pair in old.Views)if(!views.ContainsKey(pair.Key))Owned(pair.Value)?.Erase();
        var record=new FrameRecord(design.Portable(),Guid.NewGuid().ToString("N"),views);Text(root,tx,Key,JsonSerializer.Serialize(record,FrameDesign.Json));tx.Commit();return root.ObjectId;
    }
}
