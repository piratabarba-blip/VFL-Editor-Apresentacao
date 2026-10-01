using System.Text.Json;

namespace VFL.VideoApresentacao;

public sealed class ProfileStore
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VFL Editor de Apresentacao");

    private string FilePath => Path.Combine(_folder, "clientes.json");

    public List<ClientProfile> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return [];
            var profiles = JsonSerializer.Deserialize<List<ClientProfile>>(File.ReadAllText(FilePath)) ?? [];
            // Perfis das primeiras versões usavam CRF 20. A versão atual prioriza
            // preservação visual e migra esses perfis automaticamente para CRF 18.
            foreach (var profile in profiles)
                if (profile.Crf >= 20) profile.Crf = 18;
            return profiles;
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<ClientProfile> profiles)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
    }
}
