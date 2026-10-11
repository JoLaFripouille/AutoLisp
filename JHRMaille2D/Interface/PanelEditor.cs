using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Polyline=Autodesk.AutoCAD.DatabaseServices.Polyline;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
namespace JhrMailleUi;
internal sealed class HidePanel : DrawableOverrule
{
    internal ObjectId Target;internal HidePanel(ObjectId target){Target=target;SetCustomFilter();}
    public override bool IsApplicable(RXObject subject)=>subject is BlockReference b&&b.ObjectId==Target;
    public override bool WorldDraw(Drawable drawable,WorldDraw wd)=>true;
}
internal sealed class PanelEditor : IDisposable
{
    internal static PanelEditor? Active;readonly Document doc;readonly ObjectId target;readonly MeshForm form;PanelData data;Matrix3d transform;
    readonly List<Entity> preview=new();readonly List<Entity> outlines=new();readonly IntegerCollection viewports=new();
    readonly System.Windows.Forms.Timer timer=new(){Interval=300};HidePanel? hidden;bool busy,disposed,selectingOuter;bool oldOverruling;FillSettings draft;int generation;CancellationTokenSource? pending;GeometryResult? lastGeometry;string? lastKey;string legend="Intérieur : bleu   •   Extérieur : orange";
    internal PanelEditor(Document d,PanelData state,Matrix3d placement,ObjectId panel=default)
    {
        doc=d;target=panel;data=state;transform=placement;draft=state.Settings;if(state.LegacyDefaults)legend+="\nAncien panneau : paramètres non enregistrés repris par défaut, à vérifier.";form=new MeshForm(state.Settings);form.LiveMode(legend+"\nCalcul de l’aperçu…");
        form.DraftChanged+=ChangeDraft;form.ApplyRequested+=Apply;form.ContoursRequested+=Reselect;form.FormClosed+=(_,_)=>Dispose();timer.Tick+=(_,_)=>{timer.Stop();Refresh();};
        AcApp.DocumentManager.DocumentActivated+=DocumentChanged;AcApp.DocumentManager.DocumentToBeDestroyed+=DocumentClosing;
        Active=this;Highlight();AcApp.ShowModelessDialog(form);timer.Start();
    }
    void ChangeDraft(FillSettings next)
    {
        var previous=draft;draft=next;generation++;pending?.Cancel();timer.Stop();
        if(!selectingOuter&&next.Tool==FillTool.Mesh&&data.Outer==null){AskOuter(previous);return;}
        if(!selectingOuter)timer.Start();
    }
    async void AskOuter(FillSettings previous)
    {
        selectingOuter=true;bool chosen=false;form.Status("Le filet nécessite deux polylignes. Sélectionner le contour extérieur du cadre.",false);form.Hide();Clear(preview);Restore();
        try{await AcApp.DocumentManager.ExecuteInCommandContextAsync(_=>{
            if(disposed||AcApp.DocumentManager.MdiActiveDocument!=doc)return Task.CompletedTask;
            while(true){
                var opts=new PromptEntityOptions("\nFilet inox : contour extérieur du cadre (premier contour conservé) : ");opts.SetRejectMessage("\nChoisir une polyligne fermée.");opts.AddAllowedClass(typeof(Polyline),true);
                var pick=doc.Editor.GetEntity(opts);if(pick.Status!=PromptStatus.OK)break;
                try{using var tx=doc.Database.TransactionManager.StartTransaction();var p=(Polyline)tx.GetObject(pick.ObjectId,OpenMode.ForRead);
                    if(Math.Abs(p.Elevation-transform.CoordinateSystem3d.Origin.Z)>.01)throw new ArgumentException("Les deux contours doivent être coplanaires.");
                    var candidate=data with{Settings=draft,Outer=PanelStore.Vertices(p,transform.Inverse()),SourceHandles=data.SourceHandles.Take(1).Append(p.Handle.ToString()).ToArray()};
                    _=PanelGeometry.Prepare(candidate);data=candidate;chosen=true;Highlight();generation++;break;
                }catch(System.Exception error){doc.Editor.WriteMessage("\nJHR : "+error.Message);}
            }
            return Task.CompletedTask;
        },null);}catch(System.Exception error){if(!disposed)form.Status(error.Message,false);}
        finally{selectingOuter=false;if(!disposed){if(!chosen){draft=previous;form.SelectTool(previous.Tool);form.Status("Sélection annulée : remplissage précédent conservé.");}form.Show();form.Activate();timer.Start();}}
    }
    void DocumentChanged(object? sender,DocumentCollectionEventArgs e){if(e.Document!=doc)form.Close();}
    void DocumentClosing(object? sender,DocumentCollectionEventArgs e){if(e.Document==doc)form.Close();}
    static (ObjectId,ObjectId)? Select(Document d)
    {
        var a=new PromptEntityOptions("\nContour intérieur (ou limite unique du panneau) : ");a.SetRejectMessage("\nChoisir une polyligne fermée.");a.AddAllowedClass(typeof(Polyline),true);var ip=d.Editor.GetEntity(a);if(ip.Status!=PromptStatus.OK)return null;
        var b=new PromptEntityOptions("\nContour extérieur du cadre [Unique] : ");b.SetRejectMessage("\nChoisir une polyligne fermée.");b.AddAllowedClass(typeof(Polyline),true);b.Keywords.Add("Unique");var op=d.Editor.GetEntity(b);if(op.Status==PromptStatus.Keyword)return(ip.ObjectId,ObjectId.Null);return op.Status==PromptStatus.OK?(ip.ObjectId,op.ObjectId):null;
    }
    internal static void Create()
    {
        var doc=AcApp.DocumentManager.MdiActiveDocument;if(doc==null)return;if(Active!=null){Active.form.Activate();return;}var ids=Select(doc);if(ids==null)return;
        var settings=new FillSettings();if(ids.Value.Item2.IsNull)settings=settings with{Tool=FillTool.Square};var (data,matrix)=PanelStore.FromContours(doc.Database,ids.Value.Item1,ids.Value.Item2,settings);_ = new PanelEditor(doc,data,matrix);
    }
    internal static void Edit(ObjectId selected=default)
    {
        var doc=AcApp.DocumentManager.MdiActiveDocument;if(doc==null)return;if(Active!=null){Active.form.Activate();return;}if(selected.IsNull){var implied=doc.Editor.SelectImplied();if(implied.Status==PromptStatus.OK&&implied.Value.Count==1)selected=implied.Value.GetObjectIds()[0];else{var opts=new PromptEntityOptions("\nPanneau JHR à modifier : ");opts.SetRejectMessage("\nChoisir un bloc de panneau.");opts.AddAllowedClass(typeof(BlockReference),true);var r=doc.Editor.GetEntity(opts);if(r.Status!=PromptStatus.OK)return;selected=r.ObjectId;}}
        using var tx=doc.Database.TransactionManager.StartTransaction();var b=tx.GetObject(selected,OpenMode.ForRead) as BlockReference??throw new ArgumentException("Choisir une référence de panneau.");var state=PanelStore.Read(b,tx);var matrix=b.BlockTransform;doc.Editor.SetImpliedSelection([]);_ = new PanelEditor(doc,state,matrix,selected);
    }
    void Highlight()
    {
        Clear(outlines);void Add(Vertex[] vs,short color){var curve=PanelGeometry.Curve(vs);curve.TransformBy(transform);curve.ColorIndex=color;curve.LineWeight=LineWeight.LineWeight040;outlines.Add(curve);TransientManager.CurrentTransientManager.AddTransient(curve,TransientDrawingMode.Highlight,128,viewports);}
        Add(data.Inner,4);if(data.Outer!=null)Add(data.Outer,30);doc.Editor.UpdateScreen();
    }
    void Clear(List<Entity> list){foreach(var e in list){try{TransientManager.CurrentTransientManager.EraseTransient(e,viewports);}catch{}e.Dispose();}list.Clear();}
    void Restore(){if(hidden!=null){Overrule.RemoveOverrule(RXClass.GetClass(typeof(BlockReference)),hidden);hidden.Dispose();hidden=null;Overrule.Overruling=oldOverruling;doc.Editor.Regen();}}
    async void Refresh()
    {
        if(disposed||busy||selectingOuter)return;int revision=generation;busy=true;form.Status(legend+"\nCalcul de l’aperçu…",false);
        try{
            var candidate=data with{Settings=draft};candidate.Validate();pending?.Dispose();pending=new();var token=pending.Token;PanelGeometry.Prepared prepared;using(doc.LockDocument())prepared=PanelGeometry.Prepare(candidate);var result=await Task.Run(()=>PanelGeometry.Calculate(prepared,token));if(disposed||revision!=generation||AcApp.DocumentManager.MdiActiveDocument!=doc)return;lastGeometry=result;lastKey=candidate.ToJson();
            // Native offsets are computed in the AutoCAD UI thread. No database writes.
            using(doc.LockDocument()){
                Clear(preview);
                if(!target.IsNull&&hidden==null){hidden=new(target);oldOverruling=Overrule.Overruling;Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)),hidden,false);Overrule.Overruling=true;doc.Editor.Regen();}
                preview.AddRange(PanelGeometry.Entities(result,candidate));short color=7;using(var tx=doc.Database.TransactionManager.StartOpenCloseTransaction()){var layer=(LayerTableRecord)tx.GetObject(doc.Database.Clayer,OpenMode.ForRead);color=(short)layer.Color.ColorIndex;if(!target.IsNull){var b=(BlockReference)tx.GetObject(target,OpenMode.ForRead);var l=(LayerTableRecord)tx.GetObject(b.LayerId,OpenMode.ForRead);color=(short)l.Color.ColorIndex;}}
                foreach(var e in preview){e.TransformBy(transform);if(e is not Wipeout)e.ColorIndex=color;TransientManager.CurrentTransientManager.AddTransient(e,TransientDrawingMode.DirectShortTerm,129,viewports);}doc.Editor.UpdateScreen();form.Status(legend+$"\nAperçu {result.Width:0.##} × {result.Height:0.##} mm — {preview.Count} objets.");
            }
        }catch(OperationCanceledException){}catch(System.Exception e){Clear(preview);Restore();if(!disposed)form.Status(legend+"\n"+e.Message,false);}finally{busy=false;if(!disposed&&revision!=generation)timer.Start();}
        await Task.CompletedTask;
    }
    async void Apply(FillSettings s)
    {
        if(busy||disposed)return;timer.Stop();form.Enabled=false;
        try{await AcApp.DocumentManager.ExecuteInCommandContextAsync(_=>{if(disposed||AcApp.DocumentManager.MdiActiveDocument!=doc)return Task.CompletedTask;using(doc.LockDocument()){var candidate=data with{Settings=s};var id=PanelStore.Commit(doc.Database,candidate,transform,target,lastKey==candidate.ToJson()?lastGeometry:null);doc.Editor.WriteMessage("\nPanneau JHR validé : "+id.Handle);Restore();Clear(preview);form.Complete();doc.Editor.Regen();}return Task.CompletedTask;},null);}
        catch(System.Exception e){if(!disposed){form.Enabled=true;form.Status(e.Message,false);}}
    }
    async void Reselect()
    {
        if(busy||disposed)return;timer.Stop();form.Hide();try{await AcApp.DocumentManager.ExecuteInCommandContextAsync(_=>{var ids=Select(doc);if(ids!=null){var (next,matrix)=PanelStore.FromContours(doc.Database,ids.Value.Item1,ids.Value.Item2,draft);if(target.IsNull){data=next;transform=matrix;}else{ // Rebase selected world contours into the existing reference's coordinate system.
                    using var tx=doc.Database.TransactionManager.StartTransaction();var ip=(Polyline)tx.GetObject(ids.Value.Item1,OpenMode.ForRead);Polyline? op=ids.Value.Item2.IsNull?null:(Polyline)tx.GetObject(ids.Value.Item2,OpenMode.ForRead);data=next with{Inner=PanelStore.Vertices(ip,transform.Inverse()),Outer=op==null?null:PanelStore.Vertices(op,transform.Inverse())};}
                Highlight();generation++;}return Task.CompletedTask;},null);}catch(System.Exception e){form.Status(e.Message,false);}finally{if(!disposed){form.Show();timer.Start();}}
    }
    public void Dispose(){if(disposed)return;disposed=true;pending?.Cancel();timer.Stop();timer.Dispose();AcApp.DocumentManager.DocumentActivated-=DocumentChanged;AcApp.DocumentManager.DocumentToBeDestroyed-=DocumentClosing;Clear(preview);Clear(outlines);Restore();if(Active==this)Active=null;doc.Editor.UpdateScreen();}
}
