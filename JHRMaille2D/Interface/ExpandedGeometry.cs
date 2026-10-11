namespace JhrMailleUi;
public readonly record struct ExpandedPoint(double X,double Y)
{
 public static ExpandedPoint operator +(ExpandedPoint a,ExpandedPoint b)=>new(a.X+b.X,a.Y+b.Y);
 public static ExpandedPoint operator -(ExpandedPoint a,ExpandedPoint b)=>new(a.X-b.X,a.Y-b.Y);
 public static ExpandedPoint operator *(ExpandedPoint a,double t)=>new(a.X*t,a.Y*t);
 public double Length=>Math.Sqrt(X*X+Y*Y);
 public PointF Pixel(float scale)=>new((float)X*scale,(float)Y*scale);
}
public static class ExpandedGeometry
{
 public static double Bond(PatternSettings s)=>s.Margin>0?s.Margin:Math.Min(.35*s.A,Math.Max(2*s.Wire,Math.Min(.12*s.A,.25*s.B)));
 static double Cross(ExpandedPoint a,ExpandedPoint b)=>a.X*b.Y-a.Y*b.X;
 public static ExpandedPoint[] Hole(PatternSettings s)
 {
  double sx=s.A/2,sy=s.B/2,half=Bond(s)/2;
  ExpandedPoint[] points=[new(-(sx-half),0),new(-half,sy),new(half,sy),new(sx-half,0),new(half,-sy),new(-half,-sy)];
  Array.Reverse(points);
  var lines=points.Select((p,i)=>{
   var d=points[(i+1)%points.Length]-p;var n=new ExpandedPoint(-d.Y,d.X)*(s.Wire/2/d.Length);
   return (P:p+n,D:d);
  }).ToArray();
  var inset=new ExpandedPoint[points.Length];
  for(int i=0;i<points.Length;i++){
   var previous=lines[(i+lines.Length-1)%lines.Length];var current=lines[i];
   double t=Cross(current.P-previous.P,current.D)/Cross(previous.D,current.D);
   inset[i]=previous.P+previous.D*t;
  }
  var rounded=new List<ExpandedPoint>();
  for(int i=0;i<inset.Length;i++){
   var cur=inset[i];var incoming=inset[(i+inset.Length-1)%inset.Length]-cur;var outgoing=inset[(i+1)%inset.Length]-cur;
   double offset=Math.Min(.65*s.Wire,Math.Min(.2*incoming.Length,.2*outgoing.Length));
   var a=cur+incoming*(offset/incoming.Length);var b=cur+outgoing*(offset/outgoing.Length);
   for(int j=0;j<=4;j++){double t=j/4d;rounded.Add(a*((1-t)*(1-t))+cur*(2*t*(1-t))+b*(t*t));}
  }
  return rounded.ToArray();
 }
 public static void Paint(Graphics g,Rectangle view,PatternSettings s,float scale)
 {
  var extent=new RectangleF(-view.Width-view.Height,-view.Width-view.Height,2*(view.Width+view.Height),2*(view.Width+view.Height));
  using var path=new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Alternate);path.AddRectangle(extent);
  var holes=new List<PointF[]>();var shape=Hole(s);float sx=(float)s.A*scale/2,sy=(float)s.B*scale/2;
  int cols=(int)Math.Ceiling((view.Width+view.Height)/sx)+2,rows=(int)Math.Ceiling((view.Width+view.Height)/sy)+2;
  if((long)(2*cols+1)*(2*rows+1)>9000){using var f=new Font("Segoe UI",10);using var b=new SolidBrush(Color.DimGray);g.DrawString("Maille très allongée : aperçu de détail indisponible.",f,b,-view.Width/2f+20,0);return;}
  // Close material preview: centre pitches remain exact even when the detail is cropped.
  for(int row=-rows;row<=rows;row++)for(int col=-cols;col<=cols;col++){
   if((row+col)%2==0)continue;
   var hole=shape.Select(p=>new PointF((float)p.X*scale+col*sx,(float)p.Y*scale+row*sy)).ToArray();
   path.AddPolygon(hole);holes.Add(hole);
  }
  using var background=new SolidBrush(Color.FromArgb(20,25,25));g.FillRectangle(background,extent);
  using var metal=new System.Drawing.Drawing2D.LinearGradientBrush(extent,Color.FromArgb(205,212,210),Color.FromArgb(124,135,132),35);g.FillPath(metal,path);
  using var edge=new Pen(Color.FromArgb(s.Pattern==1?102:147,158,154),Math.Max(.7f,scale*.12f));
  using var highlight=new Pen(Color.FromArgb(213,222,218),Math.Max(.6f,scale*.08f));
  foreach(var hole in holes){g.DrawPolygon(edge,hole);if(s.Pattern==1)for(int i=0;i<hole.Length;i++){var a=hole[i];var b=hole[(i+1)%hole.Length];if(b.X>a.X)g.DrawLine(highlight,new PointF(a.X-.6f,a.Y-.6f),new PointF(b.X-.6f,b.Y-.6f));}}
 }
}
