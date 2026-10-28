# Memory Trail for Windows

Memory Trail is a private, local-first activity timeline for Windows. It is the Windows-native companion to the macOS Memory Trail project.

## What it does

- Builds a chronological timeline of foreground application and window activity.
- Captures meaningful window titles from apps such as Microsoft Edge, Chrome, Outlook, Teams, File Explorer, and other desktop applications.
- Captures copied text when clipboard capture is enabled.
- Searches across captured memories.
- Lets you favorite important memories.
- Lets you create manual memories with notes, links, files, or screenshot references.
- Provides pause/resume capture controls.
- Runs from the Windows system tray when the main window is closed.
- Supports per-process privacy exclusions.
- Can optionally start when you sign in to Windows.
- Includes a first-run welcome tutorial that can be replayed from Settings.

## Privacy

Memory Trail is designed to remain local-first.

- Timeline data is stored locally under `%LOCALAPPDATA%\Memory Trail`.
- Preferences are stored locally.
- No account is required.
- No analytics SDK is included.
- No advertising SDK is included.
- Capture can be paused at any time.
- Clipboard capture can be disabled.
- Process exclusions let you control which apps Memory Trail intentionally ignores.
- The app does not implement keylogging, screen recording, or hidden browser-history scraping.

## Windows implementation

The macOS application uses SwiftUI, SwiftData, AppKit, and macOS Accessibility APIs. This Windows edition recreates the same product behavior using:

- .NET 8
- WPF
- Win32 foreground-window APIs
- Windows clipboard APIs
- Windows system tray integration
- Local JSON persistence

## Build locally

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

Run:

```powershell
dotnet run --project .\MemoryLane.Windows.csproj
```

Build:

```powershell
dotnet build .\MemoryLane.Windows.csproj -c Release
```

Publish a self-contained x64 build:

```powershell
dotnet publish .\MemoryLane.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

## Automated Windows build

Every push to `main` runs the GitHub Actions workflow in `.github/workflows/build-windows.yml`. A successful run publishes a downloadable `Memory-Trail-Windows-x64.zip` workflow artifact.

## Related project

macOS version: `ram1424/Memory-Trail`
