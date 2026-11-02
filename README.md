# Memory Trail for Windows

**Memory Trail** is a private, local-first Windows activity timeline that helps you retrace the apps, pages, emails, conversations, files, clipboard text, and work context you recently used.

This repository is the Windows-native companion to the macOS Memory Trail project. It is implemented with .NET 8, WPF, Win32 foreground-window APIs, Windows UI Automation, Windows clipboard APIs, and local JSON persistence.

## Windows 1.0

The Windows 1.0 release brings the current Memory Trail product model to Windows:

- a newest-first **Today** timeline;
- a green **Live Activity** state on the latest captured item;
- session-based active-time calculation that does not add a fake minute per click;
- dynamic activity filters that appear only when relevant activity exists;
- **Ask Memory Trail** with website, email, conversation, file, recent-activity, week, and time-aware searches;
- clickable Ask results with a complete activity-details view;
- foreground application and meaningful window-title capture;
- clipboard history that can be disabled at any time;
- Favorites and manual memories;
- show/hide controls for Today Summary and activity details;
- installed-application privacy selection and manual process exclusions;
- pause/resume capture and optional startup with Windows;
- Windows system-tray operation;
- first-run privacy tutorial and replayable onboarding;
- self-contained x64 releases that do not require a separate .NET installation.

## Privacy first

Memory Trail is designed to run locally on the computer where it is installed.

- Activity history is stored under `%LOCALAPPDATA%\Memory Trail`.
- Preferences and exclusions are stored locally.
- No Memory Trail account is required.
- No analytics or advertising SDK is included.
- No captured timeline data is uploaded or synchronized.
- No external data connector is used by the capture and storage workflow.
- Capture can be paused at any time.
- Clipboard capture can be disabled.
- Installed apps and process names can be excluded.
- Credential prompts, 1Password, and Bitwarden are protected by default.
- Memory Trail is not a keylogger and does not record the screen.

## Captured context

Memory Trail records the foreground process and the meaningful title Windows exposes for the active window. Depending on the application, this can include:

- browser page or tab titles;
- Outlook email subjects;
- Teams or other chat conversation titles;
- File Explorer folders;
- Word, Excel, PowerPoint, OneNote, Notepad, and PDF document names;
- other desktop application window titles;
- copied text when clipboard capture is enabled.

Application behavior and accessibility support vary, so some apps expose more useful context than others.

## Download

Download the latest self-contained Windows x64 ZIP from the repository’s **Releases** page. Extract the ZIP and run `Memory Trail.exe`.

## Build from source

Requirements:

- Windows 10 or Windows 11;
- .NET 8 SDK.

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

## Storage

Memory Trail stores its files locally at:

```text
%LOCALAPPDATA%\Memory Trail
```

The timeline and preferences remain readable local files. Clearing app preferences does not delete timeline history unless **Clear history** is selected explicitly.

## Related project

macOS version: `ram1424/Memory-Lane`
