using System.Globalization;
namespace JhrMailleUi.Frames;
public sealed record FrameChoice(string Name,string Family,FrameProfile? Profile,string File=""){public override string ToString()=>Name;}

public sealed class FrameDialog : Form
{
    readonly List<FrameChoice> library;readonly Func<string,bool,List<FrameProfile>> importer;readonly Func<string,List<FrameChoice>> discover;
    readonly ComboBox profile=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=550},leaf=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=550};
    readonly ComboBox orientation=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Dictionary<string,NumericUpDown> numbers=new();readonly TextBox mullions=new(){Dock=DockStyle.Fill},transoms=new(){Dock=DockStyle.Fill};
    readonly CheckBox mirror=new(){Text="Retourner la section",AutoSize=true},miter=new(){Text="Assemblages à 45°",AutoSize=true,Checked=true},twoSheets=new(){Text="Tôle sur les deux faces",AutoSize=true};
    readonly Label status=new(){Dock=DockStyle.Fill,ForeColor=UiTheme.Muted};readonly ModernButton apply=new(){Text="Valider"};
    readonly FramePreview preview=new(){Dock=DockStyle.Fill};readonly ListBox cuts=new(){Dock=DockStyle.Fill};
    readonly RadioButton vertical=new(){Text="Coupe verticale",Checked=true,AutoSize=true},horizontal=new(){Text="Coupe horizontale",AutoSize=true};
    readonly NumericUpDown station=new(){DecimalPlaces=2,Minimum=0,Maximum=50000,Dock=DockStyle.Fill};
    readonly List<FrameCut> definitions=new();readonly Dictionary<FrameTool,Button> navigation=new();
    bool setting;FrameTool tool;FrameProfile? selected,selectedLeaf;
    public FrameDesign Design{get;private set;}
    public FrameDialog(FrameDesign initial,List<FrameChoice> choices,Func<string,bool,List<FrameProfile>> import,Func<string,List<FrameChoice>> browse)
    {
        Design=initial;tool=initial.Tool;library=choices;importer=import;discover=browse;definitions.AddRange(initial.Cuts);
        Text="JHR Cadres — élévation et coupes";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;BackColor=UiTheme.Canvas;ClientSize=new(1350,860);MinimumSize=new(1080,720);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=false;
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};shell.ColumnStyles.Add(new(SizeType.Absolute,180));shell.ColumnStyles.Add(new(SizeType.Absolute,335));shell.ColumnStyles.Add(new(SizeType.Percent,100));Controls.Add(shell);
        var nav=new Panel{Dock=DockStyle.Fill,BackColor=UiTheme.Nav,Padding=new(12,20,12,20)};shell.Controls.Add(nav,0,0);
        var navList=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,RowCount=5,Height=365};navList.ColumnStyles.Add(new(SizeType.Percent,100));navList.RowStyles.Add(new(SizeType.Absolute,65));navList.Controls.Add(new Label{Text="JHR / Cadres",ForeColor=Color.White,Font=new Font("Segoe UI",13,FontStyle.Bold),AutoSize=true},0,0);
        string[] titles=["Cadres en tubes","Menuiserie acier","Profils laminés","Portes tôlées"];
        foreach(var t in Enum.GetValues<FrameTool>()){var button=new NavButton{Text=titles[(int)t],Key="cadre-"+t,Dock=DockStyle.Fill,Margin=new(0,0,0,10),Font=new Font("Segoe UI",10),FlatStyle=FlatStyle.Flat};button.FlatAppearance.BorderSize=0;navigation[t]=button;navList.RowStyles.Add(new(SizeType.Absolute,72));navList.Controls.Add(button,0,(int)t+1);button.Click+=(_,_)=>SelectTool(t);}
        nav.Controls.Add(navList);nav.Controls.Add(new Label{Text="Sections réelles\nCoupes associées\nPremière version :\nrectangles",ForeColor=Color.FromArgb(171,194,204),Dock=DockStyle.Bottom,Height=90});
        var settings=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=2,Padding=new(18,22,14,10)};settings.ColumnStyles.Add(new(SizeType.Percent,55));settings.ColumnStyles.Add(new(SizeType.Percent,45));shell.Controls.Add(settings,1,0);int row=0;
        void Full(Control c){settings.Controls.Add(c,0,row++);settings.SetColumnSpan(c,2);}
        void Number(string key,string text,double value,decimal min,decimal max){settings.Controls.Add(new Label{Text=text,AutoSize=true,Padding=new(0,8,0,0)},0,row);var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Clamp((decimal)value,min,max),DecimalPlaces=2,Dock=DockStyle.Fill};numbers[key]=n;settings.Controls.Add(n,1,row++);n.ValueChanged+=(_,_)=>RefreshDraft();}
        UiTheme.Section(settings,ref row,"Sections de profil");Full(new Label{Text="Profil du cadre / dormant",AutoSize=true});Full(profile);
        Full(new Label{Text="Ossature du vantail (porte tôlée)",AutoSize=true});Full(leaf);
        var importTemplate=new ModernButton{Text="Importer le gabarit",Dock=DockStyle.Fill};UiTheme.Button(importTemplate);importTemplate.MinimumSize=Size.Empty;Full(importTemplate);
        var importProfile=new ModernButton{Text="Importer un profil DWG",Dock=DockStyle.Fill};UiTheme.Button(importProfile);importProfile.MinimumSize=Size.Empty;Full(importProfile);
        var folder=new ModernButton{Text="Bibliothèque Jansen…",Dock=DockStyle.Fill};UiTheme.Button(folder);folder.MinimumSize=Size.Empty;Full(folder);
        importTemplate.Click+=(_,_)=>Import(true);importProfile.Click+=(_,_)=>Import(false);folder.Click+=(_,_)=>{using var dialog=new FolderBrowserDialog{Description="Dossier de bibliothèque Jansen (Art_Bibl)"};if(dialog.ShowDialog(this)==DialogResult.OK){library.AddRange(discover(dialog.SelectedPath));Populate();}};
        UiTheme.Section(settings,ref row,"Dimensions hors tout");Number("width","Largeur (mm)",initial.Width,20,50000);Number("height","Hauteur (mm)",initial.Height,20,50000);
        orientation.Items.AddRange(["Section : 0°","Section : 90°","Section : 180°","Section : 270°"]);orientation.SelectedIndex=Math.Clamp(initial.Turns,0,3);Full(orientation);mirror.Checked=initial.Mirror;Full(mirror);miter.Checked=initial.Miter;Full(miter);
        UiTheme.Section(settings,ref row,"Divisions — depuis bas/gauche");Full(new Label{Text="Montants X (mm, séparés par ;) :",AutoSize=true});mullions.Text=string.Join(";",initial.Mullions.Select(x=>x.ToString(CultureInfo.InvariantCulture)));Full(mullions);Full(new Label{Text="Traverses Y (mm, séparées par ;) :",AutoSize=true});transoms.Text=string.Join(";",initial.Transoms.Select(x=>x.ToString(CultureInfo.InvariantCulture)));Full(transoms);
        UiTheme.Section(settings,ref row,"Porte tôlée");Number("gap","Jeu dormant/vantail",initial.Gap,.5m,100);Number("sheet","Épaisseur de tôle",initial.Sheet,.1m,20);twoSheets.Checked=initial.TwoSheets;Full(twoSheets);
        UiTheme.Section(settings,ref row,"Repères de coupe");Number("labels","Hauteur en mm modèle",initial.LabelHeight,1,500);
        Full(new Label{Text="La porte comprend dormant, ossature et tôles. Quincaillerie et ouvrants de menuiserie seront complétés ensuite.",AutoSize=true,MaximumSize=new(280,0),ForeColor=UiTheme.Muted});
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new(16,20,20,14)};right.ColumnStyles.Add(new(SizeType.Percent,100));right.RowStyles.Add(new(SizeType.Absolute,62));right.RowStyles.Add(new(SizeType.Percent,100));right.RowStyles.Add(new(SizeType.Absolute,140));right.RowStyles.Add(new(SizeType.Absolute,45));right.RowStyles.Add(new(SizeType.Absolute,46));shell.Controls.Add(right,2,0);
        right.Controls.Add(UiTheme.Header("Élévation et coupes","Clique ou déplace une ligne dans l’élévation pour définir la coupe."),0,0);right.Controls.Add(preview,0,1);
        var section=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=4};section.ColumnStyles.Add(new(SizeType.Percent,60));section.ColumnStyles.Add(new(SizeType.Percent,40));section.RowStyles.Add(new(SizeType.Absolute,30));section.RowStyles.Add(new(SizeType.Absolute,32));section.RowStyles.Add(new(SizeType.Percent,100));
        var direction=new FlowLayoutPanel{Dock=DockStyle.Fill};direction.Controls.AddRange([vertical,horizontal]);section.Controls.Add(direction,0,0);section.SetColumnSpan(direction,2);section.Controls.Add(station,0,1);
        var flip=new ModernButton{Text="Inverser le regard",Dock=DockStyle.Fill};UiTheme.Button(flip);flip.MinimumSize=Size.Empty;section.Controls.Add(flip,1,1);section.Controls.Add(cuts,0,2);
        var remove=new ModernButton{Text="Supprimer la coupe",Dock=DockStyle.Top};UiTheme.Button(remove);remove.MinimumSize=Size.Empty;section.Controls.Add(remove,1,2);right.Controls.Add(section,0,2);right.Controls.Add(status,0,3);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};var cancel=new ModernButton{Text="Annuler",DialogResult=DialogResult.Cancel};UiTheme.Button(cancel);UiTheme.Button(apply,true);actions.Controls.Add(cancel);actions.Controls.Add(apply);right.Controls.Add(actions,0,4);AcceptButton=apply;CancelButton=cancel;
        profile.SelectedIndexChanged+=(_,_)=>Resolve(false);leaf.SelectedIndexChanged+=(_,_)=>Resolve(true);
        foreach(Control control in new Control[]{orientation,mirror,miter,twoSheets,mullions,transoms}){if(control is ComboBox combo)combo.SelectedIndexChanged+=(_,_)=>RefreshDraft();else if(control is CheckBox check)check.CheckedChanged+=(_,_)=>RefreshDraft();else control.TextChanged+=(_,_)=>RefreshDraft();}
        cuts.SelectedIndexChanged+=(_,_)=>{setting=true;if(cuts.SelectedIndex>=0)station.Value=(decimal)definitions[cuts.SelectedIndex].Station;setting=false;RefreshDraft();};
        station.ValueChanged+=(_,_)=>{if(setting||cuts.SelectedIndex<0)return;definitions[cuts.SelectedIndex]=definitions[cuts.SelectedIndex] with{Station=(double)station.Value};UpdateCuts(cuts.SelectedIndex);};
        flip.Click+=(_,_)=>{int i=cuts.SelectedIndex;if(i>=0){definitions[i]=definitions[i] with{Reverse=!definitions[i].Reverse};UpdateCuts(i);}};remove.Click+=(_,_)=>{int i=cuts.SelectedIndex;if(i>=0){definitions.RemoveAt(i);UpdateCuts(Math.Min(i,definitions.Count-1));}};
        preview.NewVertical=vertical.Checked;vertical.CheckedChanged+=(_,_)=>preview.NewVertical=vertical.Checked;
        preview.CutChanged+=(index,coordinate)=>{if(index<0){int n=0;string name;do{name=((char)('A'+n++)).ToString();}while(definitions.Any(c=>c.Id==name));definitions.Add(new(name,vertical.Checked,coordinate));index=definitions.Count-1;}else definitions[index]=definitions[index] with{Station=coordinate};UpdateCuts(index);};
        apply.Click+=(_,_)=>{RefreshDraft();if(preview.Error==null){Design=Draft();DialogResult=DialogResult.OK;Close();}};
        UiTheme.Style(this);if(initial.Profile!=null&&!library.Any(c=>c.Profile?.Id==initial.Profile.Id))library.Add(new(initial.Profile.Name,initial.Profile.Family,initial.Profile));if(initial.LeafProfile!=null&&!library.Any(c=>c.Profile?.Id==initial.LeafProfile.Id))library.Add(new(initial.LeafProfile.Name,initial.LeafProfile.Family,initial.LeafProfile));
        Populate(initial.Profile?.Id,initial.LeafProfile?.Id);UpdateCuts(0);SelectTool(tool);
    }
    void Import(bool template){using var dialog=new OpenFileDialog{Filter=template?"Gabarit ou dessin|*.dwt;*.dwg":"Profil DWG|*.dwg",Title=template?"Gabarit contenant les sections de tubes et laminés":"Section isolée de profil"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{status.Text="Lecture des sections…";status.Refresh();foreach(var p in importer(dialog.FileName,template))library.Add(new(p.Name,p.Family,p));Populate();}catch(Exception e){status.Text=e.Message;}}
    public void SelectTool(FrameTool next){tool=next;foreach(var pair in navigation){((NavButton)pair.Value).Selected=pair.Key==tool;pair.Value.Invalidate();}Populate();leaf.Enabled=tool==FrameTool.SheetDoor;numbers["gap"].Enabled=numbers["sheet"].Enabled=twoSheets.Enabled=tool==FrameTool.SheetDoor;RefreshDraft();}
    void Populate(string? preferred=null,string? preferredLeaf=null)
    {
        setting=true;preferred??=selected?.Id;preferredLeaf??=selectedLeaf?.Id;profile.Items.Clear();leaf.Items.Clear();
        var options=library.DistinctBy(c=>c.Profile?.Id??c.File).Where(c=>tool==FrameTool.Joinery?c.Family=="Jansen":tool==FrameTool.Rolled?c.Family=="Laminés":c.Family=="Tubes").ToArray();profile.Items.AddRange(options);leaf.Items.AddRange(library.Where(c=>c.Family=="Tubes").DistinctBy(c=>c.Profile?.Id).ToArray());
        int Find(ComboBox box,string? id){for(int i=0;i<box.Items.Count;i++)if(box.Items[i] is FrameChoice choice&&choice.Profile?.Id==id)return i;return box.Items.Count>0?0:-1;}
        profile.SelectedIndex=Find(profile,preferred);leaf.SelectedIndex=Find(leaf,preferredLeaf);setting=false;Resolve(false);Resolve(true);
    }
    void Resolve(bool isLeaf){if(setting)return;var choice=(isLeaf?leaf:profile).SelectedItem as FrameChoice;try{var p=choice?.Profile;if(p==null&&choice!=null&&choice.File!=""){status.Text="Lecture du profil…";status.Refresh();p=importer(choice.File,false).Single();int index=library.IndexOf(choice);if(index>=0)library[index]=choice with{Profile=p};}if(isLeaf)selectedLeaf=p;else selected=p;RefreshDraft();}catch(Exception e){if(isLeaf)selectedLeaf=null;else selected=null;apply.Enabled=false;status.Text=e.Message;}}
    void UpdateCuts(int selectedIndex){setting=true;cuts.Items.Clear();foreach(var c in definitions)cuts.Items.Add(c.Id+"–"+c.Id+" · "+(c.Vertical?"V":"H")+" · "+c.Station.ToString("0.##")+" mm "+(c.Reverse?"←":"→"));cuts.SelectedIndex=selectedIndex>=0&&selectedIndex<definitions.Count?selectedIndex:-1;station.Value=cuts.SelectedIndex>=0?(decimal)Math.Clamp(definitions[cuts.SelectedIndex].Station,0,50000):0;setting=false;RefreshDraft();}
    FrameDesign Draft(){double[] Parse(string text)=>text.Split(';',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(x=>double.Parse(x.Replace(',','.'),CultureInfo.InvariantCulture)).ToArray();return new(){Tool=tool,Width=(double)numbers["width"].Value,Height=(double)numbers["height"].Value,Profile=selected,LeafProfile=selectedLeaf,Turns=orientation.SelectedIndex,Mirror=mirror.Checked,Miter=miter.Checked,Mullions=Parse(mullions.Text),Transoms=Parse(transoms.Text),Gap=(double)numbers["gap"].Value,Sheet=(double)numbers["sheet"].Value,TwoSheets=twoSheets.Checked,LabelHeight=(double)numbers["labels"].Value,Cuts=definitions.ToArray()};}
    void RefreshDraft(){if(setting||numbers.Count<4)return;try{var draft=Draft();preview.Set(draft,cuts.SelectedIndex);apply.Enabled=preview.Error==null;status.ForeColor=apply.Enabled?UiTheme.Muted:Color.Firebrick;status.Text=preview.Error??$"{draft.Width:0.##} × {draft.Height:0.##} mm · {draft.Cuts.Length} coupe(s). Sections à taille réelle.";}catch(Exception e){apply.Enabled=false;status.Text=e.Message;}}
}

public sealed class FramePreview : Control
{
    FrameDesign? design;FramePath[] elevation=[];readonly List<(FrameCut Cut,FramePath[] Paths)> sections=new();RectangleF area;double scale;int selected=-1,drag=-1;
    public string? Error{get;private set;}public event Action<int,double>? CutChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]public bool NewVertical{get;set;}=true;
    public FramePreview(){DoubleBuffered=true;BackColor=Color.White;Cursor=Cursors.Cross;}
    public void Set(FrameDesign next,int selection){design=next;selected=selection;sections.Clear();Error=next.Validate();try{if(Error==null){elevation=FrameGeometry.Elevation(next);foreach(var cut in next.Cuts)sections.Add((cut,FrameGeometry.Section(next,cut)));}}catch(Exception e){Error=e.Message;}Invalidate();}
    PointF ToScreen(P2 p)=>new(area.Left+(float)(p.X*scale),area.Bottom-(float)(p.Y*scale));
    void Draw(Graphics g,FramePath path,Func<P2,PointF> map,Pen pen){var pts=path.Sample().Select(map).ToArray();if(pts.Length>1)g.DrawLines(pen,pts);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;if(design==null||Error!=null){TextRenderer.DrawText(g,Error??"Importer les sections pour commencer.",Font,new Rectangle(15,15,Width-30,Height-30),UiTheme.Muted,TextFormatFlags.WordBreak);return;}
        var d=design;double depth=Math.Max(d.Profile!.Depth,d.LeafProfile?.Depth??0),gap=150;int vCount=sections.Count(s=>s.Cut.Vertical),hCount=sections.Count-vCount;
        double fullW=d.Width+vCount*(depth+gap),fullH=d.Height+hCount*(depth+gap);
        scale=Math.Min((Width-70)/fullW,(Height-70)/fullH);if(scale<=0)return;area=new(30,30,(float)(d.Width*scale),(float)(d.Height*scale));
        using var outline=new Pen(UiTheme.Ink,1.3f);foreach(var path in elevation)Draw(g,path,ToScreen,outline);
        TextRenderer.DrawText(g,$"ÉLÉVATION  {d.Width:0.##} × {d.Height:0.##}",Font,new Point(30,(int)area.Bottom+8),UiTheme.Ink);
        for(int i=0;i<d.Cuts.Length;i++){var c=d.Cuts[i];using var pen=new Pen(i==selected?Color.FromArgb(206,80,38):UiTheme.Accent,i==selected?2:1.2f){DashStyle=System.Drawing.Drawing2D.DashStyle.DashDot};var a=ToScreen(c.Vertical?new(c.Station,0):new(0,c.Station));var b=ToScreen(c.Vertical?new(c.Station,d.Height):new(d.Width,c.Station));g.DrawLine(pen,a,b);
            using var arrow=new Pen(pen.Color,1.5f){EndCap=System.Drawing.Drawing2D.LineCap.ArrowAnchor};if(c.Vertical){g.DrawLine(arrow,b.X,b.Y-8,b.X+(c.Reverse?-16:16),b.Y-8);TextRenderer.DrawText(g,c.Id,Font,new Point((int)b.X-5,(int)b.Y-24),pen.Color);}else{g.DrawLine(arrow,a.X-8,a.Y,a.X-8,a.Y+(c.Reverse?-16:16));TextRenderer.DrawText(g,c.Id,Font,new Point((int)a.X-25,(int)a.Y-8),pen.Color);}}
        int vi=0,hi=0;foreach(var entry in sections){var c=entry.Cut;float x=c.Vertical?area.Right+(float)((gap+vi++*(depth+gap))*scale):area.Left,y=c.Vertical?area.Bottom:area.Bottom+(float)((depth+gap+hi++*(depth+gap))*scale);var pts=entry.Paths.SelectMany(p=>p.Sample()).ToArray();double minX=pts.Min(p=>p.X),minY=pts.Min(p=>p.Y);using var pen=new Pen(UiTheme.Accent,1.2f);foreach(var path in entry.Paths)Draw(g,path,q=>new(x+(float)((q.X-minX)*scale),y-(float)((q.Y-minY)*scale)),pen);TextRenderer.DrawText(g,c.Id+"–"+c.Id,Font,new Point((int)x,(int)(c.Vertical?area.Top-18:y+8)),UiTheme.Accent);}
    }
    (int,double)? Pick(Point point,bool existingOnly){if(design==null||scale<=0||!area.Contains(point))return null;for(int i=0;i<design.Cuts.Length;i++){var c=design.Cuts[i];double position=c.Vertical?(point.X-area.Left)/scale:(area.Bottom-point.Y)/scale;if(Math.Abs(position-c.Station)*scale<7)return(i,Math.Clamp(position,.01,(c.Vertical?design.Width:design.Height)-.01));}if(existingOnly)return null;return(-1,0);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||design==null)return;var hit=Pick(e.Location,false);if(hit==null)return;if(hit.Value.Item1>=0){drag=hit.Value.Item1;CutChanged?.Invoke(drag,hit.Value.Item2);Capture=true;}else{ // The selected radio determines the new line; send both coordinates via a temporary station mapping.
            CutChanged?.Invoke(-1,NewVertical?(e.X-area.Left)/scale:(area.Bottom-e.Y)/scale);
        }}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(drag<0||design==null||scale<=0)return;var c=design.Cuts[drag];CutChanged?.Invoke(drag,Math.Clamp(c.Vertical?(e.X-area.Left)/scale:(area.Bottom-e.Y)/scale,.01,(c.Vertical?design.Width:design.Height)-.01));}
    protected override void OnMouseUp(MouseEventArgs e){drag=-1;Capture=false;base.OnMouseUp(e);}
}
