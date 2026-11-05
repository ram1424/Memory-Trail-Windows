using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
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
    private string StoreStartupScriptPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "Memory Trail.vbs");

    public bool CapturePaused { get; set; }
    public bool CaptureAppActivity { get; set; } = true;
    public bool CaptureClipboard { get; set; } = true;
    public bool HasCompletedOnboarding { get; set; }
    public string LastCompletedTutorialVersion { get; set; } = "";
    public bool StartWithWindows { get; set; }
    public bool AutomaticUpdateChecks { get; set; } = true;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
    public DateTimeOffset? UpdateReminderUntilUtc { get; set; }
    public string SkippedUpdateVersion { get; set; } = "";
    public string PendingUpdateVersion { get; set; } = "";
    public string PendingUpdateReleaseCommit { get; set; } = "";
    public string PendingUpdateDirectory { get; set; } = "";
    public string LastSeenVersion { get; set; } = "";

    public HashSet<string> ExcludedProcesses { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "CredentialUIBroker",
        "1Password",
        "Bitwarden"
    };

    public bool IsExcluded(string processName) =>
        string.IsNullOrWhiteSpace(processName) || ExcludedProcesses.Contains(processName);

    public bool SetStartWithWindows(bool enabled)
    {
        try
        {
            if (AppVersion.IsMicrosoftStoreBuild)
                SetStoreStartup(enabled);
            else
                SetRegistryStartup(enabled);

            StartWithWindows = enabled;
            return true;
        }
        catch (Exception ex)
        {
            StartWithWindows = false;
            App.LogNonFatal("Start with Windows", ex);
            return false;
        }
    }

    private void SetRegistryStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key is null)
            throw new InvalidOperationException("Windows startup registry key is unavailable.");

        key.DeleteValue("Memory Lane", false);
        if (enabled)
            key.SetValue("Memory Trail", "\"" + Environment.ProcessPath + "\"");
        else
            key.DeleteValue("Memory Trail", false);
    }

    private void SetStoreStartup(bool enabled)
    {
        var familyName = TryGetCurrentPackageFamilyName();
        if (string.IsNullOrWhiteSpace(familyName))
            throw new InvalidOperationException("The Microsoft Store package identity could not be resolved.");

        var appId = familyName + "!MemoryTrail";
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true)
            ?? throw new InvalidOperationException("Windows startup registry key is unavailable.");

        // Remove every legacy startup path first so the setting has one source of truth.
        key.DeleteValue("Memory Lane", false);
        key.DeleteValue("Memory Trail", false);
        if (File.Exists(StoreStartupScriptPath))
            File.Delete(StoreStartupScriptPath);

        if (!enabled)
            return;

        // Packaged desktop apps are launched through AppsFolder instead of their
        // versioned install path, which keeps startup valid after Store updates.
        key.SetValue(
            "Memory Trail",
            "explorer.exe \"shell:AppsFolder\\" + appId + "\"");
    }

    private static string? TryGetCurrentPackageFamilyName()
    {
        uint length = 0;
        var result = GetCurrentPackageFamilyName(ref length, null);
        if (result != 122 || length == 0)
            return null;

        var buffer = new StringBuilder((int)length);
        result = GetCurrentPackageFamilyName(ref length, buffer);
        return result == 0 ? buffer.ToString() : null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(
        ref uint packageFamilyNameLength,
        StringBuilder? packageFamilyName);

    public void ResetToDefaults()
    {
        CapturePaused = false;
        CaptureAppActivity = true;
        CaptureClipboard = true;
        AutomaticUpdateChecks = true;
        UpdateReminderUntilUtc = null;
        SkippedUpdateVersion = "";
        PendingUpdateVersion = "";
        PendingUpdateReleaseCommit = "";
        PendingUpdateDirectory = "";
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
            LastCompletedTutorialVersion = loaded.LastCompletedTutorialVersion ?? "";
            StartWithWindows = loaded.StartWithWindows;
            AutomaticUpdateChecks = loaded.AutomaticUpdateChecks;
            LastUpdateCheckUtc = loaded.LastUpdateCheckUtc;
            UpdateReminderUntilUtc = loaded.UpdateReminderUntilUtc;
            SkippedUpdateVersion = loaded.SkippedUpdateVersion ?? "";
            PendingUpdateVersion = loaded.PendingUpdateVersion ?? "";
            PendingUpdateReleaseCommit = loaded.PendingUpdateReleaseCommit ?? "";
            PendingUpdateDirectory = loaded.PendingUpdateDirectory ?? "";
            LastSeenVersion = loaded.LastSeenVersion ?? "";
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
