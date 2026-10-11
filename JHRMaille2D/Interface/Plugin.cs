using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;

namespace JhrMailleUi;
public static class LegacyDialog
{
    [LispFunction("jhr:maille-dialog-v140")]
    public static ResultBuffer Dialog(ResultBuffer args)
    {
        try
        {
            var raw=args.AsArray();
            if(raw.Length is not (11 or 12 or 37 or 38)) throw new ArgumentException("11 paramètres, une référence et les paramètres facultatifs des trois outils sont attendus.");
            var v=raw.Take(11).Select(x=>Convert.ToDouble(x.Value)).ToArray();
            var s=new MeshSettings(v[0],v[1],v[2],v[3],v[4],v[5],v[6]!=0,v[7]!=0,v[8],v[9],(int)v[10],raw.Length>=12?Convert.ToString(raw[11].Value)??"":"");
            var state=new FillSettings{Mesh=s};
            if(raw.Length>=37){var extra=raw.Skip(12).Select(x=>Convert.ToDouble(x.Value)).ToArray();if(extra[0] is <0 or >3 || extra[0]!=(int)extra[0])throw new ArgumentException("Outil inconnu.");state=state with{Tool=(FillTool)(int)extra[0],Square=PatternSettings.From(extra.Skip(1).Take(8).ToArray()),Expanded=PatternSettings.From(extra.Skip(9).Take(8).ToArray()),Perforated=PatternSettings.From(extra.Skip(17).Take(8).ToArray())};}
            if(raw.Length==38)state=state with{SquareMask=Convert.ToInt32(raw[37].Value)};
            using var form=new MeshForm(state);
            if(AcApp.ShowModalDialog(form)!=DialogResult.OK)
                return new ResultBuffer(new TypedValue((int)LispDataType.Nil));
            s=form.Settings;state=form.ToolSettings;
            var product=JakobCatalogue.Find(s.ProductReference);
            return new ResultBuffer(new[]{s.Width,s.Height,s.Wire,s.Lace,s.Gap,s.Angle,
                s.Woven?1d:0d,s.AutoCorners?1d:0d,s.SleeveLength,s.SleeveHeight,(double)s.Stops}
                .Select(x=>new TypedValue((int)LispDataType.Double,x)).Concat(new[]{s.ProductReference,product?.Family??"",product?.Material??""}.Select(x=>new TypedValue((int)LispDataType.Text,x))).Concat(state.ExtraValues().Append((double)state.SquareMask).Select(x=>new TypedValue((int)LispDataType.Double,x))).ToArray());
        }
        catch(System.Exception e)
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nJHRMAILLE : "+e.Message);
            return new ResultBuffer(new TypedValue((int)LispDataType.Nil));
        }
    }
    [LispFunction("jhr:maille-ui-version-v140")]
    public static string Version(ResultBuffer args)=>"1.4.4";
}
