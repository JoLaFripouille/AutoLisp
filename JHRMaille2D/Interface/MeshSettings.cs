namespace JhrMailleUi;

public sealed record MeshSettings(double Width=104, double Height=60, double Wire=1.5,
    double Lace=2, double Gap=20, double Angle=0, bool Woven=false, bool AutoCorners=true,
    double SleeveLength=5.5, double SleeveHeight=5.7, int Stops=12, string ProductReference="")
{
    public string? Validate()
    {
        if (new[]{Width,Height,Wire,Lace,Gap,Angle,SleeveLength,SleeveHeight}.Any(x=>!double.IsFinite(x)))
            return "Saisir des nombres finis.";
        if (Width<=0 || Height<=0 || Wire<=0 || Lace<=0 || Gap<=0 || SleeveLength<=0 || SleeveHeight<=0 || Stops<=0)
            return "Les dimensions et diamètres doivent être strictement positifs.";
        double join = Woven ? Wire*5 : SleeveLength;
        if (Width<=join+2*Wire || Height<=4*Wire || Math.Min(Width,Height)<=Math.Max(Wire,Lace)*4)
            return "La maille est trop petite par rapport au câble et à sa jonction.";
        if (!Woven && (SleeveHeight<Wire || SleeveLength<Wire))
            return "L’enveloppe de la bague doit être au moins égale au diamètre du câble.";
        if (Math.Abs(Angle)>360) return "L’orientation doit être comprise entre −360° et 360°.";
        return null;
    }
}
