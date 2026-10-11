namespace JhrMailleUi;
public sealed class PatternPage : UserControl
{
    public readonly Button Accept=new ModernButton{Text="Continuer",AutoSize=true};
    public readonly Button Cancel=new ModernButton{Text="Annuler",AutoSize=true};
    public event Action? Accepted;
    public event Action? Changed;
    bool live;[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool Live {get=>live;set{live=value;UpdatePreview();}}
    public event Action? Cancelled;
    readonly FillTool tool;
    readonly CheckBox mask=new(){Text="Masquage arrière-plan",AutoSize=true};
    readonly ComboBox maskScope=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
    public int Mask=>mask.Checked?maskScope.SelectedIndex+1:0;
    readonly Dictionary<string,NumericUpDown> numbers=new();
    readonly ComboBox pattern=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly PatternPreview preview=new(){Dock=DockStyle.Fill};
    readonly Label error=new(){AutoSize=true,Dock=DockStyle.Fill};
    readonly Label caption=new(){AutoSize=true,Dock=DockStyle.Fill};
    readonly CheckBox dimensions=new(){Text="Afficher les dimensions",AutoSize=true};
    public PatternPage(FillTool tool,PatternSettings s,int squareMask=0)
    {
        this.tool=tool;Dock=DockStyle.Fill;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24,24,24,18),ColumnCount=2,RowCount=3};
        layout.ColumnStyles.Add(new(SizeType.Absolute,410));layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,82));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,78));Controls.Add(layout);
        string title=tool switch{FillTool.Square=>"Grillage serrurier",FillTool.Expanded=>"Métal déployé",_=>"Tôle perforée"};
        layout.Controls.Add(UiTheme.Header(title,tool switch{FillTool.Square=>"Maille carrée • fils croisés à bords fermés",FillTool.Expanded=>"Mailles losangées • lanières et jonctions",_=>"Trous ronds ou carrés • bordure pleine"}),0,0);layout.SetColumnSpan(layout.GetControlFromPosition(0,0)!,2);
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,AutoScroll=true,Padding=new(0,0,12,0)};
        fields.ColumnStyles.Add(new(SizeType.Percent,60));fields.ColumnStyles.Add(new(SizeType.Percent,40));layout.Controls.Add(fields,0,1);
        int row=0;
        UiTheme.Section(fields,ref row,"Type de panneau");
        void Label(string text){fields.Controls.Add(new Label{Text=text,AutoSize=true,Padding=new(0,8,0,6)},0,row);}
        void Number(string key,string label,double value,decimal min=.1m,decimal max=10000){Label(label);var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Clamp((decimal)value,min,max),DecimalPlaces=2,Increment=.5m,Dock=DockStyle.Fill,Margin=new(2,5,0,5)};numbers[key]=n;fields.Controls.Add(n,1,row++);n.ValueChanged+=(_,_)=>UpdatePreview();}
        Label(tool==FillTool.Perforated?"Perforation":tool==FillTool.Square?"Sélection du grillage soudé":"Type de remplissage");fields.SetColumnSpan(fields.GetControlFromPosition(0,row)!,2);row++;
        pattern.Items.AddRange(tool switch{FillTool.Square=>new object[]{"Un contour · limite du panneau","Deux contours · fils visibles","Deux contours · derrière le cadre en caché"},FillTool.Expanded=>new object[]{"Aplati","En relief — projection de face"},_=>new object[]{"Ronds alignés · U","Ronds en quinconce 60° · T","Carrés alignés · U"}});
        fields.Controls.Add(pattern,0,row++);fields.SetColumnSpan(pattern,2);
        UiTheme.Section(fields,ref row,"Dimensions de la maille");
        Number("a",tool switch{FillTool.Square=>"Ouverture libre carrée (mm)",FillTool.Expanded=>"Grande diagonale ML (mm)",_=>"Trou : Ø / côté (mm)"},s.A);
        if(tool!=FillTool.Square)Number("b",tool==FillTool.Expanded?"Petite diagonale MW (mm)":"Entraxe des trous (mm)",s.B);
        if(tool!=FillTool.Perforated)Number("wire",tool==FillTool.Square?"Diamètre du fil (mm)":"Largeur projetée de lanière (mm)",s.Wire);
        if(tool!=FillTool.Square)Number("thickness","Épaisseur matière (mm)",s.Thickness);
        UiTheme.Section(fields,ref row,"Montage et orientation");
        Number("gap","Retrait au cadre (mm)",s.Gap,0);
        if(tool==FillTool.Expanded)Number("margin","Plat de jonction (0 = auto)",s.Margin,0);
        if(tool==FillTool.Perforated)Number("margin","Bordure pleine minimum (mm)",s.Margin,0);
        Number("angle","Orientation (degrés)",s.Angle,-360,360);
        if(tool==FillTool.Square){
            UiTheme.Section(fields,ref row,"Affichage dans le plan");
            fields.Controls.Add(mask,0,row++);fields.SetColumnSpan(mask,2);
            maskScope.Items.AddRange(["Sous les fils · ouvertures transparentes","Toute la surface intérieure"]);
            maskScope.SelectedIndex=squareMask==2?1:0;mask.Checked=squareMask!=0;maskScope.Enabled=mask.Checked;
            fields.Controls.Add(maskScope,0,row++);fields.SetColumnSpan(maskScope,2);
            mask.CheckedChanged+=(_,_)=>{maskScope.Enabled=mask.Checked;UpdatePreview();};maskScope.SelectedIndexChanged+=(_,_)=>UpdatePreview();
        }
        string hint=tool switch{
            FillTool.Square=>"Ouverture libre + diamètre = entraxe des fils.\nUn contour : les fils s’arrêtent sur cette limite.\nDeux contours : arrêt sur le contour extérieur.\nFils horizontaux devant ; verticaux interrompus aux croisements.\nExtrémités fermées ; trame centrée sans couper un fil dans sa longueur.\nMasquage limité au grillage, hors cadre.",
            FillTool.Expanded=>"ML et MW : diagonales entre axes des nœuds.\nLanières continues, plats aux jonctions et angles adoucis.\nPlat 0 : proportion automatique ; sinon longueur en mm.\nL’épaisseur est enregistrée dans la désignation.",
            _=>"T : entraxe triangulaire à 60°. U : trame alignée.\nSeuls les trous entiers sont dessinés ; la bordure reste pleine.\nL’épaisseur est enregistrée dans la désignation."};
        var help=new Label{Text=hint,AutoSize=true,MaximumSize=new(455,0),ForeColor=UiTheme.Muted,Font=new Font("Segoe UI",9),Padding=new(0,18,0,0)};fields.Controls.Add(help,0,row);fields.SetColumnSpan(help,2);
        var side=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,Padding=new(12,0,0,0)};
        side.RowStyles.Add(new(SizeType.Absolute,30));side.RowStyles.Add(new(SizeType.Percent,100));side.RowStyles.Add(new(SizeType.Absolute,52));side.RowStyles.Add(new(SizeType.Absolute,32));layout.Controls.Add(side,1,1);
        side.Controls.Add(new Label{Text="Aperçu du remplissage",AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold)},0,0);side.Controls.Add(preview,0,1);side.Controls.Add(caption,0,2);side.Controls.Add(dimensions,0,3);
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};footer.ColumnStyles.Add(new(SizeType.Percent,100));footer.ColumnStyles.Add(new(SizeType.AutoSize));footer.ColumnStyles.Add(new(SizeType.AutoSize));layout.Controls.Add(footer,0,2);layout.SetColumnSpan(footer,2);
        UiTheme.Button(Accept,true);UiTheme.Button(Cancel);Accept.Margin=new(0,12,0,0);Cancel.Margin=new(10,12,10,0);footer.Controls.Add(error,0,0);footer.Controls.Add(Cancel,1,0);footer.Controls.Add(Accept,2,0);
        Accept.Click+=(_,_)=>{if(Read().Validate(tool)==null)Accepted?.Invoke();};Cancel.Click+=(_,_)=>Cancelled?.Invoke();
        pattern.SelectedIndex=s.Pattern;pattern.SelectedIndexChanged+=(_,_)=>UpdatePreview();dimensions.CheckedChanged+=(_,_)=>UpdatePreview();UiTheme.Style(this);UpdatePreview();
    }
    double Value(string key,double fallback)=>numbers.TryGetValue(key,out var n)?(double)n.Value:fallback;
    public PatternSettings Read()=>new(Value("a",30),Value("b",Value("a",30)),Value("wire",1),Value("thickness",1),Value("gap",0),Value("angle",0),Value("margin",0),Math.Max(0,pattern.SelectedIndex));
    void UpdatePreview(){if(!numbers.ContainsKey("angle")||pattern.SelectedIndex<0)return;var s=Read();string? msg=s.Validate(tool);Accept.Enabled=msg==null;error.ForeColor=msg==null?Color.DimGray:Color.Firebrick;error.Text=msg??(Live?"Aperçu en direct.\nAppliquer pour enregistrer.":tool==FillTool.Square&&s.Pattern==0?"Valider, puis sélectionner UN contour fermé\nde la taille totale du panneau.":"Valider, puis sélectionner le contour intérieur\net le contour extérieur du cadre.");if(msg==null){preview.Tool=tool;preview.Settings=s;preview.Dimensions=dimensions.Checked;preview.Invalidate();}caption.Text=PatternRenderer.Designation(tool,s);Changed?.Invoke();}
}
