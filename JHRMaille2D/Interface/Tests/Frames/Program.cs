using System.Reflection;
using System.Text.Json;
using JhrMailleUi.Frames;
namespace JhrMailleUi { public class ToolNavigation:UserControl{} }
internal static class Program
{
 static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("OK "+name);}
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(o)!;
 static void Mouse(FramePreview p,string eventName,int x,int y)=>typeof(FramePreview).GetMethod(eventName,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(p,[new MouseEventArgs(MouseButtons.Left,1,x,y,0)]);
 [STAThread]static void Main(string[] args)
 {
  Application.SetHighDpiMode(HighDpiMode.SystemAware);Application.EnableVisualStyles();
  var profiles=JsonSerializer.Deserialize<List<FrameProfile>>(File.ReadAllText(args[0]),FrameDesign.Json)!;string output=args[1];Directory.CreateDirectory(output);
  var tube=profiles.Single(p=>p.Name=="Tube soudé / Tube 40x40 ép.3");var rolled=profiles.First(p=>p.Name=="IPE / 100");var jansen=profiles.Single(p=>p.Name=="30011");
  Check(Math.Abs(tube.Width-40)<.001&&Math.Abs(tube.Depth-40)<.001,"Vraie section du gabarit : 40 × 40");
  var intervals=tube.MaterialAt(20);Check(intervals.Length==2&&Math.Abs(intervals[0].Item2-3)<.05&&Math.Abs(intervals[1].Item1-37)<.05,"Coupe longitudinale : deux parois 3 mm, vide préservé");
  var d=new FrameDesign{Profile=tube,Width=1200,Height=2000,Mullions=[600],Transoms=[1000],Cuts=[new("A",true,300),new("B",false,500)]};
  var section=FrameGeometry.Section(d,d.Cuts[0]);Check(section.Length==tube.Paths.Length*3,"Coupe verticale traverse bas, traverse et haut");
  var reversed=FrameGeometry.Section(d,d.Cuts[0] with{Reverse=true});Check(section.SelectMany(p=>p.Points).Zip(reversed.SelectMany(p=>p.Points),(a,b)=>Math.Abs(a.X+b.X)<1e-8&&Math.Abs(a.Y-b.Y)<1e-8).All(v=>v),"Inversion du regard respecte la section");
  foreach(var p in new[]{tube,rolled,jansen})foreach(int turns in Enumerable.Range(0,4))foreach(bool mirror in new[]{false,true}){var oriented=p.Orient(turns,mirror);Check(Math.Abs(oriented.Width-(turns%2==0?p.Width:p.Depth))<.03&&Math.Abs(oriented.Depth-(turns%2==0?p.Depth:p.Width))<.03,"Orientation "+p.Name+" "+turns+" "+mirror);}
  try{FrameGeometry.Section(d,new("X",true,10));throw new Exception("Angle accepté");}catch(ArgumentException){Check(true,"Coupe de coin à 45° refusée explicitement");}
  Check((d with{Mullions=[30]}).Validate()!=null,"Division rencontrant le bord refusée");Check((d with{Width=double.NaN}).Validate()!=null,"Valeurs non finies refusées");
  var wideLeaf=tube with{Paths=tube.Paths.Select(path=>path.Map(q=>new(q.X*2,q.Y*2))).ToArray()};
  Check((d with{Tool=FrameTool.SheetDoor,LeafProfile=wideLeaf,Mullions=[500,560]}).Validate()!=null,"Divisions espacées de 60 mm refusées avec ossature 80 mm");
  foreach(var tool in Enum.GetValues<FrameTool>()){
   var initial=d with{Tool=tool,Profile=tool==FrameTool.Joinery?jansen:tool==FrameTool.Rolled?rolled:tube,LeafProfile=tube};
   Check(initial.Validate()==null,"Configuration valide "+tool);Check(FrameGeometry.Section(initial,initial.Cuts[1]).Length>0,"Section réelle "+tool);
   var choices=new List<FrameChoice>{new(tube.Name,tube.Family,tube),new(rolled.Name,rolled.Family,rolled),new(jansen.Name,jansen.Family,jansen)};
   using var f=new FrameDialog(initial,choices,(_,_)=>throw new Exception("Import inattendu"),_=>[]);
   using var timer=new System.Windows.Forms.Timer{Interval=100};timer.Tick+=(_,_)=>{timer.Stop();var preview=Field<FramePreview>(f,"preview");Check(preview.Error==null,"Aperçu valide "+tool);var numbers=Field<Dictionary<string,NumericUpDown>>(f,"numbers");numbers["width"].Value=1500;Check(Field<FrameDesign>(preview,"design").Width==1500,"Modification propagée "+tool);
    using var bitmap=new Bitmap(f.Width,f.Height);f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(output,"cadres-"+tool+".png"));
    var area=Field<RectangleF>(preview,"area");double scale=Field<double>(preview,"scale");int x=(int)(area.Left+400*scale),y=(int)(area.Bottom-600*scale);Mouse(preview,"OnMouseDown",x,y);Mouse(preview,"OnMouseUp",x,y);Check(Field<FrameDesign>(preview,"design").Cuts.Length==3,"Ligne créée en cliquant "+tool);
    area=Field<RectangleF>(preview,"area");scale=Field<double>(preview,"scale");x=(int)(area.Left+400*scale);y=(int)(area.Bottom-600*scale);Mouse(preview,"OnMouseDown",x,y);Mouse(preview,"OnMouseMove",(int)(area.Left+450*scale),y);Mouse(preview,"OnMouseUp",x,y);Check(Math.Abs(Field<FrameDesign>(preview,"design").Cuts[^1].Station-450)<10,"Déplacement de ligne "+tool);
    ((Button)f.CancelButton!).PerformClick();};timer.Start();Check(f.ShowDialog()==DialogResult.Cancel&&f.Design==initial,"Annulation conserve l’original "+tool);
  }
  Console.WriteLine("PASS frame geometry and UI");
 }
}
