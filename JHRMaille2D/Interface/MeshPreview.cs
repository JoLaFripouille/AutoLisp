using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace JhrMailleUi;
public sealed class MeshPreview : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MeshSettings Settings {get;set;}=new();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Dimensions {get;set;}
    public MeshPreview(){DoubleBuffered=true;BackColor=Color.FromArgb(251,251,249);ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);MeshRenderer.Paint(e.Graphics,ClientRectangle,Settings,Dimensions);}
}
public static class MeshRenderer
{
    static PointF Add(PointF a,PointF b)=>new(a.X+b.X,a.Y+b.Y);
    static PointF Mul(PointF a,float f)=>new(a.X*f,a.Y*f);
    static PointF Curve(PointF a,PointF b,PointF c,PointF d,float t)
    {float u=1-t;return Add(Add(Mul(a,u*u*u),Mul(b,3*u*u*t)),Add(Mul(c,3*u*t*t),Mul(d,t*t*t)));}
    static PointF[] Bezier(PointF a,PointF b,PointF c,PointF d)=>Enumerable.Range(0,49).Select(i=>Curve(a,b,c,d,i/48f)).ToArray();
    public static void Paint(Graphics g,Rectangle view,MeshSettings s,bool dimensions=false)
    {
        g.Clear(Color.FromArgb(251,251,249));g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        var save=g.Save();g.SetClip(view);g.TranslateTransform(view.Width/2f,view.Height/2f);
        g.RotateTransform((float)-s.Angle);
        float angle=(float)(Math.Abs(s.Angle)%180*Math.PI/180);
        float spanX=(float)(2.0*s.Width),spanY=(float)(2.0*s.Height);
        float scale=Math.Min((view.Width-50)/(Math.Abs(MathF.Cos(angle))*spanX+Math.Abs(MathF.Sin(angle))*spanY),
            (view.Height-50)/(Math.Abs(MathF.Sin(angle))*spanX+Math.Abs(MathF.Cos(angle))*spanY));
        float sx=(float)s.Width*scale/2,sy=(float)s.Height*scale/2;
        float d=Math.Max(.45f,(float)s.Wire*scale);
        float half=(float)(s.Woven?s.Wire*2.5:s.SleeveLength/2)*scale;
        float tangent=(float)(s.Woven?Math.Min(8,s.Width*.16):8)*scale;
        g.TranslateTransform(sx,0);
        var nodes=new List<PointF>();
        for(int row=-5;row<=5;row++)for(int col=-6;col<=6;col++)
        {
            if((row+col)%2!=0)continue;
            PointF p=new(col*sx,row*sy);nodes.Add(p);
            foreach(int sign in new[]{-1,1})
            {
                PointF q=new((col+1)*sx,(row+sign)*sy),a=new(p.X+half,p.Y),b=new(q.X-half,q.Y);
                Rope(g,Bezier(a,new(a.X+tangent,a.Y),new(b.X-tangent,b.Y),b),d);
            }
        }
        foreach(var p in nodes)
        {
            if(s.Woven)Weave(g,p,half,d);
            else Sleeve(g,p,(float)s.SleeveLength*scale,(float)s.SleeveHeight*scale,d);
        }
        if(dimensions)
        {
            g.TranslateTransform(-sx,0);
            using var pen=new Pen(Color.FromArgb(90,47,121,132),1){DashStyle=DashStyle.Dash};
            g.DrawLine(pen,-sx,0,sx,0);g.DrawLine(pen,0,-sy,0,sy);
            using var brush=new SolidBrush(Color.FromArgb(28,91,105));using var font=new Font("Segoe UI",9);
            g.DrawString($"X {s.Width:g} mm",font,brush,-sx/2,6);g.DrawString($"Y {s.Height:g} mm",font,brush,5,-sy/2);
        }
        g.Restore(save);
    }
    static void Stroke(Graphics g,PointF[] points,Color color,float width)
    {
        using var p=new Pen(color,Math.Max(.18f,width)){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        g.DrawLines(p,points);
    }
    static void Rope(Graphics g,PointF[] points,float d)
    {
        Stroke(g,points.Select(p=>new PointF(p.X+.8f,p.Y+1.2f)).ToArray(),Color.FromArgb(22,47,47,48),d+.8f);
        Stroke(g,points,Color.FromArgb(117,120,120),d);
        Stroke(g,points,Color.FromArgb(190,192,190),d*.76f);
        float length=0;
        for(int strand=0;strand<6;strand++)
        {
            var line=new PointF[points.Length];length=0;
            for(int i=0;i<points.Length;i++)
            {
                var a=points[Math.Max(0,i-1)];var b=points[Math.Min(points.Length-1,i+1)];
                float dx=b.X-a.X,dy=b.Y-a.Y,l=MathF.Sqrt(dx*dx+dy*dy);if(l<.0001f)l=1;
                if(i>0){float xx=points[i].X-points[i-1].X,yy=points[i].Y-points[i-1].Y;length+=MathF.Sqrt(xx*xx+yy*yy);}
                float phase=length/Math.Max(2,d*3.2f)*MathF.PI*2+strand*MathF.PI/3;
                float offset=MathF.Sin(phase)*d*.35f;
                line[i]=new(points[i].X-dy/l*offset,points[i].Y+dx/l*offset);
            }
            Stroke(g,line,strand%2==0?Color.FromArgb(110,114,113):Color.FromArgb(226,227,224),Math.Max(.25f,d*.13f));
        }
    }
    static void Weave(Graphics g,PointF p,float half,float d)
    {
        // The two complete ropes exchange foreground at each half turn.
        for(int part=0;part<4;part++)
        {
            foreach(int sign in (part%2==0?new[]{-1,1}:new[]{1,-1}))
            {
                var pts=Enumerable.Range(0,17).Select(i=>
                {
                    float t=(part+i/16f)/4;
                    return new PointF(p.X-half+2*half*t,p.Y+sign*d*.35f*MathF.Sin(t*4*MathF.PI));
                }).ToArray();
                Rope(g,pts,d);
            }
        }
    }
    static void Sleeve(Graphics g,PointF p,float w,float h,float d)
    {
        var r=new RectangleF(p.X-w/2,p.Y-h/2,w,h);
        using var path=new GraphicsPath();float rad=Math.Min(1.8f,Math.Min(w,h)*.2f);
        path.AddArc(r.Left,r.Top,rad*2,rad*2,180,90);path.AddArc(r.Right-2*rad,r.Top,2*rad,2*rad,270,90);
        path.AddArc(r.Right-2*rad,r.Bottom-2*rad,2*rad,2*rad,0,90);path.AddArc(r.Left,r.Bottom-2*rad,2*rad,2*rad,90,90);path.CloseFigure();
        using var brush=new LinearGradientBrush(r,Color.FromArgb(104,108,108),Color.FromArgb(230,232,230),90);
        brush.InterpolationColors=new ColorBlend{Colors=[Color.FromArgb(106,109,109),Color.FromArgb(240,241,237),Color.FromArgb(160,163,161),Color.FromArgb(219,222,218),Color.FromArgb(92,97,97)],Positions=[0,.25f,.55f,.8f,1]};
        g.FillPath(brush,path);using var edge=new Pen(Color.FromArgb(102,108,105),.8f);g.DrawPath(edge,path);
        g.DrawLine(edge,r.Left+w*.14f,r.Top+h*.12f,r.Left+w*.14f,r.Bottom-h*.12f);
        g.DrawLine(edge,r.Right-w*.14f,r.Top+h*.12f,r.Right-w*.14f,r.Bottom-h*.12f);
    }
}
