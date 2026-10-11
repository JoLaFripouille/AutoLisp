using System.Drawing.Drawing2D;
namespace JhrMailleUi;

internal static class UiTheme
{
    internal static readonly Color Canvas=Color.FromArgb(245,247,250),Ink=Color.FromArgb(31,43,55),Muted=Color.FromArgb(106,120,134),Border=Color.FromArgb(222,229,235),Accent=Color.FromArgb(15,118,110),Soft=Color.FromArgb(225,243,239),Nav=Color.FromArgb(24,36,47);
    internal static GraphicsPath Rounded(RectangleF r,float radius=10){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    internal static void Button(Button b,bool primary=false){b.AutoSize=false;b.Size=new(primary?132:100,40);b.MinimumSize=b.Size;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Cursor=Cursors.Hand;b.UseVisualStyleBackColor=false;b.BackColor=primary?Accent:Color.White;b.ForeColor=primary?Color.White:Ink;b.Font=new Font("Segoe UI",10,primary?FontStyle.Bold:FontStyle.Regular);if(b is ModernButton modern)modern.Primary=primary;}
    internal static void Style(Control root){foreach(Control c in root.Controls){
        if(c is ToolNavigation)continue;
        if(c is NumericUpDown n){n.BorderStyle=BorderStyle.FixedSingle;n.BackColor=Color.White;n.ForeColor=Ink;n.Font=new Font("Segoe UI",11);n.Margin=new(4,6,2,6);n.TextAlign=HorizontalAlignment.Right;}
        else if(c is ComboBox box){box.FlatStyle=FlatStyle.Flat;box.BackColor=Color.White;box.ForeColor=Ink;box.Font=new Font("Segoe UI",10);box.Margin=new(0,5,2,8);box.IntegralHeight=false;box.MaxDropDownItems=10;}
        else if(c is CheckBox check){check.ForeColor=Ink;check.Margin=new(0,4,0,2);}
        else if(c is Label label&&label.Font.Size<15){if(label.ForeColor==Color.DimGray)label.ForeColor=Muted;else if(label.ForeColor==SystemColors.ControlText)label.ForeColor=Ink;}
        Style(c);
    }}
    internal static Control Header(string title,string subtitle){var p=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};p.Controls.Add(new Label{Text=title,AutoSize=true,Location=new(0,0),Font=new Font("Segoe UI",19,FontStyle.Bold),ForeColor=Ink});p.Controls.Add(new Label{Text=subtitle,AutoSize=true,Location=new(0,36),Font=new Font("Segoe UI",9),ForeColor=Muted});return p;}
    internal static void Section(TableLayoutPanel fields,ref int row,string title){var label=new Label{Text=title.ToUpperInvariant(),AutoSize=true,Font=new Font("Segoe UI",9,FontStyle.Bold),ForeColor=Accent,Padding=new(0,row==0?0:18,0,7),Margin=new(0,3,0,0)};fields.Controls.Add(label,0,row++);fields.SetColumnSpan(label,2);}
}

public class ModernButton : Button
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]public bool Primary{get;set;}
    bool hover,down;
    public ModernButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;down=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){down=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor??UiTheme.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-3,Height-3);using var shape=UiTheme.Rounded(r,8);var bg=!Enabled?UiTheme.Border:Primary?(down?Color.FromArgb(10,88,82):hover?Color.FromArgb(12,100,93):UiTheme.Accent):(hover?UiTheme.Soft:Color.White);using var fill=new SolidBrush(bg);e.Graphics.FillPath(fill,shape);if(!Primary){using var pen=new Pen(UiTheme.Border);e.Graphics.DrawPath(pen,shape);}TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,!Enabled?UiTheme.Muted:Primary?Color.White:UiTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused&&ShowFocusCues){using var pen=new Pen(Primary?Color.White:UiTheme.Accent){DashStyle=DashStyle.Dot};e.Graphics.DrawRectangle(pen,5,5,Width-11,Height-11);}}
}

internal sealed class NavButton : Button
{
    internal string Key="";internal bool Selected;bool hover;
    internal NavButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(UiTheme.Nav);g.SmoothingMode=SmoothingMode.AntiAlias;using var path=UiTheme.Rounded(new(0,0,Width-1,Height-1));using var bg=new SolidBrush(Selected?UiTheme.Soft:hover?Color.FromArgb(39,55,69):UiTheme.Nav);g.FillPath(bg,path);var color=Selected?UiTheme.Accent:Color.FromArgb(191,205,217);using var pen=new Pen(color,1.4f);float x=15,y=Height/2f-10;
        if(Key.StartsWith("cadre-")){g.DrawRectangle(pen,x,y,20,20);if(Key=="cadre-Rolled"){g.DrawLine(pen,x+4,y+5,x+16,y+5);g.DrawLine(pen,x+10,y+5,x+10,y+15);g.DrawLine(pen,x+4,y+15,x+16,y+15);}else{g.DrawRectangle(pen,x+4,y+4,12,12);if(Key=="cadre-SheetDoor")g.DrawLine(pen,x+4,y+16,x+16,y+4);}}
        else if(Key=="tole-perforee"){for(int i=0;i<3;i++)for(int j=0;j<3;j++)g.DrawEllipse(pen,x+i*7,y+j*7,3.6f,3.6f);}
        else if(Key=="grillage-carre"){for(int i=0;i<4;i++){g.DrawLine(pen,x+i*6,y,x+i*6,y+18);g.DrawLine(pen,x,y+i*6,x+18,y+i*6);}}
        else{for(int i=0;i<2;i++){float xx=x+i*11;g.DrawLines(pen,new PointF[]{new(xx,y+10),new(xx+6,y),new(xx+12,y+10),new(xx+6,y+20),new(xx,y+10)});}if(Key=="metal-deploye"){g.DrawLine(pen,x,y+10,x+4,y+10);g.DrawLine(pen,x+18,y+10,x+23,y+10);}}
        TextRenderer.DrawText(g,Text,Font,new Rectangle(47,3,Width-54,Height-6),Selected?UiTheme.Ink:Color.FromArgb(227,235,241),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);if(Focused&&ShowFocusCues){using var focus=new Pen(color){DashStyle=DashStyle.Dot};g.DrawRectangle(focus,4,4,Width-9,Height-9);}}
}
