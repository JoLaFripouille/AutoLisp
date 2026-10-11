using System.Drawing.Drawing2D;
using System.ComponentModel;
namespace JhrMailleUi;
public sealed class PatternPreview : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public FillTool Tool{get;set;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public PatternSettings Settings{get;set;}=new(30,30,3,1);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool Dimensions{get;set;}
    public PatternPreview(){DoubleBuffered=true;BackColor=Color.FromArgb(251,251,249);ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);PatternRenderer.Paint(e.Graphics,ClientRectangle,Tool,Settings,Dimensions);}
}
public static class PatternRenderer
{
    public static string Designation(FillTool tool,PatternSettings s)=>tool switch{
        FillTool.Square=>$"Grillage soudé {s.A:g} × {s.A:g} mm libre · fil Ø{s.Wire:g} · entraxe {s.A+s.Wire:g} mm",
        FillTool.Expanded=>$"Déployé ML {s.A:g} × MW {s.B:g} · lanière {s.Wire:g} · ép. {s.Thickness:g} mm",
        _=>$"{(s.Pattern==2?"C":"R")}{s.A:g} {(s.Pattern==1?"T":"U")}{s.B:g} · ép. {s.Thickness:g} · bordure ≥ {s.Margin:g} mm"};
    public static void Paint(Graphics g,Rectangle view,FillTool tool,PatternSettings s,bool dimensions=false)
    {
        g.Clear(Color.FromArgb(251,251,249));g.SmoothingMode=SmoothingMode.AntiAlias;var saved=g.Save();g.SetClip(view);
        g.TranslateTransform(view.Width/2f,view.Height/2f);g.RotateTransform((float)-s.Angle);
        float scale=(float)Math.Min((view.Width-45)/(tool==FillTool.Expanded?s.A*3.5:(tool==FillTool.Square?(s.A+s.Wire)*4:s.B*7)),(view.Height-45)/(tool==FillTool.Expanded?s.B*5:(tool==FillTool.Square?(s.A+s.Wire)*4:s.B*7)));
        void Metal(PointF a,PointF b,float width,bool relief=false){using var shadow=new Pen(Color.FromArgb(42,45,48),width){LineJoin=LineJoin.Miter};using var bright=new Pen(Color.FromArgb(relief?157:193,198,196),Math.Max(.4f,width*.64f));g.DrawLine(shadow,a,b);g.DrawLine(bright,new PointF(a.X-.5f,a.Y-.5f),new PointF(b.X-.5f,b.Y-.5f));}
        if(tool==FillTool.Square){float pitch=(float)(s.A+s.Wire)*scale,d=(float)s.Wire*scale;for(int k=-7;k<=7;k++)Metal(new(k*pitch,-10000),new(k*pitch,10000),d);for(int k=-7;k<=7;k++)Metal(new(-10000,k*pitch),new(10000,k*pitch),d); }
        else if(tool==FillTool.Expanded){float sx=(float)s.A*scale/2,sy=(float)s.B*scale/2,d=(float)s.Wire*scale;for(int row=-10;row<=10;row++)for(int col=-10;col<=10;col++){if((row+col)%2!=0)continue;foreach(int sign in new[]{-1,1})Metal(new(col*sx,row*sy),new((col+1)*sx,(row+sign)*sy),d,s.Pattern==1);} }
        else {
            var r=new RectangleF(-view.Width/2f,-view.Height/2f,view.Width,view.Height);using var metal=new LinearGradientBrush(r,Color.FromArgb(211,214,213),Color.FromArgb(139,145,144),35);g.FillRectangle(metal,r);
            float pitch=(float)s.B*scale,dy=pitch*(s.Pattern==1?.8660254f:1),size=(float)s.A*scale;using var edge=new Pen(Color.FromArgb(97,103,102),Math.Max(.7f,(float)s.Thickness*scale*.1f));using var hole=new SolidBrush(Color.FromArgb(251,251,249));
            for(int row=-14;row<=14;row++)for(int col=-14;col<=14;col++){float x=col*pitch+(s.Pattern==1&&Math.Abs(row%2)==1?pitch/2:0),y=row*dy;var b=new RectangleF(x-size/2,y-size/2,size,size);if(s.Pattern==2){g.FillRectangle(hole,b);g.DrawRectangle(edge,b.X,b.Y,b.Width,b.Height);}else{g.FillEllipse(hole,b);g.DrawEllipse(edge,b);}}
        }
        if(dimensions){using var pen=new Pen(Color.FromArgb(35,103,116),1){DashStyle=DashStyle.Dash};using var brush=new SolidBrush(Color.FromArgb(35,103,116));using var font=new Font("Segoe UI",9);float length=(float)(tool==FillTool.Square?s.A+s.Wire:tool==FillTool.Expanded?s.A:s.B)*scale;g.DrawLine(pen,0,0,length,0);g.DrawString($"{length/scale:g} mm entre axes",font,brush,4,8);}
        g.Restore(saved);
    }
}
