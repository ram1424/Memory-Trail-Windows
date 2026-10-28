using Microsoft.Win32;
using System.Text.Json;

namespace MemoryLane.Windows;

public sealed class PreferencesService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Trail");
    private readonly string _legacyFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Lane");
    private string FilePath => Path.Combine(_folder, "preferences.json");

    public bool CapturePaused { get; set; }
    public bool CaptureAppActivity { get; set; } = true;
    public bool CaptureClipboard { get; set; } = true;
    public bool HasCompletedOnboarding { get; set; }
    public bool StartWithWindows { get; set; }

    public HashSet<string> ExcludedProcesses { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "CredentialUIBroker",
        "1Password",
        "Bitwarden"
    };

    public bool IsExcluded(string processName) =>
        string.IsNullOrWhiteSpace(processName) || ExcludedProcesses.Contains(processName);

    public void SetStartWithWindows(bool enabled)
    {
        StartWithWindows = enabled;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key is null) return;

            key.DeleteValue("Memory Lane", false);
            if (enabled)
                key.SetValue("Memory Trail", $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue("Memory Trail", false);
        }
        catch { }
    }

    public void ResetToDefaults()
    {
        CapturePaused = false;
        CaptureAppActivity = true;
        CaptureClipboard = true;
        StartWithWindows = false;
        SetStartWithWindows(false);
        ExcludedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CredentialUIBroker",
            "1Password",
            "Bitwarden"
        };
        Save();
    }

    public void Load()
    {
        try
        {
            Directory.CreateDirectory(_folder);
            MigrateLegacyFile();
            if (!File.Exists(FilePath)) return;
            var loaded = JsonSerializer.Deserialize<PreferencesService>(File.ReadAllText(FilePath));
            if (loaded is null) return;

            CapturePaused = loaded.CapturePaused;
            CaptureAppActivity = loaded.CaptureAppActivity;
            CaptureClipboard = loaded.CaptureClipboard;
            HasCompletedOnboarding = loaded.HasCompletedOnboarding;
            StartWithWindows = loaded.StartWithWindows;
            ExcludedProcesses = new HashSet<string>(
                loaded.ExcludedProcesses ?? [],
                StringComparer.OrdinalIgnoreCase);
        }
        catch { }
    }

    private void MigrateLegacyFile()
    {
        try
        {
            var legacyFile = Path.Combine(_legacyFolder, "preferences.json");
            if (!File.Exists(FilePath) && File.Exists(legacyFile))
                File.Copy(legacyFile, FilePath, false);
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
