using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using System.Globalization;
namespace JhrMailleUi;
internal static class PanelStore
{
    internal const string Key="JHR_PANEL_V1";
    internal static bool HasMetadata(BlockReference b,Transaction tx)=>!b.ExtensionDictionary.IsNull&&((DBDictionary)tx.GetObject(b.ExtensionDictionary,OpenMode.ForRead)).Contains(Key);
    internal static bool IsPanel(BlockReference b,Transaction tx){if(HasMetadata(b,tx))return true;foreach(string app in new[]{"JHR_WEBNET","JHR_REMPLISSAGE"}){using var rb=b.GetXDataForApplication(app);if(rb!=null)return true;}return false;}
    internal static PanelData Read(BlockReference b,Transaction tx)
    {
        if(!HasMetadata(b,tx))return ImportLegacy(b,tx);
        var dict=(DBDictionary)tx.GetObject(b.ExtensionDictionary,OpenMode.ForRead);var record=(Xrecord)tx.GetObject(dict.GetAt(Key),OpenMode.ForRead);using var buffer=record.Data;
        return PanelData.Parse(string.Concat(buffer.AsArray().Select(v=>(string)v.Value)));
    }
    internal static void Write(BlockReference b,PanelData data,Transaction tx)
    {
        if(!b.IsWriteEnabled)b.UpgradeOpen();if(b.ExtensionDictionary.IsNull)b.CreateExtensionDictionary();var dict=(DBDictionary)tx.GetObject(b.ExtensionDictionary,OpenMode.ForWrite);
        Xrecord rec;if(dict.Contains(Key))rec=(Xrecord)tx.GetObject(dict.GetAt(Key),OpenMode.ForWrite);else{rec=new Xrecord();dict.SetAt(Key,rec);tx.AddNewlyCreatedDBObject(rec,true);}
        string json=data.ToJson();var chunks=new List<TypedValue>();for(int i=0;i<json.Length;i+=200)chunks.Add(new((int)DxfCode.Text,json.Substring(i,Math.Min(200,json.Length-i))));using var rb=new ResultBuffer(chunks.ToArray());rec.Data=rb;
    }
    internal static Vertex[] Vertices(Polyline p,Matrix3d local)
    {
        if(!p.Closed||p.NumberOfVertices<3||!p.Normal.IsEqualTo(Vector3d.ZAxis,new Tolerance(1e-8,1e-8)))throw new ArgumentException("Choisir une polyligne fermée dans le plan XY.");
        return Enumerable.Range(0,p.NumberOfVertices).Select(i=>{var q=p.GetPoint3dAt(i).TransformBy(local);return new Vertex(q.X,q.Y,p.GetBulgeAt(i));}).ToArray();
    }
    internal static (PanelData,Matrix3d) FromContours(Database db,ObjectId inside,ObjectId outside,FillSettings settings)
    {
        using var tx=db.TransactionManager.StartTransaction();var ip=tx.GetObject(inside,OpenMode.ForRead) as Polyline??throw new ArgumentException("Contour intérieur attendu.");var op=outside.IsNull?null:tx.GetObject(outside,OpenMode.ForRead) as Polyline??throw new ArgumentException("Contour extérieur attendu.");
        if(op!=null && (inside==outside||Math.Abs(ip.Elevation-op.Elevation)>.01))throw new ArgumentException("Choisir deux contours distincts et coplanaires.");var bounds=ip.GeometricExtents;var origin=new Point3d((bounds.MinPoint.X+bounds.MaxPoint.X)/2,(bounds.MinPoint.Y+bounds.MaxPoint.Y)/2,ip.Elevation);var matrix=Matrix3d.Displacement(origin.GetAsVector());
        return (new(){Settings=settings,Inner=Vertices(ip,matrix.Inverse()),Outer=op==null?null:Vertices(op,matrix.Inverse()),SourceHandles=op==null?[ip.Handle.ToString()]:[ip.Handle.ToString(),op.Handle.ToString()]},matrix);
    }
    internal static ObjectId Resolve(Database db,string handle)=>db.GetObjectId(false,new Handle(long.Parse(handle,NumberStyles.HexNumber,CultureInfo.InvariantCulture)),0);
    static PanelData ImportLegacy(BlockReference b,Transaction tx)
    {
        using var rb=b.GetXDataForApplication("JHR_WEBNET")??b.GetXDataForApplication("JHR_REMPLISSAGE");if(rb==null)throw new ArgumentException("Ce bloc n’est pas un panneau JHR éditable.");var raw=rb.AsArray();string app=Convert.ToString(raw.FirstOrDefault().Value)??"";bool mesh=app=="JHR_WEBNET";
        if(!mesh&&app!="JHR_REMPLISSAGE")throw new ArgumentException("Ce bloc n’est pas un panneau JHR éditable.");
        var hs=raw.Where(v=>v.TypeCode==1005).Select(v=>(string)v.Value).ToArray();if(hs.Length<2)throw new ArgumentException("Les contours de cet ancien panneau sont manquants. Recréer le panneau une fois avec JHRMAILLE.");
        var db=b.Database;var ip=tx.GetObject(Resolve(db,hs[0]),OpenMode.ForRead) as Polyline;var op=tx.GetObject(Resolve(db,hs[1]),OpenMode.ForRead) as Polyline;if(ip==null||op==null)throw new ArgumentException("Les contours sources de cet ancien panneau ont été supprimés.");
        var values=raw.Where(v=>v.TypeCode==1040).Select(v=>Convert.ToDouble(v.Value)).ToArray();var s=new FillSettings();double angle=0;
        if(mesh){if(values.Length<4)throw new ArgumentException("Paramètres hérités incomplets.");string desc=string.Concat(raw.Where(v=>v.TypeCode==1000).Select(v=>Convert.ToString(v.Value)));string reference=raw.Where(v=>v.TypeCode==1000).Select(v=>Convert.ToString(v.Value)??"").FirstOrDefault(v=>v.StartsWith("reference_jakob="))?.Split('=')[1]??"";s=s with{Mesh=new(Width:values[3],Height:values[2],Wire:values[0],Lace:values[1],Woven:desc.Contains("mode=tresse"),ProductReference:reference)};}
        else{var tool=(FillTool)Convert.ToInt32(raw.First(v=>v.TypeCode==1070).Value);var pattern=PatternSettings.From(values.Take(8).ToArray());angle=pattern.Angle*Math.PI/180;s=s.WithPattern(tool,pattern with{Angle=0}) with{Tool=tool};}
        // Legacy definitions are in the original engine's local rotated coordinates.
        // Using source-frame origin instead of this reference preserves moved/copied panels.
        var box=ip.GeometricExtents;var matrix=Matrix3d.Displacement(new((box.MinPoint.X+box.MaxPoint.X)/2,(box.MinPoint.Y+box.MaxPoint.Y)/2,ip.Elevation))*Matrix3d.Rotation(angle,Vector3d.ZAxis,Point3d.Origin);
        if(mesh){ // Original angle is not stored in legacy xdata; recover it from the original reference if still present.
            var space=(BlockTableRecord)tx.GetObject(b.OwnerId,OpenMode.ForRead);var original=space.Cast<ObjectId>().Select(id=>tx.GetObject(id,OpenMode.ForRead)).OfType<BlockReference>().FirstOrDefault(r=>r.BlockTableRecord==b.BlockTableRecord&&r.Position.DistanceTo(new((box.MinPoint.X+box.MaxPoint.X)/2,(box.MinPoint.Y+box.MaxPoint.Y)/2,ip.Elevation))<.01);angle=original?.Rotation??b.Rotation;matrix=Matrix3d.Displacement(new((box.MinPoint.X+box.MaxPoint.X)/2,(box.MinPoint.Y+box.MaxPoint.Y)/2,ip.Elevation))*Matrix3d.Rotation(angle,Vector3d.ZAxis,Point3d.Origin);
        }
        return new(){Engine="legacy",LegacyDefaults=true,Settings=s,Inner=Vertices(ip,matrix.Inverse()),Outer=ip.ObjectId==op.ObjectId?null:Vertices(op,matrix.Inverse()),SourceHandles=hs};
    }
    internal static void HiddenType(Database db,Transaction tx)
    {
        var table=(LinetypeTable)tx.GetObject(db.LinetypeTableId,OpenMode.ForRead);if(table.Has("JHR_CACHE"))return;table.UpgradeOpen();var type=new LinetypeTableRecord{Name="JHR_CACHE",AsciiDescription="JHR — derrière le cadre",PatternLength=6,NumDashes=2};type.SetDashLengthAt(0,4);type.SetDashLengthAt(1,-2);table.Add(type);tx.AddNewlyCreatedDBObject(type,true);
    }
    internal static ObjectId Commit(Database db,PanelData data,Matrix3d transform,ObjectId target=default,GeometryResult? cached=null)
    {
        data=data with{Engine="1.4.3",LegacyDefaults=false};data.Validate();var geometry=cached??PanelGeometry.Generate(data);using var tx=db.TransactionManager.StartTransaction();HiddenType(db,tx);
        var entities=PanelGeometry.Entities(geometry,data);try{
            var table=(BlockTable)tx.GetObject(db.BlockTableId,OpenMode.ForWrite);string stem=data.Settings.Tool switch{FillTool.Mesh=>"PANNEAU_FILET_INOX_",FillTool.Square=>"PANNEAU_GRILLAGE_CARRE_",FillTool.Expanded=>"PANNEAU_METAL_DEPLOYE_",_=>"PANNEAU_TOLE_PERFOREE_"};string dims=$"{geometry.Width:0.##}x{geometry.Height:0.##}".Replace(',','.');string name=stem+dims;int index=1;while(table.Has(name+"_"+index))index++;var def=new BlockTableRecord{Name=name+"_"+index};table.Add(def);tx.AddNewlyCreatedDBObject(def,true);foreach(var e in entities){def.AppendEntity(e);tx.AddNewlyCreatedDBObject(e,true);}
            var ids=new ObjectIdCollection();foreach(var e in entities.Where(e=>e is Wipeout))ids.Add(e.ObjectId);if(ids.Count>0)((DrawOrderTable)tx.GetObject(def.DrawOrderTableId,OpenMode.ForWrite)).MoveToBottom(ids);
            BlockReference b;if(target.IsNull){b=new BlockReference(Point3d.Origin,def.ObjectId);b.SetDatabaseDefaults(db);b.TransformBy(transform);var space=(BlockTableRecord)tx.GetObject(db.CurrentSpaceId,OpenMode.ForWrite);space.AppendEntity(b);tx.AddNewlyCreatedDBObject(b,true);}else{b=tx.GetObject(target,OpenMode.ForWrite) as BlockReference??throw new ArgumentException("Référence de panneau attendue.");b.BlockTableRecord=def.ObjectId;}
            if(target.IsNull){b.ColorIndex=256;b.LineWeight=LineWeight.ByLayer;}foreach(string app in new[]{"JHR_WEBNET","JHR_REMPLISSAGE"}){using var old=b.GetXDataForApplication(app);if(old!=null){using var cleared=new ResultBuffer(new TypedValue(1001,app));b.XData=cleared;}}Write(b,data,tx);var id=b.ObjectId;tx.Commit();return id;
        }finally{foreach(var e in entities)if(e.ObjectId.IsNull)e.Dispose();}
    }
}
