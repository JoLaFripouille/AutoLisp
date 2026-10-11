using System.IO;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
[assembly: CommandClass(typeof(JhrMailleUi.Frames.FrameCommands))]
namespace JhrMailleUi.Frames;
public sealed class FrameCommands
{
    [CommandMethod("JHRCADRE",CommandFlags.Modal)]public void Create()=>FrameEditor.Safe(FrameEditor.Create);
    [CommandMethod("JHRCADREEDIT",CommandFlags.Modal|CommandFlags.UsePickSet)]public void Edit()=>FrameEditor.Safe(()=>FrameEditor.Edit());
    [LispFunction("jhr:cadre")]public static string Json(ResultBuffer args)=>FrameApi.Execute((string)args.AsArray()[0].Value);
    [CommandMethod("JHRCADREAPI",CommandFlags.Modal)]public void ApiFile(){var ed=AcApp.DocumentManager.MdiActiveDocument.Editor;var r=ed.GetString(new PromptStringOptions("\nFichier JSON : "){AllowSpaces=true});if(r.Status!=PromptStatus.OK)return;FrameEditor.Safe(()=>{string path=r.StringResult.Trim('"');File.WriteAllText(path+".response.json",FrameApi.Execute(File.ReadAllText(path)));ed.WriteMessage("\nRéponse : "+path+".response.json");});}
    [LispFunction("jhr:cadre-import")]
    public static string Import(ResultBuffer args)
    {
        try{var values=args.AsArray();string path=(string)values[0].Value;bool template=values.Length>1&&Convert.ToInt32(values[1].Value)==1;
            var profiles=template?ProfileLibrary.ImportTemplate(path):new List<FrameProfile>{ProfileLibrary.ImportFile(path)};ProfileLibrary.Save(profiles);
            return JsonSerializer.Serialize(new{ok=true,count=profiles.Count,profiles=profiles.Select(p=>new{p.Id,p.Name,p.Family,width=p.Width,depth=p.Depth,paths=p.Paths.Length})},FrameDesign.Json);
        }catch(System.Exception e){return JsonSerializer.Serialize(new{ok=false,error=e.Message});}
    }
}
