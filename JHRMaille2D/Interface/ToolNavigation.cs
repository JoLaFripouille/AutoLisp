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
        BackColor=Color.FromArgb(30,54,59);ForeColor=Color.White;Padding=new(16,24,16,20);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
        layout.RowStyles.Add(new(SizeType.Absolute,46));layout.RowStyles.Add(new(SizeType.Absolute,36));layout.RowStyles.Add(new(SizeType.Percent,100));
        layout.Controls.Add(new Label{Text="JHRMAILLE",AutoSize=true,Font=new Font("Segoe UI",15,FontStyle.Bold),ForeColor=Color.White},0,0);
        layout.Controls.Add(new Label{Text="OUTILS",AutoSize=true,ForeColor=Color.FromArgb(179,205,207),Font=new Font("Segoe UI",9,FontStyle.Bold)},0,1);
        layout.Controls.Add(items,0,2);Controls.Add(layout);
        items.SizeChanged+=(_,_)=>{foreach(var button in buttons.Values)button.Width=Math.Max(100,items.ClientSize.Width-4);};
    }
    public void AddTool(string key,string title)
    {
        if(buttons.ContainsKey(key))throw new ArgumentException("Cet outil existe déjà.",nameof(key));
        var button=new Button{Text=title,Height=64,Width=170,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,
            Padding=new(12,0,8,0),Margin=new(0,0,0,10),Font=new Font("Segoe UI",10,FontStyle.Bold),Cursor=Cursors.Hand,
            UseVisualStyleBackColor=false,DialogResult=DialogResult.None,AccessibleName=title};
        button.FlatAppearance.BorderSize=0;button.Click+=(_,_)=>SelectTool(key);
        buttons.Add(key,button);items.Controls.Add(button);if(SelectedKey==null)SelectTool(key);
    }
    public void SelectTool(string key)
    {
        if(!buttons.ContainsKey(key))throw new ArgumentException("Outil inconnu.",nameof(key));
        bool changed=SelectedKey!=key;SelectedKey=key;
        foreach(var item in buttons){bool selected=item.Key==key;item.Value.BackColor=selected?Color.FromArgb(221,237,232):BackColor;
            item.Value.ForeColor=selected?Color.FromArgb(24,60,52):Color.White;item.Value.FlatAppearance.MouseOverBackColor=selected?Color.FromArgb(208,228,219):Color.FromArgb(47,76,80);}
        if(changed)ToolSelected?.Invoke(key);
    }
}
