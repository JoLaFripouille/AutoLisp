using System.Text.Json;
using System.Text.Json.Serialization;
namespace JhrMailleUi.Frames;

public readonly record struct P2(double X,double Y)
{
    public static P2 operator +(P2 a,P2 b)=>new(a.X+b.X,a.Y+b.Y);
    public static P2 operator -(P2 a,P2 b)=>new(a.X-b.X,a.Y-b.Y);
}
public sealed record FramePath(P2[] Points,double[]? Bulges=null,bool Closed=false)
{
    public FramePath Map(Func<P2,P2> f,bool mirror=false)=>new(Points.Select(f).ToArray(),Bulges?.Select(b=>mirror?-b:b).ToArray(),Closed);
    public IEnumerable<P2> Sample()
    {
        if(Points.Length<2)yield break;
        for(int i=0;i<(Closed?Points.Length:Points.Length-1);i++){
            P2 a=Points[i],b=Points[(i+1)%Points.Length];double bulge=Bulges!=null&&i<Bulges.Length?Bulges[i]:0;yield return a;
            if(Math.Abs(bulge)<1e-12)continue;double dx=b.X-a.X,dy=b.Y-a.Y,chord=Math.Sqrt(dx*dx+dy*dy);
            if(chord<1e-9)continue;double offset=chord*(1-bulge*bulge)/(4*bulge);var c=new P2((a.X+b.X)/2-dy/chord*offset,(a.Y+b.Y)/2+dx/chord*offset);
            double radius=chord*(1+bulge*bulge)/(4*Math.Abs(bulge)),start=Math.Atan2(a.Y-c.Y,a.X-c.X),sweep=4*Math.Atan(bulge);
            int steps=Math.Clamp((int)Math.Ceiling(Math.Abs(sweep)/Math.Max(.002,2*Math.Acos(Math.Clamp(1-.01/radius,-1,1)))),2,1000);
            for(int j=1;j<steps;j++){double angle=start+sweep*j/steps;yield return new(c.X+radius*Math.Cos(angle),c.Y+radius*Math.Sin(angle));}
        }
        yield return Closed?Points[0]:Points[^1];
    }
    public static FramePath Rect(double x,double y,double w,double h)=>new([new(x,y),new(x+w,y),new(x+w,y+h),new(x,y+h)],null,true);
    public static FramePath Line(double x,double y,double x2,double y2)=>new([new(x,y),new(x2,y2)]);
}
public sealed record FrameProfile(string Id,string Name,string Family,FramePath[] Paths,string Source="")
{
    [JsonIgnore] public double Width=>Paths.SelectMany(p=>p.Sample()).Max(p=>p.X);
    [JsonIgnore] public double Depth=>Paths.SelectMany(p=>p.Sample()).Max(p=>p.Y);
    public FrameProfile Orient(int turns,bool mirror)
    {
        turns=((turns%4)+4)%4;FramePath[] paths=Paths.Select(p=>p.Map(q=>{if(mirror)q=new(-q.X,q.Y);for(int i=0;i<turns;i++)q=new(-q.Y,q.X);return q;},mirror)).ToArray();
        var sample=paths.SelectMany(p=>p.Sample()).ToArray();double x=sample.Min(p=>p.X),y=sample.Min(p=>p.Y);return this with{Paths=paths.Select(p=>p.Map(q=>new(q.X-x,q.Y-y))).ToArray()};
    }
    public (double,double)[] MaterialAt(double station)
    {
        // Even/odd intersections through the actual section outlines, including cavities.
        station=Math.Clamp(station,.00001,Width-.00001);var hits=new List<double>();
        foreach(var path in Paths){var pts=path.Sample().ToArray();for(int i=0;i<pts.Length-1;i++){var a=pts[i];var b=pts[i+1];if((a.X<=station&&b.X>station)||(b.X<=station&&a.X>station))hits.Add(a.Y+(station-a.X)*(b.Y-a.Y)/(b.X-a.X));}}
        hits.Sort();if(hits.Count%2!=0)throw new ArgumentException("Section non fermée : impossible de calculer la coupe longitudinale du profil "+Name+".");
        return Enumerable.Range(0,hits.Count/2).Select(i=>(hits[2*i],hits[2*i+1])).Where(p=>p.Item2-p.Item1>1e-5).ToArray();
    }
}
public enum FrameTool { Tubes,Joinery,Rolled,SheetDoor }
public sealed record FrameCut(string Id,bool Vertical,double Station,bool Reverse=false);
public sealed record FrameDesign
{
    public int Schema{get;init;}=1;public FrameTool Tool{get;init;}=FrameTool.Tubes;
    public double Width{get;init;}=1200;public double Height{get;init;}=2000;
    public FrameProfile? Profile{get;init;}public FrameProfile? LeafProfile{get;init;}
    public int Turns{get;init;}public bool Mirror{get;init;}public bool Miter{get;init;}=true;
    public double[] Mullions{get;init;}=[];public double[] Transoms{get;init;}=[];
    public double Gap{get;init;}=5;public double Sheet{get;init;}=2;public bool TwoSheets{get;init;}
    public double LabelHeight{get;init;}=20;
    public FrameCut[] Cuts{get;init;}=[];
    public string? Validate()
    {
        if(Schema!=1||!Enum.IsDefined(Tool))return "Format de cadre inconnu.";
        if(Profile==null||Profile.Paths.Length==0)return "Importer et sélectionner une section de profil.";
        bool Bad(FrameProfile? profile)=>profile!=null&&(profile.Paths.Length>3000||profile.Paths.Sum(p=>p.Points.Length)>25000||profile.Paths.Any(p=>p.Points.Length<2||p.Points.Any(q=>!double.IsFinite(q.X)||!double.IsFinite(q.Y)||Math.Abs(q.X)>10000||Math.Abs(q.Y)>10000)||p.Bulges?.Any(b=>!double.IsFinite(b)||Math.Abs(b)>10000)==true));
        if(Bad(Profile)||Bad(LeafProfile))return "Géométrie de section invalide.";
        if(!double.IsFinite(LabelHeight)||LabelHeight<=0||LabelHeight>500)return "Hauteur de repère invalide.";
        if(!double.IsFinite(Width)||!double.IsFinite(Height)||Width<=0||Height<=0||Width>50000||Height>50000)return "Dimensions du cadre invalides.";
        var p=Profile.Orient(Turns,Mirror);double face=p.Width;
        if(face<=0||p.Depth<=0||face*2>=Width||face*2>=Height)return "Le profil est trop large pour le cadre.";
        if(Mullions.Length+Transoms.Length>60||Cuts.Length>20)return "Limiter le cadre à 60 divisions et 20 coupes.";
        if(Mullions.Concat(Transoms).Any(x=>!double.IsFinite(x)))return "Position de division invalide.";
        double inset=face;if(Tool==FrameTool.SheetDoor){if(LeafProfile==null)return "Choisir la section d’ossature du vantail.";if(!double.IsFinite(Gap)||Gap<=0||!double.IsFinite(Sheet)||Sheet<=0||Sheet>20)return "Jeu ou épaisseur de tôle invalide.";face=LeafProfile.Orient(Turns,Mirror).Width;inset+=Gap+face;if(inset*2>=Width||inset*2>=Height)return "Le vantail ne tient pas dans le dormant.";}
        bool Valid(double[] xs,double size)=>xs.Order().Zip(xs.Order().Skip(1),(a,b)=>b-a).All(x=>x>face)&&xs.All(x=>x>inset+face/2&&x<size-inset-face/2);
        if(!Valid(Mullions,Width)||!Valid(Transoms,Height))return "Les divisions se chevauchent ou rencontrent le bord du cadre.";
        if(Tool==FrameTool.SheetDoor&&Sheet>LeafProfile!.Orient(Turns,Mirror).Depth/(TwoSheets?2:1))return "Tôle trop épaisse pour la profondeur du vantail.";
        if(Cuts.Select(c=>c.Id).Distinct().Count()!=Cuts.Length||Cuts.Any(c=>!System.Text.RegularExpressions.Regex.IsMatch(c.Id,@"^[A-Z][A-Z0-9]{0,5}$")||!double.IsFinite(c.Station)||c.Station<=0||c.Station>=(c.Vertical?Width:Height)))return "Ligne de coupe invalide.";
        return null;
    }
    public FrameDesign Portable()=>this with{Profile=Profile==null?null:Profile with{Source=""},LeafProfile=LeafProfile==null?null:LeafProfile with{Source=""}};
    public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,Converters={new JsonStringEnumConverter()}};
}
public sealed record FrameMember(bool Horizontal,double Start,double End,double Side,FrameProfile Profile,double DepthOffset=0);
public static class FrameGeometry
{
    public static List<FrameMember> Members(FrameDesign d)
    {
        var list=new List<FrameMember>();var p=d.Profile!.Orient(d.Turns,d.Mirror);
        void Ring(double x,double y,double w,double h,FrameProfile s){double f=s.Width;var opposite=s with{Paths=s.Paths.Select(p=>p.Map(q=>new(f-q.X,q.Y),true)).ToArray()};list.Add(new(true,x,x+w,y,s));list.Add(new(true,x,x+w,y+h-f,opposite));list.Add(new(false,y+f,y+h-f,x,s));list.Add(new(false,y+f,y+h-f,x+w-f,opposite));}
        Ring(0,0,d.Width,d.Height,p);double inset=p.Width;
        if(d.Tool==FrameTool.SheetDoor){inset+=d.Gap;var leaf=d.LeafProfile!.Orient(d.Turns,d.Mirror);Ring(inset,inset,d.Width-2*inset,d.Height-2*inset,leaf);inset+=leaf.Width;p=leaf;}
        foreach(double x in d.Mullions)list.Add(new(false,inset,d.Height-inset,x-p.Width/2,p));
        foreach(double y in d.Transoms){var borders=new[]{inset}.Concat(d.Mullions.Order().SelectMany(x=>new[]{x-p.Width/2,x+p.Width/2})).Append(d.Width-inset).ToArray();for(int i=0;i<borders.Length;i+=2)list.Add(new(true,borders[i],borders[i+1],y-p.Width/2,p));}
        return list;
    }
    public static FramePath[] Elevation(FrameDesign d)
    {
        var error=d.Validate();if(error!=null)throw new ArgumentException(error);var shapes=new List<FramePath>();var p=d.Profile!.Orient(d.Turns,d.Mirror);double f=p.Width;
        // Four separate members, with correct 45 degree or square junctions.
        void Ring(double x,double y,double w,double h,double face){
            if(d.Miter){shapes.Add(FramePath.Rect(x,y,w,h));shapes.Add(FramePath.Rect(x+face,y+face,w-2*face,h-2*face));foreach(var q in new[]{(x,y,1,1),(x+w,y,-1,1),(x+w,y+h,-1,-1),(x,y+h,1,-1)})shapes.Add(FramePath.Line(q.Item1,q.Item2,q.Item1+q.Item3*face,q.Item2+q.Item4*face));}
            else{shapes.Add(FramePath.Rect(x,y,w,face));shapes.Add(FramePath.Rect(x,y+h-face,w,face));shapes.Add(FramePath.Rect(x,y+face,face,h-2*face));shapes.Add(FramePath.Rect(x+w-face,y+face,face,h-2*face));}
        }
        Ring(0,0,d.Width,d.Height,f);int skip=4;if(d.Tool==FrameTool.SheetDoor){double a=f+d.Gap;Ring(a,a,d.Width-2*a,d.Height-2*a,d.LeafProfile!.Orient(d.Turns,d.Mirror).Width);skip=8;}
        foreach(var m in Members(d).Skip(skip))shapes.Add(m.Horizontal?FramePath.Rect(m.Start,m.Side,m.End-m.Start,m.Profile.Width):FramePath.Rect(m.Side,m.Start,m.Profile.Width,m.End-m.Start));
        return shapes.ToArray();
    }
    public static FramePath[] Section(FrameDesign d,FrameCut cut)
    {
        var error=d.Validate();if(error!=null)throw new ArgumentException(error);
        // Corner mitres require intersecting the oblique end faces. Do not return a square approximation.
        if(d.Miter){double f=d.Profile!.Orient(d.Turns,d.Mirror).Width,size=cut.Vertical?d.Width:d.Height;
            if(cut.Station<f||cut.Station>size-f)throw new ArgumentException("Coupe dans un angle à 45° : déplacer la ligne au-delà de la largeur du profil, ou choisir des assemblages droits.");
            if(d.Tool==FrameTool.SheetDoor){double a=f+d.Gap,l=d.LeafProfile!.Orient(d.Turns,d.Mirror).Width;
                if(cut.Station>a&&cut.Station<a+l||cut.Station>size-a-l&&cut.Station<size-a)throw new ArgumentException("Coupe dans un angle du vantail à 45° : déplacer la ligne hors de l’angle.");}}
        var paths=new List<FramePath>();double sign=cut.Reverse?-1:1;
        foreach(var m in Members(d)){
            bool crossing=cut.Vertical==m.Horizontal;double face=m.Profile.Width;
            if(crossing){if(cut.Station<=m.Start+1e-6||cut.Station>=m.End-1e-6)continue;
                foreach(var path in m.Profile.Paths)paths.Add(path.Map(q=>cut.Vertical?new(sign*(q.Y+m.DepthOffset),q.X+m.Side):new(q.X+m.Side,sign*(q.Y+m.DepthOffset)),cut.Vertical?!cut.Reverse:cut.Reverse));
            }else{if(cut.Station<=m.Side+1e-6||cut.Station>=m.Side+face-1e-6)continue;foreach(var interval in m.Profile.MaterialAt(cut.Station-m.Side)){
                    var path=cut.Vertical?FramePath.Rect(interval.Item1+m.DepthOffset,m.Start,interval.Item2-interval.Item1,m.End-m.Start):FramePath.Rect(m.Start,interval.Item1+m.DepthOffset,m.End-m.Start,interval.Item2-interval.Item1);
                    paths.Add(path.Map(q=>cut.Vertical?new(sign*q.X,q.Y):new(q.X,sign*q.Y),cut.Reverse));
                }}
        }
        if(d.Tool==FrameTool.SheetDoor){var p=d.Profile!.Orient(d.Turns,d.Mirror);var leaf=d.LeafProfile!.Orient(d.Turns,d.Mirror);double a=p.Width+d.Gap;double low=a,high=(cut.Vertical?d.Width:d.Height)-a;
            if(cut.Station>low&&cut.Station<high){double length=(cut.Vertical?d.Height:d.Width)-2*a;foreach(double z in d.TwoSheets?new[]{-d.Sheet,leaf.Depth}:new[]{-d.Sheet})paths.Add((cut.Vertical?FramePath.Rect(z,a,d.Sheet,length):FramePath.Rect(a,z,length,d.Sheet)).Map(q=>cut.Vertical?new(sign*q.X,q.Y):new(q.X,sign*q.Y),cut.Reverse));}
        }
        if(paths.Count==0)throw new ArgumentException("La ligne "+cut.Id+" ne traverse aucun élément.");return paths.ToArray();
    }
}
