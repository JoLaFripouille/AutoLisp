namespace JhrMailleUi;
public sealed class MeshForm : Form
{
    public MeshSettings Settings {get;private set;}
    readonly MeshSettings initial;
    readonly MeshPreview preview=new(){Dock=DockStyle.Fill};
    readonly ComboBox mode=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    readonly Dictionary<string,NumericUpDown> numbers=new();
    readonly ComboBox family=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    readonly ComboBox diameter=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    readonly ComboBox reference=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,DropDownWidth=520};
    readonly Label productInfo=new(){AutoSize=true,MaximumSize=new(370,0),ForeColor=Color.FromArgb(48,73,79),Padding=new(0,5,0,12)};
    bool catalogUpdating;
    readonly CheckBox corners=new(){Text="Compléter les angles automatiquement",AutoSize=true,Checked=true};
    readonly CheckBox dimensions=new(){Text="Afficher les diagonales entre centres",AutoSize=true};
    readonly Label error=new(){Dock=DockStyle.Fill,ForeColor=Color.Firebrick,AutoSize=true};
    readonly Label caption=new(){AutoSize=true,ForeColor=Color.FromArgb(48,73,79),Padding=new(0,8,0,8)};
    readonly Button accept=new(){Text="Valider les paramètres",AutoSize=true,Height=36};
    bool setting;
    public MeshForm(MeshSettings s)
    {
        initial=s;Settings=s;Text="JHR · Maille inox 2D";Font=new Font("Segoe UI",10);
        AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(1160,860);MinimumSize=new(1040,740);
        StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(244,247,246);
        MinimizeBox=false;MaximizeBox=true;ShowInTaskbar=false;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(20),ColumnCount=2,RowCount=3};
        layout.ColumnStyles.Add(new(SizeType.Absolute,410));layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,65));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,86));Controls.Add(layout);
        var title=new Label{Text="Configurer la maille",Font=new Font(Font.FontFamily,18,FontStyle.Bold),AutoSize=true};
        layout.Controls.Add(title,0,0);layout.SetColumnSpan(title,2);
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,AutoScroll=true,Padding=new(0,0,16,0)};
        fields.ColumnStyles.Add(new(SizeType.Percent,57));fields.ColumnStyles.Add(new(SizeType.Percent,43));layout.Controls.Add(fields,0,1);
        int row=0;
        void Label(string t){var l=new Label{Text=t,AutoSize=true,Padding=new(0,8,0,6)};fields.Controls.Add(l,0,row);}
        void Number(string key,string label,double value,decimal min=.01m,decimal max=10000, int decimals=2)
        {
            Label(label);var n=new NumericUpDown{Minimum=min,Maximum=max,DecimalPlaces=decimals,Increment=.5m,Dock=DockStyle.Fill,Value=Math.Clamp((decimal)value,min,max),Margin=new(2,5,0,5)};
            numbers[key]=n;fields.Controls.Add(n,1,row++);n.ValueChanged+=(_,_)=>UpdatePreview();
        }
        Label("Gamme Jakob");fields.SetColumnSpan(fields.GetControlFromPosition(0,row)!,2);row++;
        fields.Controls.Add(family,0,row++);fields.SetColumnSpan(family,2);
        Label("Diamètre proposé");fields.Controls.Add(diameter,1,row++);
        Label("Référence et maille");fields.SetColumnSpan(fields.GetControlFromPosition(0,row)!,2);row++;
        fields.Controls.Add(reference,0,row++);fields.SetColumnSpan(reference,2);
        fields.Controls.Add(productInfo,0,row++);fields.SetColumnSpan(productInfo,2);
        Label("Jonctions");fields.SetColumnSpan(fields.GetControlFromPosition(0,row)!,2);row++;
        mode.Items.AddRange(["Avec bagues serties","Tressées · sans bagues"]);fields.Controls.Add(mode,0,row++);fields.SetColumnSpan(mode,2);
        Number("wire","Câble de maille Ø (mm)",s.Wire,.1m,20);
        Number("width","Largeur X (mm)",s.Width,1,10000);
        Number("height","Hauteur Y (mm)",s.Height,1,10000);
        var hint=new Label{Text="Diagonales entre centres, avant rotation.\nLes dimensions du panneau viennent des contours.",AutoSize=true,ForeColor=Color.DimGray,Padding=new(0,4,0,10)};
        fields.Controls.Add(hint,0,row++);fields.SetColumnSpan(hint,2);
        Number("lace","Câble de laçage Ø (mm)",s.Lace,.1m,20);
        Number("gap","Retrait au cadre (mm)",s.Gap,.1m,10000);
        Number("angle","Orientation (degrés)",s.Angle,-360,360,1);
        Number("sleeveL","Longueur de bague (mm)",s.SleeveLength,.1m,100);
        Number("sleeveH","Hauteur de bague (mm)",s.SleeveHeight,.1m,100);
        Number("stops","Pas des butées de rive",s.Stops,1,1000,0);
        fields.Controls.Add(corners,0,row++);fields.SetColumnSpan(corners,2);corners.Checked=s.AutoCorners;
        var side=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new(12,0,0,0)};
        side.RowStyles.Add(new(SizeType.Absolute,30));side.RowStyles.Add(new(SizeType.Percent,100));side.RowStyles.Add(new(SizeType.Absolute,42));side.RowStyles.Add(new(SizeType.Absolute,38));layout.Controls.Add(side,1,1);
        side.Controls.Add(new Label{Text="Aperçu des câbles et des jonctions",AutoSize=true,Font=new Font(Font,FontStyle.Bold)},0,0);
        side.Controls.Add(preview,0,1);side.Controls.Add(caption,0,2);side.Controls.Add(dimensions,0,3);
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1};footer.ColumnStyles.Add(new(SizeType.Percent,100));footer.ColumnStyles.Add(new(SizeType.AutoSize));footer.ColumnStyles.Add(new(SizeType.AutoSize));layout.Controls.Add(footer,0,2);layout.SetColumnSpan(footer,2);
        footer.Controls.Add(error,0,0);var cancel=new Button{Text="Annuler",DialogResult=DialogResult.Cancel,AutoSize=true,CausesValidation=false,Margin=new(10,12,10,0)};
        accept.Margin=new(0,12,0,0);footer.Controls.Add(cancel,1,0);footer.Controls.Add(accept,2,0);AcceptButton=accept;CancelButton=cancel;
        accept.Click+=(_,_)=>{var candidate=Read();if(candidate.Validate() is string msg){error.Text=msg;return;}Settings=candidate;DialogResult=DialogResult.OK;Close();};
        mode.SelectedIndex=s.Woven?1:0;mode.SelectedIndexChanged+=(_,_)=>UpdatePreview();
        corners.CheckedChanged+=(_,_)=>UpdatePreview();dimensions.CheckedChanged+=(_,_)=>UpdatePreview();
        FormClosing+=(_,e)=>{if(DialogResult!=DialogResult.OK){Settings=initial;DialogResult=DialogResult.Cancel;}};
        family.Items.Add("Personnalisée · saisie libre");
        foreach(var name in JakobCatalogue.Items.Select(x=>x.Family).Distinct())family.Items.Add(name);
        family.SelectedIndexChanged+=(_,_)=>PopulateDiameters();
        diameter.SelectedIndexChanged+=(_,_)=>PopulateReferences();
        reference.SelectedIndexChanged+=(_,_)=>ApplyReference();
        var product=JakobCatalogue.Find(s.ProductReference);
        catalogUpdating=true;
        family.SelectedItem=product!=null&&product.Matches(s)?product.Family:"Personnalisée · saisie libre";
        catalogUpdating=false;PopulateDiameters(s.ProductReference);
        Shown+=(_,_)=>UpdatePreview();UpdatePreview();
    }
    MeshSettings Read()=>new((double)numbers["width"].Value,(double)numbers["height"].Value,(double)numbers["wire"].Value,
        (double)numbers["lace"].Value,(double)numbers["gap"].Value,(double)numbers["angle"].Value,mode.SelectedIndex==1,corners.Checked,
        (double)numbers["sleeveL"].Value,(double)numbers["sleeveH"].Value,(int)numbers["stops"].Value,SelectedProduct?.Reference??"");
    JakobMesh? SelectedProduct=>reference.SelectedItem as JakobMesh;
    void PopulateDiameters(string preferred="")
    {
        if(catalogUpdating)return;
        catalogUpdating=true;
        try {
            diameter.Items.Clear();reference.Items.Clear();
            if(family.SelectedIndex>0){
                foreach(double d in JakobCatalogue.Items.Where(x=>x.Family==(string)family.SelectedItem!).Select(x=>x.Wire).Distinct().Order())diameter.Items.Add(d);
                double wanted=JakobCatalogue.Find(preferred)?.Wire??(double)numbers["wire"].Value;
                diameter.SelectedItem=diameter.Items.Cast<double>().OrderBy(x=>Math.Abs(x-wanted)).First();
            }
        } finally {catalogUpdating=false;}
        PopulateReferences(preferred);
    }
    void PopulateReferences(string preferred="")
    {
        if(catalogUpdating)return;
        catalogUpdating=true;
        try {
            reference.Items.Clear();
            if(family.SelectedIndex>0 && diameter.SelectedItem is double d){
                foreach(var r in JakobCatalogue.Items.Where(x=>x.Family==(string)family.SelectedItem!&&x.Wire==d).OrderBy(x=>x.Mw).ThenBy(x=>x.Reference))reference.Items.Add(r);
                reference.SelectedItem=reference.Items.Cast<JakobMesh>().FirstOrDefault(x=>x.Reference==preferred)
                    ??reference.Items.Cast<JakobMesh>().OrderBy(x=>Math.Abs(x.Mw-(double)numbers["height"].Value)).First();
            }
        } finally {catalogUpdating=false;}
        ApplyReference();
    }
    void ApplyReference()
    {
        if(catalogUpdating)return;
        var product=SelectedProduct;setting=true;
        try {
            if(product!=null){
                var s=product.Apply(Read());
                numbers["wire"].Value=(decimal)s.Wire;numbers["width"].Value=(decimal)s.Width;numbers["height"].Value=(decimal)s.Height;
                numbers["sleeveL"].Value=(decimal)s.SleeveLength;numbers["sleeveH"].Value=(decimal)s.SleeveHeight;mode.SelectedIndex=s.Woven?1:0;
            }
            foreach(var k in new[]{"wire","width","height"})numbers[k].Enabled=product==null;
            mode.Enabled=product==null;diameter.Enabled=reference.Enabled=family.SelectedIndex>0;
            productInfo.Text=product==null?"Valeurs libres. Choisir une gamme pour utiliser une référence fabricant.":
                $"{product.Material} · câble {product.Construction}\nCatalogue {JakobCatalogue.Data.Edition} · page {product.PdfPage} · {product.WeightKgM2:g} kg/m²"+
                (product.RollHeightMm is double rh?$"\nRouleau {rh:g} × {product.RollLengthMm:g} mm · filet {product.BaseReference}":$"\nLongueur max. de production catalogue : {product.MaxProductionLengthM:g} m");
        } finally {setting=false;}
        UpdatePreview();
    }
    void UpdatePreview()
    {
        if(setting||numbers.Count!=9||mode.SelectedIndex<0)return;
        setting=true;
        try
        {
            var s=Read();var message=s.Validate();accept.Enabled=message==null;error.ForeColor=message==null?Color.DimGray:Color.Firebrick;error.Text=message??"Valider, puis sélectionner le contour intérieur\net le contour extérieur du cadre.";
            numbers["sleeveL"].Enabled=numbers["sleeveH"].Enabled=!s.Woven&&SelectedProduct==null;
            if(message==null){preview.Settings=s;preview.Dimensions=dimensions.Checked;preview.Invalidate();}
            caption.Text=$"X {s.Width:g} × Y {s.Height:g} mm  ·  câble Ø{s.Wire:g} mm  ·  {s.Angle:g}°";
        }
        finally{setting=false;}
    }
}
