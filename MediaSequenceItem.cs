namespace VFL.VideoApresentacao;

public sealed class MediaSequenceItem
{
    public string Path { get; set; } = "";
    public double ImageSeconds { get; set; } = 4;
    public bool IsImage => IsImagePath(Path);
    public string TypeLabel => IsImage ? "IMAGEM" : "VÍDEO";
    public string DurationLabel => IsImage ? $"{ImageSeconds:0.#} s" : "Duração original";

    public static bool IsImagePath(string path) =>
        new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" }
            .Contains(System.IO.Path.GetExtension(path).ToLowerInvariant());
}
