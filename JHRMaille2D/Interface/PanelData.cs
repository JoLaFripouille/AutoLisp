using System.Text.Json;
namespace JhrMailleUi;
public sealed record Vertex(double X,double Y,double Bulge=0);
public sealed record PanelData
{
    public int Schema {get;init;}=1;
    public string Engine {get;init;}="1.4.3";
    public FillSettings Settings {get;init;}=new();
    public Vertex[] Inner {get;init;}=[];
    public Vertex[]? Outer {get;init;}
    public bool LegacyDefaults {get;init;}
    public string[] SourceHandles {get;init;}=[];
    public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    public string ToJson()=>JsonSerializer.Serialize(this,Json);
    public static PanelData Parse(string json)=>JsonSerializer.Deserialize<PanelData>(json,Json)??throw new ArgumentException("Paramètres de panneau absents.");
    public void Validate(){if(Schema!=1)throw new ArgumentException("Version de panneau non prise en charge.");if(!Enum.IsDefined(Settings.Tool))throw new ArgumentException("Outil inconnu.");var e=Settings.Validate();if(e!=null)throw new ArgumentException(e);foreach(var c in new[]{Inner,Outer})if(c!=null&&(c.Length<3||c.Length>2000||c.Any(v=>!double.IsFinite(v.X)||!double.IsFinite(v.Y)||!double.IsFinite(v.Bulge))))throw new ArgumentException("Contour invalide ou trop complexe.");if(!(Settings.Tool==FillTool.Square&&Settings.Square.Pattern==0)&&Outer==null)throw new ArgumentException("Sélectionner aussi le contour extérieur du cadre.");}
}
