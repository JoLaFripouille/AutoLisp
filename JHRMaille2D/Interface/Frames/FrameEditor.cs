using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
namespace JhrMailleUi.Frames;
internal static class FrameEditor
{
    internal static bool Active;
    internal static void Safe(Action work){try{work();}catch(Exception e){AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nJHR Cadres : "+e.Message);}finally{Active=false;}}
    internal static void TransformCheck(BlockReference b){var s=b.ScaleFactors;if(Math.Abs(s.X-1)>.00001||Math.Abs(s.Y-1)>.00001||Math.Abs(s.Z-1)>.00001||Math.Abs(b.Normal.Z-1)>.00001)throw new ArgumentException("Le cadre doit rester à l’échelle 1:1 dans le plan XY. Rétablir son échelle avant modification.");}
    static FrameDesign? Show(FrameDesign initial)
    {
        Active=true;try{using var dialog=new FrameDialog(initial,ProfileLibrary.Choices(),(path,template)=>{var p=template?ProfileLibrary.ImportTemplate(path):new List<FrameProfile>{ProfileLibrary.ImportFile(path)};ProfileLibrary.Save(p);return p;},path=>ProfileLibrary.Discover(path));return AcApp.ShowModalDialog(dialog)==DialogResult.OK?dialog.Design:null;}finally{Active=false;}
    }
    internal static void Create()
    {
        var doc=AcApp.DocumentManager.MdiActiveDocument;var ed=doc.Editor;var p=ProfileLibrary.Load().FirstOrDefault(p=>p.Family=="Tubes"&&Math.Abs(p.Width-40)<.01&&Math.Abs(p.Depth-40)<.01&&p.Name.Contains("ép.3",StringComparison.OrdinalIgnoreCase))??ProfileLibrary.Load().FirstOrDefault(p=>p.Family=="Tubes");
        var draft=Show(new(){Profile=p});if(draft==null)return;
        var location=ed.GetPoint("\nCoin inférieur gauche du cadre : ");if(location.Status!=PromptStatus.OK)return;
        var point=location.Value.TransformBy(ed.CurrentUserCoordinateSystem);var transform=Matrix3d.Displacement(point.GetAsVector());
        if(Math.Abs(ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis.Z-1)>.00001)throw new ArgumentException("Placer le SCU dans le plan XY avant de créer le cadre.");
        double angle=Vector3d.XAxis.GetAngleTo(ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis,Vector3d.ZAxis);transform*=Matrix3d.Rotation(angle,Vector3d.ZAxis,Point3d.Origin);
        using(doc.LockDocument()){FrameStore.Commit(doc.Database,draft,transform);ed.Regen();}ed.WriteMessage("\nCadre et "+draft.Cuts.Length+" coupe(s) créés, en mm à l’échelle 1:1.");
    }
    internal static void Edit(ObjectId id=default)
    {
        var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;var ed=doc.Editor;
        if(id.IsNull){var implied=ed.SelectImplied();if(implied.Status==PromptStatus.OK){using var tx=db.TransactionManager.StartOpenCloseTransaction();id=implied.Value.GetObjectIds().FirstOrDefault(i=>tx.GetObject(i,OpenMode.ForRead) is BlockReference b&&FrameStore.IsFrame(b,tx));}
            if(id.IsNull){var options=new PromptEntityOptions("\nChoisir le bloc principal du cadre JHR : ");options.SetRejectMessage("Choisir un bloc de cadre.");options.AddAllowedClass(typeof(BlockReference),true);var result=ed.GetEntity(options);if(result.Status!=PromptStatus.OK)return;id=result.ObjectId;}}
        FrameRecord old;Matrix3d transform;using(var tx=db.TransactionManager.StartOpenCloseTransaction()){var b=(BlockReference)tx.GetObject(id,OpenMode.ForRead);TransformCheck(b);old=FrameStore.Read(b,tx);transform=b.BlockTransform;}
        var next=Show(old.Design);if(next==null)return;using(doc.LockDocument()){FrameStore.Commit(db,next,transform,id,old.Revision);ed.Regen();}ed.WriteMessage("\nCadre et coupes mis à jour. Placement des coupes conservé.");
    }
}
