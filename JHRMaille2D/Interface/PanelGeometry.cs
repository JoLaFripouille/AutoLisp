using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using static JhrMailleUi.GeometryRuntime;
namespace JhrMailleUi;
internal sealed class GeometryResult
{
    internal object? Shapes;internal Dictionary<string,object?> Components=new();
    internal int Cells,Nodes,Boundary;internal double Width,Height;
}
internal static class PanelGeometry
{
    internal static Polyline Curve(Vertex[] vertices){var p=new Polyline(vertices.Length);for(int i=0;i<vertices.Length;i++)p.AddVertexAt(i,new(vertices[i].X,vertices[i].Y),vertices[i].Bulge,0,0);p.Closed=true;return p;}
    static object? Sample(Polyline p){var result=new List<object?>();for(int i=0;i<p.NumberOfVertices;i++){double d0=p.GetDistanceAtParameter(i),d1=p.GetDistanceAtParameter(i+1);int count=Math.Abs(p.GetBulgeAt(i))<1e-10?1:Math.Max(4,(int)(1+(d1-d0)/3));for(int j=0;j<count;j++){var q=p.GetPointAtParameter(i+(double)j/count);result.Add(L(q.X,q.Y));}}return L(result.ToArray());}
    static object? Positive(GeometryRuntime g,object? p)=>D(g.Invoke("wm:area",p))<0?L(Items(p).Reverse().ToArray()):p;
    static object? Inset(GeometryRuntime g,Polyline p,object? original,double gap)
    {
        if(gap<1e-8)return original;
        foreach(int sign in new[]{-1,1}){
            DBObjectCollection? curves=null;
            try {curves=p.GetOffsetCurves(sign*gap);var valid=new List<object?>();foreach(DBObject obj in curves)if(obj is Polyline q){var points=Positive(g,Sample(q));double area=Math.Abs(D(g.Invoke("wm:area",points)));if(area>1 && area<Math.Abs(D(g.Invoke("wm:area",original)))&&Items(points).All(v=>Truth(g.Invoke("wm:inside",v,original))))valid.Add(points);}if(valid.Count>1)throw new ArgumentException("Le retrait divise le panneau. Traiter chaque portion séparément.");if(valid.Count==1)return valid[0];}
            catch(Autodesk.AutoCAD.Runtime.Exception){}
            finally{if(curves!=null){foreach(DBObject o in curves)o.Dispose();curves.Dispose();}}
        }
        throw new ArgumentException("Retrait impossible : réduire le retrait ou vérifier le contour.");
    }
    internal sealed record Prepared(PanelData Data,object? Inner,object? Outer,object? Plate,object? Poly,double Angle);
    internal static GeometryResult Generate(PanelData data,CancellationToken token=default)=>Calculate(Prepare(data),token);
    internal static Prepared Prepare(PanelData data)
    {
        data.Validate();var settings=data.Settings;double angle=(settings.Tool==FillTool.Mesh?settings.Mesh.Angle:settings.Pattern(settings.Tool).Angle)*Math.PI/180;
        using var inner=Curve(data.Inner);using var outer=Curve(data.Outer??data.Inner);
        var rotation=Matrix3d.Rotation(-angle,Vector3d.ZAxis,Point3d.Origin);inner.TransformBy(rotation);outer.TransformBy(rotation);
        var g=new GeometryRuntime();var ip=Positive(g,Sample(inner));var op=Positive(g,Sample(outer));
        if(Truth(g.Invoke("wm:selfcross",ip))||Truth(g.Invoke("wm:selfcross",op)))throw new ArgumentException("Contour auto-intersectant.");
        if(data.Outer!=null&&(!Items(ip).All(v=>Truth(g.Invoke("wm:inside",v,op)))||Items(g.Invoke("wm:edges",ip)).Any(a=>Items(g.Invoke("wm:edges",op)).Any(b=>Truth(g.Invoke("wm:hit",Car(a),Car(Cdr(a)),Car(b),Car(Cdr(b))))))))throw new ArgumentException("Le contour extérieur doit entourer strictement le contour intérieur.");
        object? plate,poly;
        if(settings.Tool==FillTool.Mesh){plate=op;poly=Inset(g,inner,ip,settings.Mesh.Gap);}
        else{var s=settings.Pattern(settings.Tool);var source=settings.Tool==FillTool.Square&&s.Pattern!=0?outer:inner;var basePoly=settings.Tool==FillTool.Square&&s.Pattern!=0?op:ip;plate=Inset(g,source,basePoly,s.Gap);poly=Inset(g,source,basePoly,s.Gap+(settings.Tool==FillTool.Perforated?s.Margin:0));}
        return new(data,ip,op,plate,poly,angle);
    }
    // Pure .NET calculation; safe to run on a worker while the user changes settings.
    internal static GeometryResult Calculate(Prepared prepared,CancellationToken token=default)
    {
        var data=prepared.Data;var settings=data.Settings;var ip=prepared.Inner;var op=prepared.Outer;var plate=prepared.Plate;var poly=prepared.Poly;double angle=prepared.Angle;var g=new GeometryRuntime(token);var c=g.Root;
        c.Bind("wm:inner",ip);c.Bind("wm:outer",op);c.Bind("wm:shapes",null);c.Bind("rf:masks",null);c.Bind("rf:count",0d);c.Bind("*wm:tool*",(double)settings.Tool);c.Bind("*rf:mask*",(double)settings.SquareMask);
        if(settings.Tool==FillTool.Mesh){
            var s=settings.Mesh;string[] keys=["ml","mw","wire","lace","gap","angle","woven","corner-auto","sleeve-l","sleeve-h","stops","maxnodes"];
            double[] values=[s.Width,s.Height,s.Wire,s.Lace,s.Gap,angle,s.Woven?1:0,s.AutoCorners?1:0,s.SleeveLength,s.SleeveHeight,s.Stops,15000];
            c.Bind("*wm:cfg*",L(keys.Select((k,i)=>(object?)new GeometryRuntime.Cell(new Symbol(k),values[i])).ToArray()));
            c.Bind("wm:netpoly",poly);foreach(var name in new[]{"wm:anchors","wm:cuts","wm:visibility"})c.Bind(name,null);foreach(var name in new[]{"wm:nodes","wm:boundary","wm:corners","wm:extensions","wm:cells"})c.Bind(name,0d);
            string bead=s.Woven?$"JHR_MAILLE_TRESSE_D{s.Wire.ToString("F3",System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.').Replace('.','_')}_V1":$"JHR_WEBNET_DOUILLE_{s.SleeveLength:g}x{s.SleeveHeight:g}_V1";
            c.Bind("wm:bead",bead);g.Invoke("wm:mesh",poly);g.Invoke("wm:terminals");if(Items(c.Get("wm:anchors")).Count()<4)throw new ArgumentException("Cadre trop petit pour cette maille.");g.Invoke("wm:lace",poly);g.Invoke("wm:eye-block");if(s.Woven)g.Invoke("wm:weave-block",bead);else g.Invoke("wm:bead-block",bead,s.SleeveLength,s.SleeveHeight);g.Invoke("wm:bead-block","JHR_WEBNET_BUTEE_4x4_V1",4d,4d);plate=op;
        }else{
            var s=settings.Pattern(settings.Tool);var opts=L(s.Values().Cast<object?>().ToArray());
            if(settings.Tool==FillTool.Square)g.Invoke("rf:square",poly,opts);else if(settings.Tool==FillTool.Expanded)g.Invoke("rf:expanded",poly,opts);else g.Invoke("rf:perforated",plate,poly,opts);
            if(I(c.Get("rf:count"))==0)throw new ArgumentException("Aucun motif ne tient dans ce panneau.");
        }
        var shapes=Items(c.Get("rf:masks")).Reverse().Concat(Items(c.Get("wm:shapes")).Reverse()).ToArray();
        // Rotate every drawable back to the stored frame coordinate system later.
        var ps=Items(plate).Select(v=>{var a=Items(v).Select(D).ToArray();return new Point2d(a[0]*Math.Cos(angle)-a[1]*Math.Sin(angle),a[0]*Math.Sin(angle)+a[1]*Math.Cos(angle));}).ToArray();
        return new(){Shapes=L(shapes),Components=g.Components,Cells=I(c.Get("wm:cells")),Nodes=I(c.Get("wm:nodes")),Boundary=I(c.Get("wm:boundary")),Width=ps.Max(p=>p.X)-ps.Min(p=>p.X),Height=ps.Max(p=>p.Y)-ps.Min(p=>p.Y)};
    }
    static object? Value(object? shape,int code)=>Cdr(Items(shape).FirstOrDefault(v=>Eq(Car(v),code)));
    static Point2d Point(object? v){var a=Items(v).Select(D).ToArray();return new(a[0],a[1]);}
    internal static List<Entity> Entities(GeometryResult result,PanelData data)
    {
        var entities=new List<Entity>();double angle=(data.Settings.Tool==FillTool.Mesh?data.Settings.Mesh.Angle:data.Settings.Pattern(data.Settings.Tool).Angle)*Math.PI/180;
        void Add(object? shape,Matrix3d matrix,int depth=0){
            if(depth>4)throw new ArgumentException("Composant imbriqué trop profond.");string type=(string)Value(shape,0)!;Entity e;
            if(type=="INSERT"){
                string name=(string)Value(shape,2)!;var p=Point(Value(shape,10));double a=D(Value(shape,50));var transform=matrix*Matrix3d.Displacement(new(p.X,p.Y,0))*Matrix3d.Rotation(a,Vector3d.ZAxis,Point3d.Origin);
                if(!result.Components.TryGetValue(name,out var children))throw new ArgumentException("Composant manquant : "+name);foreach(var child in Items(children))Add(child,transform,depth+1);return;
            }
            if(type=="LWPOLYLINE"){
                var points=Items(shape).Where(v=>Eq(Car(v),10)).Select(v=>Point(Cdr(v))).ToArray();var p=new Polyline(points.Length);for(int i=0;i<points.Length;i++)p.AddVertexAt(i,points[i],0,0,0);p.Closed=I(Value(shape,70))==1;p.ConstantWidth=D(Value(shape,43));e=p;
            }else if(type=="CIRCLE"){var p=Point(Value(shape,10));e=new Circle(new(p.X,p.Y,0),Vector3d.ZAxis,D(Value(shape,40)));}
            else if(type=="WIPEOUT"){
                var b=Point(Value(shape,10));var u=Point(Value(shape,11));var v=Point(Value(shape,12));var pts=Items(shape).Where(q=>Eq(Car(q),14)).Select(q=>{var p=Point(Cdr(q));return new Point2d(b.X+(p.X+.5)*u.X+(.5-p.Y)*v.X,b.Y+(p.X+.5)*u.Y+(.5-p.Y)*v.Y);}).ToArray();var w=new Wipeout();w.SetFrom(new Point2dCollection(pts),Vector3d.ZAxis);e=w;
            }else throw new ArgumentException("Type géométrique non pris en charge : "+type);
            e.Layer="0";e.ColorIndex=256;e.LineWeight=LineWeight.ByLayer;e.TransformBy(matrix);if((string?)Value(shape,6)=="JHR_CACHE"){var db=HostApplicationServices.WorkingDatabase;using var tr=db.TransactionManager.StartOpenCloseTransaction();var types=(LinetypeTable)tr.GetObject(db.LinetypeTableId,OpenMode.ForRead);if(types.Has("JHR_CACHE"))e.LinetypeId=types["JHR_CACHE"];}entities.Add(e);
        }
        try{var matrix=Matrix3d.Rotation(angle,Vector3d.ZAxis,Point3d.Origin);foreach(var shape in Items(result.Shapes))Add(shape,matrix);return entities;}catch{foreach(var e in entities)e.Dispose();throw;}
    }
}
