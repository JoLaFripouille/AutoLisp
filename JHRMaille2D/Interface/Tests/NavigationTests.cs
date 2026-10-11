using System.Reflection;
using JhrMailleUi;
static class NavigationTests
{
    internal static void Run(string output)
    {
        foreach(float scale in new[]{1f,1.25f,1.5f}){
            using var f=new MeshForm(new MeshSettings());f.LiveMode("Test des dimensions et du bandeau");
            using var timer=new System.Windows.Forms.Timer{Interval=150};Exception? failure=null;
            timer.Tick+=(_,_)=>{timer.Stop();try{
                f.Scale(new SizeF(scale,scale));
                var nav=(ToolNavigation)typeof(MeshForm).GetField("navigation",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(f)!;
                var buttons=(Dictionary<string,Button>)typeof(ToolNavigation).GetField("buttons",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(nav)!;
                foreach(int width in new[]{740,790,1100}){
                    f.Width=(int)(width*scale);f.PerformLayout();Application.DoEvents();
                    foreach(var pair in buttons){nav.SelectTool(pair.Key);Application.DoEvents();var bounds=pair.Value.RectangleToScreen(pair.Value.ClientRectangle);
                        for(Control? parent=pair.Value.Parent;parent!=null;parent=parent.Parent)if(!parent.RectangleToScreen(parent.ClientRectangle).Contains(bounds))throw new Exception("Bouton coupe "+pair.Key+" scale="+scale+" width="+width);
                        if(pair.Value.Height>110*scale)throw new Exception("Bouton etire "+pair.Key);
                    }
                }
                using var image=new Bitmap(f.Width,f.Height);f.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(output,"navigation-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
                Console.WriteLine("OK navigation entiere a "+scale+" et trois largeurs");
            }catch(Exception e){failure=e;}finally{f.Close();}};timer.Start();f.ShowDialog();if(failure!=null)throw failure;
        }
        Console.WriteLine("NAVIGATION TESTS DONE");
    }
}
