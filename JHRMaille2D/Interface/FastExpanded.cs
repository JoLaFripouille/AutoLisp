using static JhrMailleUi.GeometryRuntime;
namespace JhrMailleUi;
internal static class FastExpanded
{
    readonly record struct P(double X,double Y){public static P operator+(P a,P b)=>new(a.X+b.X,a.Y+b.Y);public static P operator-(P a,P b)=>new(a.X-b.X,a.Y-b.Y);public static P operator*(P a,double s)=>new(a.X*s,a.Y*s);internal double Length=>Math.Sqrt(X*X+Y*Y);}
    readonly record struct Box(double X0,double Y0,double X1,double Y1){internal bool Overlaps(Box b)=>X0<=b.X1&&b.X0<=X1&&Y0<=b.Y1&&b.Y0<=Y1;}
    static P[] Points(object? value)=>Items(value).Select(v=>{var n=Items(v).Select(D).ToArray();return new P(n[0],n[1]);}).ToArray();
    static object? Values(IEnumerable<P> ps)=>L(ps.Select(p=>L(p.X,p.Y)).ToArray());
    static Box Bounds(P[] ps)=>new(ps.Min(p=>p.X),ps.Min(p=>p.Y),ps.Max(p=>p.X),ps.Max(p=>p.Y));
    static double Cross(P a,P b)=>a.X*b.Y-a.Y*b.X;
    static double Dot(P a,P b)=>a.X*b.X+a.Y*b.Y;
    static IEnumerable<(P,P)> Edges(P[] ps){for(int i=0;i<ps.Length;i++)yield return(ps[i],ps[(i+1)%ps.Length]);}
    static bool Inside(P p,P[] ps){bool odd=false;foreach(var (a,b) in Edges(ps)){var v=b-a;double vv=Dot(v,v);if(vv>1e-18&&(p-(a+v*Math.Clamp(Dot(p-a,v)/vv,0,1))).Length<1e-6)return true;if((a.Y>p.Y)!=(b.Y>p.Y)&&p.X<a.X+(p.Y-a.Y)*(b.X-a.X)/(b.Y-a.Y))odd=!odd;}return odd;}
    static double? Hit(P a,P b,P c,P d){var r=b-a;var s=d-c;double det=Cross(r,s);if(Math.Abs(det)<=1e-10)return null;double t=Cross(c-a,s)/det,u=Cross(c-a,r)/det;return t>=-1e-8&&t<=1.00000001&&u>=-1e-8&&u<=1.00000001?Math.Clamp(t,0,1):null;}
    static List<(P,P)> Clip(P a,P b,P[] poly){var cuts=new List<double>{0,1};foreach(var(c,d)in Edges(poly)){var t=Hit(a,b,c,d);if(t.HasValue)cuts.Add(t.Value);}var ts=cuts.Distinct().Order().ToArray();var result=new List<(P,P)>();for(int i=0;i<ts.Length-1;i++)if(ts[i+1]-ts[i]>1e-8&&Inside(a+(b-a)*((ts[i]+ts[i+1])/2),poly))result.Add((a+(b-a)*ts[i],a+(b-a)*ts[i+1]));return result;}
    internal static object? Generate(GeometryRuntime g,object? polygon,object? options)
    {
        var poly=Points(polygon);var opts=Items(options).Select(D).ToArray();var box=Bounds(poly);double sx=opts[0]/2,sy=opts[1]/2;var basis=Points(g.Invoke("rf:expanded-hole",options));int c0=(int)Math.Floor(box.X0/sx)-1,c1=(int)(box.X1/sx)+2,r0=(int)Math.Floor(box.Y0/sy)-1,r1=(int)(box.Y1/sy)+2;
        if((long)(c1-c0+1)*(r1-r0+1)>15000)throw new ArgumentException("Motif trop dense : diviser le panneau.");var holes=new List<(Box,P[])>();
        void Draw(P[] ps,bool closed){g.Invoke("wm:draw",Values(ps),0d,closed?true:null);if(!closed)g.Root.Set("rf:count",D(g.Root.Get("rf:count"))+1);}
        void Edge(P a,P b){if((b-a).Length>1e-7)Draw([a,b],false);}
        for(int row=r0;row<=r1;row++)for(int col=c0;col<=c1;col++)if((row+col)%2!=0){var centre=new P(col*sx,row*sy);var hole=basis.Select(q=>q+centre).ToArray();var hb=Bounds(hole);if(!hb.Overlaps(box))continue;holes.Add((hb,hole));bool fits=hole.All(p=>Inside(p,poly));if(fits)foreach(var(a,b)in Edges(hole))if((b-a).Length-Clip(a,b,poly).Sum(seg=>(seg.Item2-seg.Item1).Length)>1e-7){fits=false;break;}if(fits){Draw(hole,true);g.Root.Set("rf:count",D(g.Root.Get("rf:count"))+1);}else foreach(var(a,b)in Edges(hole))foreach(var(p,q)in Clip(a,b,poly))Edge(p,q);}
        foreach(var(a,b)in Edges(poly)){var vector=b-a;double size=Dot(vector,vector);var eb=Bounds([a,b]);var spans=new List<(double,double)>();foreach(var(hb,hole)in holes)if(hb.Overlaps(eb))foreach(var(p,q)in Clip(a,b,hole)){double lo=Dot(p-a,vector)/size,hi=Dot(q-a,vector)/size;if(hi-lo>1e-8)spans.Add((Math.Max(0,lo),Math.Min(1,hi)));}double cursor=0;foreach(var(lo,hi)in spans.OrderBy(p=>p.Item1)){if(lo>cursor)Edge(a+vector*cursor,a+vector*lo);cursor=Math.Max(cursor,hi);}if(cursor<1)Edge(a+vector*cursor,b);}
        return null;
    }
}
