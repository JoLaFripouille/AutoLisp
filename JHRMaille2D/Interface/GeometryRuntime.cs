using System.Globalization;
namespace JhrMailleUi;

// Small, compiled compatibility layer for the translated geometric mathematics.
// Immutable cells preserve the approved list/association semantics. No parser or evaluator at runtime.
partial class GeometryRuntime
{
    internal sealed record Symbol(string Name);
    internal sealed record Cell(object? Head, object? Tail);
    internal delegate object? Lambda(object?[] args);
    readonly Dictionary<string,Func<Scope,object?[],object?>> functions=new(StringComparer.OrdinalIgnoreCase);
    internal sealed class Scope
    {
        readonly Scope? parent; readonly Dictionary<string,object?> values=new(StringComparer.OrdinalIgnoreCase);
        internal Scope(Scope? p=null,object? names=null,object?[]? args=null,object? locals=null)
        {parent=p;int i=0;foreach(var n in Items(names))values[(string)n!]=args!=null&&i<args.Length?args[i++]:null;foreach(var n in Items(locals))values[(string)n!]=null;}
        internal object? Get(string name)=>values.TryGetValue(name,out var v)?v:parent?.Get(name);
        internal object? Set(string name,object? value){if(values.ContainsKey(name)||parent==null)values[name]=value;else parent.Set(name,value);return value;}
        internal void Bind(string name,object? value)=>values[name]=value;
    }
    internal Scope Root {get;}=new();
    internal Dictionary<string,object?> Components {get;}=new();
    long iterations; readonly CancellationToken cancellation; readonly long maxIterations=100_000_000;
    internal GeometryRuntime(CancellationToken token=default){cancellation=token;RegisterGeometry();functions["rf:expanded"]=(c,a)=>FastExpanded.Generate(this,a[0],a[1]);}
    void Check(){if(++iterations>maxIterations)throw new ArgumentException("Calcul trop complexe : diviser le panneau.");cancellation.ThrowIfCancellationRequested();}
    internal static object? Run(Func<object?> f)=>f();
    internal static bool Truth(object? v)=>v!=null && (v is not bool b||b);
    internal static object? Bool(bool v)=>v?true:null;
    internal static double D(object? v)=>Convert.ToDouble(v,CultureInfo.InvariantCulture);
    internal static int I(object? v)=>(int)D(v);
    internal static object? L(params object?[] a){object? v=null;for(int i=a.Length-1;i>=0;i--)v=new Cell(a[i],v);return v;}
    internal static IEnumerable<object?> Items(object? x){while(x is Cell c){yield return c.Head;x=c.Tail;}if(x!=null)throw new ArgumentException("Liste géométrique incohérente.");}
    internal static object? Car(object? x)=>x is Cell c?c.Head:null;
    internal static object? Cdr(object? x)=>x is Cell c?c.Tail:null;
    internal static bool Eq(object? a,object? b,double tol=0){if(a==null||b==null)return a==b;if(a is Cell ac && b is Cell bc)return Eq(ac.Head,bc.Head,tol)&&Eq(ac.Tail,bc.Tail,tol);if(a is string sa&&b is string sb)return sa==sb;if(a is Symbol s1&&b is Symbol s2)return s1.Name==s2.Name;if(a is bool || b is bool)return Equals(a,b);if(a is IConvertible&&b is IConvertible)return Math.Abs(D(a)-D(b))<=tol;return Equals(a,b);}
    object? Fn(Scope c,object? fn,params object?[] a)=>fn is Lambda f?f(a):Call(c,fn is Symbol s?s.Name:(string)fn!,a);
    internal object? Invoke(string name,params object?[] a)=>Call(Root,name,a);
    internal object? Call(Scope c,string n,object?[] a)
    {
        Check();if(functions.TryGetValue(n,out var f))return f(c,a);
        if(n.Length>=3&&n[0]=='c'&&n[^1]=='r'&&n[1..^1].All(x=>x is 'a' or 'd')){object? r=a[0];foreach(char op in n[1..^1].Reverse())r=op=='a'?Car(r):Cdr(r);return r;}
        switch(n){
        case "+":return a.Sum(D); case "*":return a.Aggregate(1d,(s,v)=>s*D(v));
        case "-":return a.Length==1?-D(a[0]):a.Skip(1).Aggregate(D(a[0]),(s,v)=>s-D(v));
        case "/":return a.Skip(1).Aggregate(D(a[0]),(s,v)=>s/D(v));
        case "1+":return D(a[0])+1;case "1-":return D(a[0])-1;
        case "=":return Bool(a.Skip(1).All(v=>Eq(a[0],v)));case "/=":return Bool(!Eq(a[0],a[1]));
        case "<":return Bool(D(a[0])<D(a[1]));case ">":return Bool(D(a[0])>D(a[1]));case "<=":return Bool(D(a[0])<=D(a[1]));case ">=":return Bool(D(a[0])>=D(a[1]));
        case "min":return a.Min(D);case "max":return a.Max(D);case "sqrt":return Math.Sqrt(D(a[0]));case "abs":return Math.Abs(D(a[0]));case "sin":return Math.Sin(D(a[0]));case "cos":return Math.Cos(D(a[0]));
        case "rem":return D(a[0])%D(a[1]);case "fix":return (double)I(a[0]);case "float":return D(a[0]);
        case "angle":{var p=Items(a[0]).Select(D).ToArray();var q=Items(a[1]).Select(D).ToArray();var r=Math.Atan2(q[1]-p[1],q[0]-p[0]);return r<0?r+2*Math.PI:r;}
        case "distance":{var p=Items(a[0]).Select(D).ToArray();var q=Items(a[1]).Select(D).ToArray();return Math.Sqrt(p.Zip(q,(x,y)=>(x-y)*(x-y)).Sum());}
        case "list":return L(a);case "cons":return new Cell(a[0],a[1]);case "append":return L(a.SelectMany(Items).ToArray());case "reverse":return L(Items(a[0]).Reverse().ToArray());
        case "last":return Items(a[0]).LastOrDefault();case "nth":return Items(a[1]).ElementAtOrDefault(I(a[0]));case "length":return (double)Items(a[0]).Count();
        case "assoc":return Items(a[1]).FirstOrDefault(v=>Eq(Car(v),a[0]));
        case "member":{object? x=a[1];while(x is Cell v){if(Eq(v.Head,a[0]))return x;x=v.Tail;}return null;}
        case "subst":return L(Items(a[2]).Select(v=>Eq(v,a[1])?a[0]:v).ToArray());
        case "equal":return Bool(Eq(a[0],a[1],a.Length>2?D(a[2]):0));case "not":return Bool(!Truth(a[0]));
        case "mapcar":{var lists=a.Skip(1).Select(x=>Items(x).ToArray()).ToArray();int count=lists.Min(x=>x.Length);return L(Enumerable.Range(0,count).Select(i=>Fn(c,a[0],lists.Select(v=>v[i]).ToArray())).ToArray());}
        case "apply":return Fn(c,a[0],Items(a[1]).ToArray());
        case "vl-every":return Bool(Items(a[1]).All(v=>Truth(Fn(c,a[0],v))));case "vl-some":{foreach(var v in Items(a[1])){var r=Fn(c,a[0],v);if(Truth(r))return r;}return null;}
        case "vl-remove":return L(Items(a[1]).Where(v=>!Eq(v,a[0])).ToArray());
        case "vl-sort":{var values=new List<object?>();foreach(var v in Items(a[0]))if(!values.Any(x=>Eq(x,v)))values.Add(v);values.Sort((x,y)=>Truth(Fn(c,a[1],x,y))?-1:Truth(Fn(c,a[1],y,x))?1:0);return L(values.ToArray());}
        case "strcat":return string.Concat(a);case "rtos":return D(a[0]).ToString("F"+(a.Length>2?I(a[2]):2),CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        case "vl-string-translate":{string r=(string)a[2]!;string from=(string)a[0]!,to=(string)a[1]!;return new string(r.Select(ch=>from.IndexOf(ch) is var idx&&idx>=0&&idx<to.Length?to[idx]:ch).ToArray());}
        case "princ":return a.FirstOrDefault();case "exit":throw new ArgumentException(Convert.ToString(c.Get("wm:reason"))??"Géométrie impossible.");
        case "rf:hidden-type":return true;
        case "tblsearch":return Bool(Components.ContainsKey((string)a[1]!));
        case "wm:block":Components[(string)a[0]!]=a[1];return true;
        default:throw new InvalidOperationException("Opération géométrique non traduite : "+n);
        }
    }
}
