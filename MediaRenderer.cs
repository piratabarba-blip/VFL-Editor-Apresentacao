using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace VFL.VideoApresentacao;

public sealed record RenderProgress(double Percent, string Message);

public sealed class MediaRenderer
{
    public static string? FindTool(string name)
    {
        var exe = name + ".exe";
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Tools", exe),
            Path.Combine(AppContext.BaseDirectory, exe),
            exe
        };
        foreach (var candidate in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo(candidate, "-version")
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                if (process is null) continue;
                process.WaitForExit(3000);
                if (process.ExitCode == 0) return candidate;
            }
            catch { }
        }
        return null;
    }

    public async Task RenderAsync(ClientProfile profile, IReadOnlyList<MediaSequenceItem> sequence,
        string outputPath, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
    {
        var ffmpeg = FindTool("ffmpeg") ?? throw new InvalidOperationException(
            "FFmpeg não foi encontrado. Coloque ffmpeg.exe e ffprobe.exe na pasta Tools do programa.");
        var ffprobe = FindTool("ffprobe") ?? throw new InvalidOperationException("ffprobe.exe não foi encontrado.");
        Validate(profile, sequence);
        progress.Report(new RenderProgress(0, "Analisando a sequência..."));

        var sources = new List<MediaSource> { new(profile.IntroPath, profile.IntroImageSeconds, MediaRole.Intro) };
        sources.AddRange(sequence.Select(item => new MediaSource(item.Path, item.ImageSeconds, MediaRole.Content)));
        sources.Add(new MediaSource(profile.OutroPath, profile.OutroImageSeconds, MediaRole.Outro));

        var media = new List<MediaInfo>(sources.Count);
        foreach (var source in sources)
            media.Add(await ProbeAsync(ffprobe, source.Path, source.ImageSeconds, cancellationToken));

        var shortest = media.Min(item => item.Duration);
        if (shortest <= 0) throw new InvalidOperationException("Uma das mídias possui duração inválida.");
        var fade = Math.Min(Math.Max(0.05, profile.FadeSeconds), shortest / 2);
        var totalDuration = media.Sum(item => item.Duration) - fade * (media.Count - 1);
        var args = BuildArguments(profile, sources, outputPath, media, fade, totalDuration);

        var psi = new ProcessStartInfo(ffmpeg)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var errors = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            errors.AppendLine(e.Data);
            if (errors.Length > 16000) errors.Remove(0, 8000);
        };
        process.Start();
        process.BeginErrorReadLine();
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
        });

        while (!process.StandardOutput.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken) ?? "";
            if (!line.StartsWith("out_time_ms=", StringComparison.Ordinal) || !long.TryParse(line[12..], out var micros)) continue;
            var pct = Math.Clamp(micros / 1_000_000d / totalDuration * 100, 0, 99.5);
            progress.Report(new RenderProgress(pct, $"Renderizando {sequence.Count} item(ns)... {pct:0}%"));
        }

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("O FFmpeg não conseguiu gerar o vídeo.\n\n" + errors.ToString()[..Math.Min(errors.Length, 4000)]);
        progress.Report(new RenderProgress(100, "Vídeo concluído!"));
    }

    private static void Validate(ClientProfile profile, IReadOnlyList<MediaSequenceItem> sequence)
    {
        if (sequence.Count == 0) throw new InvalidOperationException("A sequência está vazia.");
        var required = new List<(string Name, string Path)>
        {
            ("abertura", profile.IntroPath), ("encerramento", profile.OutroPath), ("logo", profile.LogoPath)
        };
        required.AddRange(sequence.Select((item, index) => ($"item {index + 1} da sequência", item.Path)));
        var missing = required.Where(item => string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path))
            .Select(item => item.Name).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("Arquivo ausente: " + string.Join(", ", missing) + ".");
    }

    private static async Task<MediaInfo> ProbeAsync(string ffprobe, string path, double imageDuration, CancellationToken ct)
    {
        if (MediaSequenceItem.IsImagePath(path)) return new MediaInfo(imageDuration, false, true, 0, 0, 0);
        var psi = new ProcessStartInfo(ffprobe)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "-v", "error", "-show_entries", "format=duration:stream=codec_type,width,height,avg_frame_rate", "-of", "json", path })
            psi.ArgumentList.Add(argument);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível executar o ffprobe.");
        var json = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        using var document = JsonDocument.Parse(json);
        var durationText = document.RootElement.GetProperty("format").GetProperty("duration").GetString() ?? "0";
        var duration = double.Parse(durationText, CultureInfo.InvariantCulture);
        var streams = document.RootElement.GetProperty("streams").EnumerateArray().ToArray();
        var hasAudio = streams.Any(stream => stream.GetProperty("codec_type").GetString() == "audio");
        var video = streams.FirstOrDefault(stream => stream.GetProperty("codec_type").GetString() == "video");
        var width = video.ValueKind == JsonValueKind.Undefined || !video.TryGetProperty("width", out var widthNode) ? 0 : widthNode.GetInt32();
        var height = video.ValueKind == JsonValueKind.Undefined || !video.TryGetProperty("height", out var heightNode) ? 0 : heightNode.GetInt32();
        var fps = video.ValueKind == JsonValueKind.Undefined || !video.TryGetProperty("avg_frame_rate", out var fpsNode)
            ? 30 : ParseFrameRate(fpsNode.GetString());
        return new MediaInfo(duration, hasAudio, false, width, height, fps);
    }

    private static List<string> BuildArguments(ClientProfile profile, IReadOnlyList<MediaSource> sources,
        string output, IReadOnlyList<MediaInfo> media, double fade, double total)
    {
        var args = new List<string> { "-y", "-hide_banner", "-progress", "pipe:1", "-nostats" };
        for (var i = 0; i < sources.Count; i++) AddInput(args, sources[i].Path, media[i]);
        var logoIndex = sources.Count;
        args.AddRange(["-loop", "1", "-i", profile.LogoPath]);
        var musicIndex = -1;
        if (!string.IsNullOrWhiteSpace(profile.MusicPath) && File.Exists(profile.MusicPath))
        {
            musicIndex = logoIndex + 1;
            args.AddRange(["-stream_loop", "-1", "-i", profile.MusicPath]);
        }

        var ci = CultureInfo.InvariantCulture;
        string N(double value) => value.ToString("0.###", ci);
        var contentVideoIndex = Enumerable.Range(0, sources.Count)
            .FirstOrDefault(index => sources[index].Role == MediaRole.Content && !media[index].IsImage, -1);
        var formatIndex = contentVideoIndex >= 0
            ? contentVideoIndex
            : Enumerable.Range(0, media.Count).FirstOrDefault(index => !media[index].IsImage, -1);
        var format = formatIndex >= 0 ? media[formatIndex] : new MediaInfo(0, false, true, profile.Width, profile.Height, 30);
        var outputWidth = format.Width > 0 ? format.Width + format.Width % 2 : profile.Width;
        var outputHeight = format.Height > 0 ? format.Height + format.Height % 2 : profile.Height;
        var outputFps = format.FrameRate is > 0 and <= 120 ? format.FrameRate : 30;

        var filter = new StringBuilder();
        for (var i = 0; i < media.Count; i++)
        {
            filter.Append($"[{i}:v]scale={outputWidth}:{outputHeight}:force_original_aspect_ratio=decrease,");
            filter.Append($"pad={outputWidth}:{outputHeight}:(ow-iw)/2:(oh-ih)/2:black,fps={N(outputFps)},");
            filter.Append($"format=yuv420p,trim=duration={N(media[i].Duration)},setpts=PTS-STARTPTS[v{i}];");
        }

        var accumulatedDuration = media[0].Duration;
        var currentVideo = "v0";
        for (var i = 1; i < media.Count; i++)
        {
            var outputLabel = $"vx{i}";
            var offset = accumulatedDuration - fade;
            filter.Append($"[{currentVideo}][v{i}]xfade=transition=fade:duration={N(fade)}:offset={N(offset)}[{outputLabel}];");
            currentVideo = outputLabel;
            accumulatedDuration += media[i].Duration - fade;
        }

        var introDuration = media[0].Duration;
        var outroDuration = media[^1].Duration;
        var logoStart = Math.Max(0, introDuration - fade / 2);
        var logoEnd = Math.Max(logoStart, total - outroDuration + fade / 2);
        filter.Append($"[{logoIndex}:v]scale={profile.LogoWidth}:-1,format=rgba,colorchannelmixer=aa=1[logo];");
        filter.Append($"[{currentVideo}][logo]overlay={profile.LogoMarginLeft}:H-h-{profile.LogoMarginBottom}:");
        filter.Append($"enable='between(t\\,{N(logoStart)}\\,{N(logoEnd)})':shortest=1,format=yuv420p[vout];");

        for (var i = 0; i < media.Count; i++)
            AddAudio(filter, i, media[i], profile.OriginalVolumePercent, $"a{i}", N);
        var currentAudio = "a0";
        for (var i = 1; i < media.Count; i++)
        {
            var outputLabel = $"ax{i}";
            filter.Append($"[{currentAudio}][a{i}]acrossfade=d={N(fade)}:c1=tri:c2=tri[{outputLabel}];");
            currentAudio = outputLabel;
        }

        if (musicIndex >= 0)
        {
            filter.Append($"[{musicIndex}:a]atrim=duration={N(total)},asetpts=PTS-STARTPTS,volume={N(profile.MusicVolumePercent / 100)},");
            filter.Append($"afade=t=in:st=0:d={N(fade)},afade=t=out:st={N(Math.Max(0, total - fade))}:d={N(fade)}[music];");
            filter.Append($"[{currentAudio}][music]amix=inputs=2:duration=first:dropout_transition=2,alimiter=limit=0.95[aout]");
        }
        else filter.Append($"[{currentAudio}]alimiter=limit=0.95[aout]");

        args.AddRange(["-filter_complex", filter.ToString(), "-map", "[vout]", "-map", "[aout]",
            "-c:v", "libx264", "-preset", "slow", "-crf", profile.Crf.ToString(ci),
            "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-shortest", output]);
        return args;
    }

    private static void AddAudio(StringBuilder filter, int index, MediaInfo info, double volume,
        string label, Func<double, string> number)
    {
        var prefix = info.HasAudio ? $"[{index}:a]" : "anullsrc=channel_layout=stereo:sample_rate=48000,";
        filter.Append($"{prefix}atrim=duration={number(info.Duration)},asetpts=PTS-STARTPTS,");
        filter.Append($"aformat=sample_rates=48000:channel_layouts=stereo,volume={number(volume / 100)}[{label}];");
    }

    private static void AddInput(List<string> args, string path, MediaInfo info)
    {
        if (info.IsImage)
            args.AddRange(["-loop", "1", "-t", info.Duration.ToString(CultureInfo.InvariantCulture), "-i", path]);
        else args.AddRange(["-i", path]);
    }

    private static double ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 30;
        var parts = value.Split('/');
        if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) && denominator != 0)
            return numerator / denominator;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) ? fps : 30;
    }

    private sealed record MediaSource(string Path, double ImageSeconds, MediaRole Role);
    private sealed record MediaInfo(double Duration, bool HasAudio, bool IsImage, int Width, int Height, double FrameRate);
    private enum MediaRole { Intro, Content, Outro }
}
