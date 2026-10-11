using JhrMailleUi;
using static JhrMailleUi.GeometryRuntime;
static class MathChecks {
 static void Require(bool ok,string note){if(!ok)throw new Exception(note);Console.WriteLine("PASS "+note);}
 static void Main(){
  object? box=L(L(-100d,-100d),L(100d,-100d),L(100d,100d),L(-100d,100d));
  var g=new GeometryRuntime();Require(Math.Abs(D(g.Invoke("wm:area",box))-40000)<1e-7,"aire du cadre");
  Require(Truth(g.Invoke("wm:inside",L(0d,0d),box))&&!Truth(g.Invoke("wm:inside",L(110d,0d),box)),"test interieur/exterieur");
  Require(!Truth(g.Invoke("wm:selfcross",box)),"cadre simple accepte");
  Require(Truth(g.Invoke("wm:selfcross",L(L(-1d,-1d),L(1d,1d),L(-1d,1d),L(1d,-1d)))),"cadre croise refuse");
  g.Root.Bind("wm:inner",box);g.Root.Bind("wm:outer",box);g.Root.Bind("wm:shapes",null);g.Root.Bind("rf:masks",null);g.Root.Bind("rf:count",0d);g.Root.Bind("*wm:tool*",2d);
  g.Invoke("rf:expanded",box,L(40d,20d,2d,1.5d,0d,0d,0d,0d));
  Require(Items(g.Root.Get("wm:shapes")).Any(),"deploye C# genere sans AutoCAD ni LISP");
  using var cancel=new CancellationTokenSource();cancel.Cancel();bool cancelled=false;
  try{new GeometryRuntime(cancel.Token).Invoke("wm:area",box);}catch(OperationCanceledException){cancelled=true;}
  Require(cancelled,"calcul obsolete annule");
  Console.WriteLine("DONE");
 }
}
