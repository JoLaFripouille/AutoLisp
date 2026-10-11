namespace JhrMailleUi;

/// <summary>Navigation extensible de JHRMAILLE. Chaque outil conserve sa propre page.</summary>
public sealed class ToolNavigation : UserControl
{
    readonly FlowLayoutPanel items=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    readonly Dictionary<string,Button> buttons=new();
    public string? SelectedKey {get;private set;}
    public event Action<string>? ToolSelected;
    public ToolNavigation()
    {
        BackColor=UiTheme.Nav;ForeColor=Color.White;Padding=new(14,26,14,20);items.BackColor=BackColor;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};
        layout.RowStyles.Add(new(SizeType.Absolute,46));layout.RowStyles.Add(new(SizeType.Absolute,36));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,50));
        layout.Controls.Add(new Label{Text="JHR  /  Panneaux",AutoSize=true,Font=new Font("Segoe UI",13,FontStyle.Bold),ForeColor=Color.White},0,0);
        layout.Controls.Add(new Label{Text="REMPLISSAGE",AutoSize=true,ForeColor=Color.FromArgb(179,205,207),Font=new Font("Segoe UI",9,FontStyle.Bold)},0,1);
        layout.Controls.Add(items,0,2);layout.Controls.Add(new Label{Text="2D paramétrique\nAperçu en direct",AutoSize=true,ForeColor=Color.FromArgb(133,156,174),Font=new Font("Segoe UI",9)},0,3);Controls.Add(layout);
        items.SizeChanged+=(_,_)=>{foreach(var button in buttons.Values)button.Width=Math.Max(100,items.ClientSize.Width-16);};
    }
    public void AddTool(string key,string title)
    {
        if(buttons.ContainsKey(key))throw new ArgumentException("Cet outil existe déjà.",nameof(key));
        var button=new NavButton{Key=key,Text=title,Height=68,Width=178,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,
            Padding=new(47,0,8,0),Margin=new(0,0,0,10),Font=new Font("Segoe UI",10,FontStyle.Regular),Cursor=Cursors.Hand,
            UseVisualStyleBackColor=false,DialogResult=DialogResult.None,AccessibleName=title};
        button.FlatAppearance.BorderSize=0;button.Click+=(_,_)=>SelectTool(key);
        buttons.Add(key,button);items.Controls.Add(button);button.Width=Math.Max(100,items.ClientSize.Width-16);if(SelectedKey==null)SelectTool(key);
    }
    public void SelectTool(string key)
    {
        if(!buttons.ContainsKey(key))throw new ArgumentException("Outil inconnu.",nameof(key));
        bool changed=SelectedKey!=key;SelectedKey=key;
        foreach(var item in buttons){bool selected=item.Key==key;((NavButton)item.Value).Selected=selected;item.Value.Invalidate();item.Value.BackColor=selected?Color.FromArgb(221,237,232):BackColor;
            item.Value.ForeColor=selected?Color.FromArgb(24,60,52):Color.White;item.Value.FlatAppearance.MouseOverBackColor=selected?Color.FromArgb(208,228,219):Color.FromArgb(47,76,80);}
        if(changed)ToolSelected?.Invoke(key);
    }
}
