namespace JhrMailleUi;

/// <summary>Navigation extensible de JHRMAILLE. Chaque outil conserve sa propre page.</summary>
public sealed class ToolNavigation : UserControl
{
    readonly Panel items=new(){Dock=DockStyle.Fill,AutoScroll=true,Margin=Padding.Empty,Padding=Padding.Empty};
    readonly Dictionary<string,Button> buttons=new();
    public string? SelectedKey {get;private set;}
    public event Action<string>? ToolSelected;
    public ToolNavigation()
    {
        BackColor=UiTheme.Nav;ForeColor=Color.White;Padding=new(14,26,14,20);items.BackColor=BackColor;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,46));layout.RowStyles.Add(new(SizeType.Absolute,36));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,92));
        layout.Controls.Add(new Label{Text="JHR  /  Panneaux",AutoSize=true,Font=new Font("Segoe UI",13,FontStyle.Bold),ForeColor=Color.White},0,0);
        layout.Controls.Add(new Label{Text="REMPLISSAGE",AutoSize=true,ForeColor=Color.FromArgb(179,205,207),Font=new Font("Segoe UI",9,FontStyle.Bold)},0,1);
        layout.Controls.Add(items,0,2);
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
        footer.ColumnStyles.Add(new(SizeType.Percent,100));
        footer.RowStyles.Add(new(SizeType.Absolute,36));footer.RowStyles.Add(new(SizeType.Percent,100));
        var updates=new ModernButton{Text="Mises à jour",Dock=DockStyle.Fill,Margin=new(0,0,0,8),AccessibleName="Vérifier les mises à jour GitHub"};UiTheme.Button(updates);updates.MinimumSize=Size.Empty;
        updates.Click+=(_,_)=>{using var dialog=new UpdateDialog();dialog.ShowDialog(FindForm());};footer.Controls.Add(updates,0,0);
        footer.Controls.Add(new Label{Text="v"+UpdateService.CurrentVersion.ToString(3)+" · 2D paramétrique\nAperçu en direct",AutoSize=true,ForeColor=Color.FromArgb(133,156,174),Font=new Font("Segoe UI",9)},0,1);layout.Controls.Add(footer,0,3);Controls.Add(layout);
    }
    public void AddTool(string key,string title)
    {
        if(buttons.ContainsKey(key))throw new ArgumentException("Cet outil existe déjà.",nameof(key));
        var button=new NavButton{Key=key,Text=title,Height=68,Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,
            Padding=new(47,0,8,0),Margin=new(0,0,0,10),Font=new Font("Segoe UI",10,FontStyle.Regular),Cursor=Cursors.Hand,
            UseVisualStyleBackColor=false,DialogResult=DialogResult.None,AccessibleName=title};
        button.FlatAppearance.BorderSize=0;button.Click+=(_,_)=>SelectTool(key);
        buttons.Add(key,button);var slot=new Panel{Dock=DockStyle.Top,Height=78,Padding=new(0,0,0,10),Margin=Padding.Empty};slot.Controls.Add(button);items.Controls.Add(slot);slot.BringToFront();if(SelectedKey==null)SelectTool(key);
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
