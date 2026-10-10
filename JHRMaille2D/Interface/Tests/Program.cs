using System.Reflection;
using System.Drawing.Imaging;
using System.Globalization;
using JhrMailleUi;
static class Program
{
 static void Check(bool v,string msg){if(!v)throw new Exception(msg);Console.WriteLine("OK "+msg);}
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(o)!;
 static void CatalogueTests(MeshSettings initial,string output)
 {
  Check(JakobCatalogue.Items.Count==202,"202 références catalogue uniques");
  var expected=new Dictionary<string,int>{{"Webnet Micro",36},{"Webnet Duplex avec douilles",41},{"Webnet standard avec douilles",14},{"Webnet sans douilles",62},{"Webnet Duplex sans douilles",41},{"Webnet Micro en rouleau",8}};
  foreach(var pair in expected)Check(JakobCatalogue.Items.Count(x=>x.Family==pair.Key)==pair.Value,pair.Key+" : "+pair.Value);
  var mounting=initial with{Lace=4,Gap=20,Angle=32,Stops=7,AutoCorners=false};
  foreach(var r in JakobCatalogue.Items){
   var applied=r.Apply(mounting);
   if(applied.Validate()!=null||!r.Matches(applied)||applied.Gap!=20||applied.Lace!=4||applied.Angle!=32||applied.Stops!=7||applied.AutoCorners)throw new Exception("Référence invalide "+r.Reference);
   if(r.RollHeightMm!=null){var basis=JakobCatalogue.Find(r.BaseReference)!;if(r.Mw!=basis.Mw||r.Ml!=basis.Ml||r.Wire!=basis.Wire)throw new Exception("Rouleau erroné "+r.Reference);}
  }
  Check(true,"Les 202 références conservent les réglages de montage ; rouleaux conformes aux mailles de base");
  Check(JakobCatalogue.Find("20260-0150-060")!.Ml==105.66,"ML exact du catalogue, sans approximation géométrique");
  using(var f=new MeshForm(initial)){
   Check(Field<ComboBox>(f,"family").SelectedIndex==0&&Field<MeshPreview>(f,"preview").Settings.Width==104,"Ancien réglage 104 × 60 conservé en saisie libre");
  }
  foreach(var r in JakobCatalogue.Items){
   using var f=new MeshForm(mounting);
   var family=Field<ComboBox>(f,"family");var diameter=Field<ComboBox>(f,"diameter");var reference=Field<ComboBox>(f,"reference");
   family.SelectedItem=r.Family;diameter.SelectedItem=r.Wire;reference.SelectedItem=reference.Items.Cast<JakobMesh>().Single(x=>x.Reference==r.Reference);
   var got=Field<MeshPreview>(f,"preview").Settings;
   if(!r.Matches(got)||got.ProductReference!=r.Reference||got.Gap!=20||got.Lace!=4)throw new Exception("Interface erronée "+r.Reference);
   if(reference.Items.Cast<JakobMesh>().Any(x=>x.Family!=r.Family||x.Wire!=r.Wire))throw new Exception("Filtre incorrect");
   var numbers=Field<Dictionary<string,NumericUpDown>>(f,"numbers");
   if(numbers["wire"].Enabled||numbers["width"].Enabled||numbers["height"].Enabled||Field<ComboBox>(f,"mode").Enabled)throw new Exception("Référence modifiable sans saisie libre");
   family.SelectedIndex=0;
   if(Field<MeshPreview>(f,"preview").Settings.ProductReference!=""||!numbers["width"].Enabled||numbers["width"].Value!=(decimal)r.Ml)throw new Exception("Retour libre incorrect");
  }
  Check(true,"Les 202 choix fonctionnent dans l’interface, filtrent les références et reviennent en saisie libre");
  foreach(string familyName in expected.Keys){
   var r=JakobCatalogue.Items.First(x=>x.Family==familyName);
   using var f=new MeshForm(r.Apply(mounting));
   Check(Field<ComboBox>(f,"reference").SelectedItem is JakobMesh selected&&selected.Reference==r.Reference,"Restauration "+r.Reference);
   using var timer=new System.Windows.Forms.Timer{Interval=100};
   timer.Tick+=(_,_)=>{timer.Stop();using var bitmap=new Bitmap(f.Width,f.Height);f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(Path.Combine(output,"catalogue-"+r.Reference+".png"));f.AcceptButton!.PerformClick();};timer.Start();
   Check(f.ShowDialog()==DialogResult.OK&&f.Settings.ProductReference==r.Reference,"Validation référence "+r.Reference);
  }
  using(var f=new MeshForm(initial)){
   Field<ComboBox>(f,"family").SelectedItem="Webnet Duplex sans douilles";
   using var timer=new System.Windows.Forms.Timer{Interval=60};timer.Tick+=(_,_)=>{timer.Stop();f.CancelButton!.PerformClick();};timer.Start();
   Check(f.ShowDialog()==DialogResult.Cancel&&f.Settings==initial,"Annuler un choix catalogue conserve les paramètres initiaux");
  }
  using(var f=new MeshForm(initial with{ProductReference="20260-0150-060"}))Check(Field<ComboBox>(f,"family").SelectedIndex==0,"Référence périmée invalidée quand les dimensions diffèrent");
 }
 [STAThread] static void Main(string[] args)
 {
  ApplicationConfiguration.Initialize();string output=args[0];Directory.CreateDirectory(output);
  var s=new MeshSettings();Check(s.Validate()==null,"Valeurs existantes acceptées");
  using(var f=new MeshForm(s)){
   var nav=Field<ToolNavigation>(f,"navigation");
   Check(nav.SelectedKey=="filet-inox-losange","L’outil Filet inox losange est sélectionné à l’ouverture");
   var buttons=Field<Dictionary<string,Button>>(nav,"buttons");
   Check(buttons.Count==1&&buttons.Values.Single().Text=="Filet inox losange","Une colonne d’outils avec le nom demandé");
   using var timer=new System.Windows.Forms.Timer{Interval=100};
   timer.Tick+=(_,_)=>{timer.Stop();buttons.Values.Single().PerformClick();Check(f.Visible&&Field<MeshPreview>(f,"preview").Settings==s,"Cliquer sur l’outil conserve la fenêtre et les réglages");f.CancelButton!.PerformClick();};timer.Start();
   Check(f.ShowDialog()==DialogResult.Cancel&&f.Settings==s,"La navigation conserve l’annulation");
  }
  CatalogueTests(s,output);
  Check((s with{Wire=20}).Validate()!=null,"Câble trop gros refusé");Check((s with{Width=2,Woven=true}).Validate()!=null,"Jonction tressée trop grande refusée");
  Check((s with{Width=double.NaN}).Validate()!=null,"Valeur non finie refusée");
  foreach(bool woven in new[]{false,true})foreach(double d in new[]{1.5,3.0})
  {
   using var b=new Bitmap(1100,650);using var g=Graphics.FromImage(b);MeshRenderer.Paint(g,new Rectangle(0,0,b.Width,b.Height),s with{Woven=woven,Wire=d});
   b.Save(Path.Combine(output,$"apercu-{(woven?"tresse":"bagues")}-d{d.ToString(CultureInfo.InvariantCulture)}.png"),ImageFormat.Png);
  }
  using(var f=new MeshForm(s with{Woven=true}))
  {
   var preview=Field<MeshPreview>(f,"preview");Check(preview.Settings.Woven,"Aperçu initial tressé");
   var numbers=Field<Dictionary<string,NumericUpDown>>(f,"numbers");numbers["wire"].Value=3;Check(preview.Settings.Wire==3,"Changement du diamètre met à jour l’aperçu");
   numbers["width"].Value=120;Check(preview.Settings.Width==120,"Changement de largeur propagé");
   Field<ComboBox>(f,"mode").SelectedIndex=0;Check(!preview.Settings.Woven && numbers["sleeveL"].Enabled,"Mode bagues et champs associés");
   numbers["wire"].Value=20;Check(!Field<Button>(f,"accept").Enabled,"Validation impossible pour une maille incompatible");
   numbers["wire"].Value=2;
   using var timer=new System.Windows.Forms.Timer{Interval=60};timer.Tick+=(_,_)=>{timer.Stop();Field<Button>(f,"accept").PerformClick();};timer.Start();
   Check(f.ShowDialog()==DialogResult.OK && f.Settings.Width==120 && f.Settings.Wire==2,"Valider ferme la fenêtre et retourne les valeurs");
  }
  using(var f=new MeshForm(s with{Woven=true}))
  {
   using var timer=new System.Windows.Forms.Timer{Interval=150};timer.Tick+=(_,_)=>{timer.Stop();using var b=new Bitmap(f.Width,f.Height);f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(output,"interface.png"),ImageFormat.Png);f.Close();};timer.Start();f.ShowDialog();
  }
  foreach(string action in new[]{"bouton","croix"})
  {
   using var f=new MeshForm(s);using var timer=new System.Windows.Forms.Timer{Interval=60};
   timer.Tick+=(_,_)=>{timer.Stop();Field<Dictionary<string,NumericUpDown>>(f,"numbers")["wire"].Value=2;if(action=="bouton")((Button)f.CancelButton!).PerformClick();else f.Close();};timer.Start();
   Check(f.ShowDialog()==DialogResult.Cancel && f.Settings==s,"Annuler par "+action+" conserve les réglages initiaux");
  }
  Console.WriteLine("DONE");
 }
}
