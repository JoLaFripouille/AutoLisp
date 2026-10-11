namespace JhrMailleUi;
public enum FillTool { Mesh, Square, Expanded, Perforated }
public sealed record PatternSettings(double A,double B,double Wire,double Thickness,double Gap=0,double Angle=0,double Margin=0,int Pattern=0)
{
    public double[] Values()=>[A,B,Wire,Thickness,Gap,Angle,Margin,Pattern];
    public static PatternSettings From(double[] v)=>new(v[0],v[1],v[2],v[3],v[4],v[5],v[6],(int)v[7]);
    public string? Validate(FillTool tool){
        if(Values().Any(x=>!double.IsFinite(x)))return "Saisir des nombres finis.";
        if(A<=0||B<=0||Wire<=0||Thickness<=0||Gap<0||Margin<0||Math.Abs(Angle)>360)return "VÃ©rifier les dimensions positives, le retrait et lâ€™orientation.";
        if(tool==FillTool.Square && (Pattern is <0 or >2 || Wire>=A))return "Le diamÃ¨tre du fil doit Ãªtre infÃ©rieur Ã  lâ€™ouverture libre.";
        if(tool==FillTool.Expanded && (A<B || Wire>=Math.Min(A,B)/3 || Margin>=A/2 || Pattern is <0 or >1))return "La grande diagonale doit dÃ©passer la petite ; la laniÃ¨re doit Ãªtre infÃ©rieure au tiers de la petite diagonale.";
        if(tool==FillTool.Perforated && (A>=B || Pattern is <0 or >2))return "Le trou doit Ãªtre plus petit que son entraxe.";
        return null;
    }
}
public sealed record FillSettings
{
    public FillTool Tool {get;init;}
    public MeshSettings Mesh {get;init;}=new();
    public PatternSettings Square {get;init;}=new(30,30,3,1);
    public PatternSettings Expanded {get;init;}=new(40,20,2,1.5);
    public PatternSettings Perforated {get;init;}=new(6,10,1,2,Margin:10,Pattern:1);
    public PatternSettings Pattern(FillTool tool)=>tool switch {FillTool.Square=>Square,FillTool.Expanded=>Expanded,FillTool.Perforated=>Perforated,_=>throw new ArgumentException("Outil de motif attendu.")};
    public FillSettings WithPattern(FillTool tool,PatternSettings value)=>tool switch {FillTool.Square=>this with{Square=value},FillTool.Expanded=>this with{Expanded=value},FillTool.Perforated=>this with{Perforated=value},_=>throw new ArgumentException()};
    public double[] ExtraValues()=>new[]{(double)Tool}.Concat(Square.Values()).Concat(Expanded.Values()).Concat(Perforated.Values()).ToArray();
    public string? Validate()=>Tool==FillTool.Mesh?Mesh.Validate():Pattern(Tool).Validate(Tool);
}
