namespace JhrMailleUi;
public sealed class PatternPage : UserControl
{
    public readonly Button Accept=new(){Text="Valider les paramètres",AutoSize=true};
    public readonly Button Cancel=new(){Text="Annuler",AutoSize=true};
    public event Action? Accepted;
    public event Action? Cancelled;
    readonly FillTool tool;
    readonly Dictionary<string,NumericUpDown> numbers=new();
    readonly ComboBox pattern=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly PatternPreview preview=new(){Dock=DockStyle.Fill};
    readonly Label error=new(){AutoSize=true,Dock=DockStyle.Fill};
    readonly Label caption=new(){AutoSize=true,Dock=DockStyle.Fill};
    readonly CheckBox dimensions=new(){Text="Afficher les dimensions",AutoSize=true};
    public PatternPage(FillTool tool,PatternSettings s)
    {
        this.tool=tool;Dock=DockStyle.Fill;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(20),ColumnCount=2,RowCount=3};
        layout.ColumnStyles.Add(new(SizeType.Absolute,410));layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,65));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,86));Controls.Add(layout);
        string title=tool switch{FillTool.Square=>"Grillage serrurier — maille carrée",FillTool.Expanded=>"Métal déployé",_=>"Tôle perforée"};
        layout.Controls.Add(new Label{Text=title,Font=new Font("Segoe UI",17,FontStyle.Bold),AutoSize=true},0,0);layout.SetColumnSpan(layout.GetControlFromPosition(0,0)!,2);
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,AutoScroll=true,Padding=new(0,0,16,0)};
        fields.ColumnStyles.Add(new(SizeType.Percent,60));fields.ColumnStyles.Add(new(SizeType.Percent,40));layout.Controls.Add(fields,0,1);
        int row=0;
        void Label(string text){fields.Controls.Add(new Label{Text=text,AutoSize=true,Padding=new(0,8,0,6)},0,row);}
        void Number(string key,string label,double value,decimal min=.1m,decimal max=10000){Label(label);var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Clamp((decimal)value,min,max),DecimalPlaces=2,Increment=.5m,Dock=DockStyle.Fill,Margin=new(2,5,0,5)};numbers[key]=n;fields.Controls.Add(n,1,row++);n.ValueChanged+=(_,_)=>UpdatePreview();}
        Label(tool==FillTool.Perforated?"Perforation":tool==FillTool.Square?"Sélection du grillage soudé":"Type de remplissage");fields.SetColumnSpan(fields.GetControlFromPosition(0,row)!,2);row++;
        pattern.Items.AddRange(tool switch{FillTool.Square=>new object[]{"Un contour · limite du panneau","Deux contours · fils visibles","Deux contours · derrière le cadre en caché"},FillTool.Expanded=>new object[]{"Aplati","En relief — projection de face"},_=>new object[]{"Ronds alignés · U","Ronds en quinconce 60° · T","Carrés alignés · U"}});
        fields.Controls.Add(pattern,0,row++);fields.SetColumnSpan(pattern,2);
        Number("a",tool switch{FillTool.Square=>"Ouverture libre carrée (mm)",FillTool.Expanded=>"Grande diagonale ML (mm)",_=>"Trou : Ø / côté (mm)"},s.A);
        if(tool!=FillTool.Square)Number("b",tool==FillTool.Expanded?"Petite diagonale MW (mm)":"Entraxe des trous (mm)",s.B);
        if(tool!=FillTool.Perforated)Number("wire",tool==FillTool.Square?"Diamètre du fil (mm)":"Largeur projetée de lanière (mm)",s.Wire);
        if(tool!=FillTool.Square)Number("thickness","Épaisseur matière (mm)",s.Thickness);
        Number("gap","Retrait au cadre (mm)",s.Gap,0);
        if(tool==FillTool.Perforated)Number("margin","Bordure pleine minimum (mm)",s.Margin,0);
        Number("angle","Orientation (degrés)",s.Angle,-360,360);
        string hint=tool switch{
            FillTool.Square=>"Ouverture libre + diamètre = entraxe des fils.\nUn contour : les fils s’arrêtent sur cette limite.\nDeux contours : arrêt sur le contour extérieur.\nFils horizontaux devant ; verticaux interrompus aux croisements.",
            FillTool.Expanded=>"ML et MW : diagonales entre axes des nœuds.\nLa lanière indique la largeur visible en projection 2D.\nL’épaisseur est enregistrée dans la désignation.",
            _=>"T : entraxe triangulaire à 60°. U : trame alignée.\nSeuls les trous entiers sont dessinés ; la bordure reste pleine.\nL’épaisseur est enregistrée dans la désignation."};
        var help=new Label{Text=hint,AutoSize=true,MaximumSize=new(365,0),ForeColor=Color.DimGray,Padding=new(0,16,0,0)};fields.Controls.Add(help,0,row);fields.SetColumnSpan(help,2);
        var side=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,Padding=new(12,0,0,0)};
        side.RowStyles.Add(new(SizeType.Absolute,30));side.RowStyles.Add(new(SizeType.Percent,100));side.RowStyles.Add(new(SizeType.Absolute,52));side.RowStyles.Add(new(SizeType.Absolute,32));layout.Controls.Add(side,1,1);
        side.Controls.Add(new Label{Text="Aperçu du remplissage",AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold)},0,0);side.Controls.Add(preview,0,1);side.Controls.Add(caption,0,2);side.Controls.Add(dimensions,0,3);
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};footer.ColumnStyles.Add(new(SizeType.Percent,100));footer.ColumnStyles.Add(new(SizeType.AutoSize));footer.ColumnStyles.Add(new(SizeType.AutoSize));layout.Controls.Add(footer,0,2);layout.SetColumnSpan(footer,2);
        Accept.Margin=new(0,12,0,0);Cancel.Margin=new(10,12,10,0);footer.Controls.Add(error,0,0);footer.Controls.Add(Cancel,1,0);footer.Controls.Add(Accept,2,0);
        Accept.Click+=(_,_)=>{if(Read().Validate(tool)==null)Accepted?.Invoke();};Cancel.Click+=(_,_)=>Cancelled?.Invoke();
        pattern.SelectedIndex=s.Pattern;pattern.SelectedIndexChanged+=(_,_)=>UpdatePreview();dimensions.CheckedChanged+=(_,_)=>UpdatePreview();UpdatePreview();
    }
    double Value(string key,double fallback)=>numbers.TryGetValue(key,out var n)?(double)n.Value:fallback;
    public PatternSettings Read()=>new(Value("a",30),Value("b",Value("a",30)),Value("wire",1),Value("thickness",1),Value("gap",0),Value("angle",0),Value("margin",0),Math.Max(0,pattern.SelectedIndex));
    void UpdatePreview(){if(!numbers.ContainsKey("angle")||pattern.SelectedIndex<0)return;var s=Read();string? msg=s.Validate(tool);Accept.Enabled=msg==null;error.ForeColor=msg==null?Color.DimGray:Color.Firebrick;error.Text=msg??(tool==FillTool.Square&&s.Pattern==0?"Valider, puis sélectionner UN contour fermé\nde la taille totale du panneau.":"Valider, puis sélectionner le contour intérieur\net le contour extérieur du cadre.");if(msg==null){preview.Tool=tool;preview.Settings=s;preview.Dimensions=dimensions.Checked;preview.Invalidate();}caption.Text=PatternRenderer.Designation(tool,s);}
}
