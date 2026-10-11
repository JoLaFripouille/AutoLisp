using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
[assembly: ExtensionApplication(typeof(JhrMailleUi.PanelPlugin))]
[assembly: CommandClass(typeof(JhrMailleUi.PanelPlugin))]
namespace JhrMailleUi;
public sealed class PanelPlugin : IExtensionApplication
{
    public static PanelPlugin? Instance;
    internal static ObjectId Pending;static object? doubleClickSetting;static Document? pendingDocument;static bool queued;static DateTime pendingUntil;static Document? guardDocument;static DateTime restoreAfter;internal static string DoubleClickInfo="idle";
    public void Initialize(){foreach(var a in AppDomain.CurrentDomain.GetAssemblies().Where(a=>a!=typeof(PanelPlugin).Assembly&&a.GetName().Name!.StartsWith("JhrMaillePlugin"))){var old=a.GetType("JhrMailleUi.PanelPlugin");var instance=old?.GetField("Instance")?.GetValue(null);if(instance!=null)old!.GetMethod("Terminate")!.Invoke(instance,null);}
        Instance=this;AcApp.BeginDoubleClick+=DoubleClick;AcApp.Idle+=Idle;AcApp.DocumentManager.DocumentLockModeChanged+=VetoDefaultEdit;AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nJHR Panneaux .NET 1.4.4 — JHRMAILLE / JHRPANNEAUEDIT / JHRPANNEAUAPI.");}
    public void Terminate(){AcApp.BeginDoubleClick-=DoubleClick;AcApp.Idle-=Idle;AcApp.DocumentManager.DocumentLockModeChanged-=VetoDefaultEdit;PanelEditor.Active?.Dispose();RestoreSetting();}
    void RestoreSetting(){if(doubleClickSetting!=null){AcApp.SetSystemVariable("DBLCLKEDIT",doubleClickSetting);doubleClickSetting=null;}}
    void Idle(object? sender,EventArgs e)
    {
        // AutoCAD posts its own edit command after BeginDoubleClick. Keep the
        // setting disabled through that dispatch, even after Pending is consumed.
        if(doubleClickSetting!=null&&DateTime.UtcNow>=restoreAfter&&Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))==0)RestoreSetting();
        if(DateTime.UtcNow>pendingUntil){Pending=ObjectId.Null;pendingDocument=null;guardDocument=null;queued=false;RestoreSetting();return;}
        if(Pending.IsNull||queued)return;
        if(AcApp.DocumentManager.MdiActiveDocument==pendingDocument&&Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))==0){queued=true;pendingDocument!.SendStringToExecute("JHRPANNEAUDBL144F ",true,false,false);}
    }
    void VetoDefaultEdit(object? sender,DocumentLockModeChangedEventArgs e)
    {
        // Routing consumes Pending before a late BEDIT arrives. The short-lived
        // guard is independent, scoped to this document and this double-click.
        string name=e.GlobalCommandName.TrimStart('_','.').ToUpperInvariant();
        if(e.Document==guardDocument&&DateTime.UtcNow<=pendingUntil&&new[]{"BEDIT","MODIFBLOC","EATTEDIT","DDEDIT","PROPERTIES"}.Contains(name)){
            DoubleClickInfo+=" veto:"+name;e.Veto();
        }
    }
    void DoubleClick(object? sender,BeginDoubleClickEventArgs e)
    {
        try{
            var d=AcApp.DocumentManager.MdiActiveDocument;if(d==null)return;ObjectId match=ObjectId.Null;var point=e.Location;DoubleClickInfo="point:"+point+" active:"+AcApp.GetSystemVariable("CMDACTIVE");
            using(var tx=d.Database.TransactionManager.StartOpenCloseTransaction()){
                var implied=d.Editor.SelectImplied();var candidates=implied.Status==PromptStatus.OK?implied.Value.GetObjectIds():Array.Empty<ObjectId>();
                // Select at the actual double-click point, including blocks without PICKFIRST.
                double epsilon=Convert.ToDouble(AcApp.GetSystemVariable("VIEWSIZE"))/500;var hit=d.Editor.SelectCrossingWindow(point+new Vector3d(-epsilon,-epsilon,0),point+new Vector3d(epsilon,epsilon,0),new SelectionFilter([new TypedValue((int)DxfCode.Start,"INSERT")]));
                if(hit.Status==PromptStatus.OK)candidates=hit.Value.GetObjectIds();foreach(var id in candidates)if(tx.GetObject(id,OpenMode.ForRead) is BlockReference b&&PanelStore.IsPanel(b,tx)){match=id;break;}
            }
            if(match.IsNull){DoubleClickInfo+=" no panel";guardDocument=null;Pending=ObjectId.Null;pendingDocument=null;queued=false;RestoreSetting();return;}Pending=match;pendingDocument=d;guardDocument=d;pendingUntil=DateTime.UtcNow.AddSeconds(3);queued=false;
            if(doubleClickSetting==null)doubleClickSetting=AcApp.GetSystemVariable("DBLCLKEDIT");
            AcApp.SetSystemVariable("DBLCLKEDIT",0);restoreAfter=DateTime.UtcNow.AddMilliseconds(750);DoubleClickInfo+=" panel:"+match.Handle;
        }catch(System.Exception error){DoubleClickInfo+=" error:"+error.Message;RestoreSetting();}
    }
    void Safe(Action f){try{f();}catch(System.Exception e){AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nJHR : "+e.Message);}}
    [CommandMethod("JHRMAILLE",CommandFlags.Modal)]public void Maille()=>Safe(PanelEditor.Create);
    [CommandMethod("JHRPANNEAU",CommandFlags.Modal)]public void Create()=>Safe(PanelEditor.Create);
    [CommandMethod("JHRPANNEAUEDIT",CommandFlags.Modal|CommandFlags.UsePickSet)]public void Edit()=>Safe(()=>PanelEditor.Edit());
    [CommandMethod("JHRPANNEAUDBL144F",CommandFlags.Modal)]public void DoubleEdit(){var id=Pending;Pending=ObjectId.Null;queued=false;DoubleClickInfo+=" execute";if(!id.IsNull&&AcApp.DocumentManager.MdiActiveDocument==pendingDocument)Safe(()=>PanelEditor.Edit(id));pendingDocument=null;}
    [CommandMethod("JHRPANNEAUUPDATE",CommandFlags.Modal)]public void Updates()=>Safe(()=>{using var dialog=new UpdateDialog();AcApp.ShowModalDialog(dialog);});
    [CommandMethod("JHRPANNEAUAPI",CommandFlags.Modal)]public void ApiFile(){var d=AcApp.DocumentManager.MdiActiveDocument;var r=d.Editor.GetString(new PromptStringOptions("\nFichier de requête JSON : "){AllowSpaces=true});if(r.Status!=PromptStatus.OK)return;string path=r.StringResult.Trim('"');string output=path+".response.json";try{File.WriteAllText(output,PanelApi.Execute(File.ReadAllText(path)));d.Editor.WriteMessage("\nRéponse : "+output);}catch(System.Exception e){d.Editor.WriteMessage("\nJHR : "+e.Message);}}
    [LispFunction("jhr:panneau-double-info")]public static string DoubleInfo(ResultBuffer args)=>DoubleClickInfo;
    [LispFunction("jhr:panneau") ]public static string Json(ResultBuffer args)=>PanelApi.Execute((string)args.AsArray()[0].Value);
    [LispFunction("jhr:maille-net-create-v140")]public static object? CreateLisp(ResultBuffer args){try{PanelEditor.Create();}catch(System.Exception e){AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nJHR : "+e.Message);}return null;}
}
