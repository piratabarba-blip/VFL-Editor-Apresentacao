using VFL.VideoApresentacao;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var media = Path.Combine(root, "teste-render");
var profile = new ClientProfile
{
    Name = "Teste",
    IntroPath = Path.Combine(media, "intro.png"),
    OutroPath = Path.Combine(media, "final.mp4"),
    LogoPath = Path.Combine(media, "logo.png"),
    MusicPath = Path.Combine(media, "musica.mp3"),
    Width = 640,
    Height = 360,
    LogoWidth = 100,
    FadeSeconds = .5,
    IntroImageSeconds = 1.2
};
var sequence = new List<MediaSequenceItem>
{
    new() { Path = Path.Combine(media, "principal.mp4") },
    new() { Path = Path.Combine(media, "meio.png"), ImageSeconds = 1.4 },
    new() { Path = Path.Combine(media, "principal2.mp4") },
    new() { Path = Path.Combine(media, "principal3.mp4") }
};
var output = Path.Combine(media, "resultado.mp4");
var progress = new Progress<RenderProgress>(p => Console.WriteLine($"{p.Percent:0}% {p.Message}"));
await new MediaRenderer().RenderAsync(profile, sequence, output, progress, CancellationToken.None);
if (!File.Exists(output) || new FileInfo(output).Length < 1000) throw new Exception("Arquivo de teste não foi produzido.");
Console.WriteLine($"OK: {output} ({new FileInfo(output).Length} bytes)");
