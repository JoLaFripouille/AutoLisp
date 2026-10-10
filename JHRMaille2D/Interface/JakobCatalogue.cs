using System.Text.Json;
using System.Reflection;
namespace JhrMailleUi;

public sealed record JakobMesh
{
    public string Reference { get; init; } = "";
    public string Family { get; init; } = "";
    public double Wire { get; init; }
    public double Mw { get; init; }
    public double Ml { get; init; }
    public bool Woven { get; init; }
    public double? SleeveLength { get; init; }
    public double? SleeveHeight { get; init; }
    public string Construction { get; init; } = "";
    public string Material { get; init; } = "";
    public double WeightKgM2 { get; init; }
    public double MaxProductionLengthM { get; init; }
    public int PdfPage { get; init; }
    public string BaseReference { get; init; } = "";
    public double? RollHeightMm { get; init; }
    public double? RollLengthMm { get; init; }
    public MeshSettings Apply(MeshSettings s) => s with {
        Width=Ml, Height=Mw, Wire=Wire, Woven=Woven,
        SleeveLength=SleeveLength??s.SleeveLength, SleeveHeight=SleeveHeight??s.SleeveHeight,
        ProductReference=Reference };
    public bool Matches(MeshSettings s) => Math.Abs(s.Width-Ml)<1e-6 && Math.Abs(s.Height-Mw)<1e-6
        && Math.Abs(s.Wire-Wire)<1e-6 && s.Woven==Woven
        && (Woven || (Math.Abs(s.SleeveLength-SleeveLength!.Value)<1e-6 && Math.Abs(s.SleeveHeight-SleeveHeight!.Value)<1e-6));
    public override string ToString() => $"{Reference} · MW {Mw:g} × ML {Ml:g} mm";
}
public sealed record JakobCatalogueData(string Edition,string VerifiedDate,string Source,JakobMesh[] Items);
public static class JakobCatalogue
{
    public static readonly JakobCatalogueData Data=Load();
    public static IReadOnlyList<JakobMesh> Items=>Data.Items;
    public static JakobMesh? Find(string reference)=>Items.FirstOrDefault(x=>x.Reference==reference);
    static JakobCatalogueData Load()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("JhrMaille.JakobWebnet.json")
            ??throw new InvalidOperationException("Catalogue Jakob embarqué absent.");
        var data=JsonSerializer.Deserialize<JakobCatalogueData>(stream)??throw new InvalidOperationException("Catalogue Jakob invalide.");
        if(data.Items.Length==0 || data.Items.Select(x=>x.Reference).Distinct().Count()!=data.Items.Length
            || data.Items.Any(x=>x.Mw<=0 || x.Ml<=0 || x.Wire<=0 || (!x.Woven && (x.SleeveLength is not >0 || x.SleeveHeight is not >0))))
            throw new InvalidOperationException("Les références ou dimensions du catalogue Jakob sont invalides.");
        return data;
    }
}
