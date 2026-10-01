namespace VFL.VideoApresentacao;

public sealed class ClientProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Novo cliente";
    public string IntroPath { get; set; } = "";
    public string OutroPath { get; set; } = "";
    public string LogoPath { get; set; } = "";
    public string MusicPath { get; set; } = "";
    public double IntroImageSeconds { get; set; } = 4;
    public double OutroImageSeconds { get; set; } = 4;
    public double FadeSeconds { get; set; } = 1;
    public double MusicVolumePercent { get; set; } = 18;
    public double OriginalVolumePercent { get; set; } = 100;
    public int LogoWidth { get; set; } = 220;
    public int LogoMarginLeft { get; set; } = 30;
    public int LogoMarginBottom { get; set; } = 30;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Crf { get; set; } = 18;

    public override string ToString() => Name;
}
