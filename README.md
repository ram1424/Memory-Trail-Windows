# Memory Trail for Windows

**Memory Trail 1.0 (Build 1)** is a private, local-first Windows activity timeline that helps you retrace what you were working on without sending your captured history to a cloud service.

> **Private by design · Local storage · Open source · Windows 10/11 · Self-contained x64 build**

[Download the latest release](https://github.com/ram1424/Memory-Lane-Windows/releases/latest) · [Privacy & data handling](PRIVACY.md) · [macOS edition](https://github.com/ram1424/Memory-Lane)

## What Memory Trail does

Memory Trail builds a newest-first history of your workday from information Windows exposes locally. The current Windows release includes:

- **Today timeline** with newest activity first and a green **Live Activity** marker on the latest item.
- **Foreground-app capture** using the Windows foreground-event system with polling fallback and automatic collector recovery.
- **Window context capture** for meaningful titles Windows exposes, including browser tabs/pages, Outlook subjects, Teams/chat titles, File Explorer folders, Office documents, PDFs, and other desktop windows.
- **Application fallback logging** so an app switch can still be recorded when Windows does not expose a useful window title.
- **Clipboard history** for copied text when clipboard capture is enabled.
- **Ask Memory Trail**, a local search experience for recent apps, websites, email, conversations, files, time ranges, and nearby activity.
- **Favorites**, manual memories, category filters, Today Summary, Insights, and activity details.
- **Session-based active time** using inactivity boundaries instead of counting every event as a separate minute.
- **Show/Hide Panels** for a cleaner workspace.
- **Windows system tray** operation and optional **Start with Windows**.
- A polished **Soft Blue** Windows UI, loading screen, and privacy-first first-run tutorial.
- Self-contained Windows x64 packaging with no separate .NET installation required.

## Privacy first

Memory Trail is designed around local capture and local storage.

### What stays on your PC

Captured timeline data is stored in:

```text
%LOCALAPPDATA%\Memory Trail\timeline.json
```

Preferences, exclusions, onboarding state, and capture settings are stored in:

```text
%LOCALAPPDATA%\Memory Trail\preferences.json
```

### What Memory Trail does not do

- No Memory Trail account is required.
- No captured timeline history is uploaded or synchronized by the app.
- No analytics or advertising SDK is included.
- No external connector is required for capture, storage, timeline display, or Ask Memory Trail.
- Memory Trail does **not** record your screen.
- Memory Trail is **not** a keylogger and does not capture typed keystrokes.
- Memory Trail does not intentionally capture excluded processes.

### Privacy controls

You can:

- Pause all capture at any time.
- Turn **App Activity** capture on or off.
- Turn **Clipboard** capture on or off.
- Exclude Windows processes from capture.
- Clear the timeline explicitly.
- Reset preferences without deleting timeline history.
- Review the source code in this repository.

The default exclusion list includes:

- `CredentialUIBroker`
- `1Password`
- `Bitwarden`

See [PRIVACY.md](PRIVACY.md) for the full privacy and data-handling description.

## What may be captured

Depending on the application and what Windows exposes, a timeline item may contain:

- application/process name;
- foreground window title;
- browser page or tab title;
- Outlook email subject;
- Teams or other supported conversation title;
- File Explorer folder or filename;
- Word, Excel, PowerPoint, OneNote, Notepad, PDF, or other document title;
- executable path used for reopening an application;
- copied text when clipboard capture is enabled;
- timestamp, category, favorite state, and locally generated activity metadata.

Some applications expose less context than others. When a useful title is unavailable, Memory Trail can record an app-level event such as **Opened Microsoft Edge** rather than dropping the event.

## Capture reliability

Build 1 includes a hardened Windows capture pipeline:

- Windows foreground-change event hook;
- one-second polling fallback;
- immediate capture when the collector starts;
- Windows UI Automation title fallback;
- automatic capture-service health checks and restart;
- accurate UI status for **Capturing**, **Capture paused**, **App capture off**, and **Starting capture**.

## Download

The Releases page is intentionally kept clean: **only the current release should remain published**.

Download **Memory Trail 1.0 (Build 1)** from the [latest release](https://github.com/ram1424/Memory-Lane-Windows/releases/latest), extract the ZIP, and run:

```text
Memory Trail.exe
```

## Build from source

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

```powershell
dotnet restore .\MemoryTrail.Windows.csproj
dotnet build .\MemoryTrail.Windows.csproj -c Release
dotnet run --project .\MemoryTrail.Windows.csproj
```

Create a self-contained x64 build:

```powershell
dotnet publish .\MemoryTrail.Windows.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o .\publish
```

## Technology

Memory Trail for Windows is built with:

- .NET 8
- WPF
- Win32 foreground-window APIs
- Windows UI Automation
- Windows clipboard APIs
- local JSON persistence
- GitHub Actions for self-contained Windows release builds

## Version

**Memory Trail 1.0 (Build 1)**

The in-app **Settings → About Memory Trail** section links back to this GitHub repository for source review, privacy verification, and release information.
