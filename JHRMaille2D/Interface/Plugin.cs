using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;

namespace JhrMailleUi;
public static class Plugin
{
    [LispFunction("jhr:maille-dialog-v120")]
    public static ResultBuffer Dialog(ResultBuffer args)
    {
        try
        {
            var raw=args.AsArray();
            if(raw.Length is not (11 or 12)) throw new ArgumentException("11 paramètres et une référence facultative sont attendus.");
            var v=raw.Take(11).Select(x=>Convert.ToDouble(x.Value)).ToArray();
            var s=new MeshSettings(v[0],v[1],v[2],v[3],v[4],v[5],v[6]!=0,v[7]!=0,v[8],v[9],(int)v[10],raw.Length==12?Convert.ToString(raw[11].Value)??"":"");
            using var form=new MeshForm(s);
            if(AcApp.ShowModalDialog(form)!=DialogResult.OK)
                return new ResultBuffer(new TypedValue((int)LispDataType.Nil));
            s=form.Settings;
            var product=JakobCatalogue.Find(s.ProductReference);
            return new ResultBuffer(new[]{s.Width,s.Height,s.Wire,s.Lace,s.Gap,s.Angle,
                s.Woven?1d:0d,s.AutoCorners?1d:0d,s.SleeveLength,s.SleeveHeight,(double)s.Stops}
                .Select(x=>new TypedValue((int)LispDataType.Double,x)).Concat(new[]{s.ProductReference,product?.Family??"",product?.Material??""}.Select(x=>new TypedValue((int)LispDataType.Text,x))).ToArray());
        }
        catch(System.Exception e)
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nJHRMAILLE : "+e.Message);
            return new ResultBuffer(new TypedValue((int)LispDataType.Nil));
        }
    }
    [LispFunction("jhr:maille-ui-version-v120")]
    public static string Version(ResultBuffer args)=>"1.2.0";
}
