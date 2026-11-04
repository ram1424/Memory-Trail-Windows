# Memory Trail Privacy & Data Handling

**Applies to:** Memory Trail for Windows Version 1

Memory Trail is designed as a **privacy-first, local-first application**. Its purpose is to help you retrace recent computer activity while keeping the captured timeline on the Windows PC where the application runs.

This document explains what Memory Trail may capture, how that information is processed, where it is stored, what does and does not leave the app's local workflow, and the controls available to you.

## Core privacy model

Memory Trail's timeline workflow is built around five principles:

1. **Local capture** — activity is read from Windows APIs on the local computer.
2. **Local processing** — titles, categories, duplicate detection, summaries, active-time calculations, and Ask Memory Trail searches are processed inside the application.
3. **Local storage** — captured history and preferences are written to files under the current Windows user's Local AppData folder.
4. **No captured-history cloud dependency** — capture, timeline display, Insights, and Ask Memory Trail do not require a Memory Trail account, external connector, or cloud AI service.
5. **User control** — app activity, clipboard capture, exclusions, pause/resume, history deletion, and startup behavior are controlled from Settings.

## How data moves through Memory Trail

### 1. Capture

When **App Activity** is enabled, Memory Trail reads locally available foreground-window information through Windows foreground-window APIs and Windows UI Automation.

When **Clipboard** capture is enabled, Memory Trail reads copied text from the Windows clipboard.

### 2. Process

Memory Trail processes captured information inside the application. Processing includes:

- normalizing window titles;
- mapping process names to friendly application names;
- choosing an activity category;
- suppressing duplicate consecutive app/window entries;
- preparing timeline details;
- calculating local summaries and active-time information;
- searching the local timeline for Ask Memory Trail.

### 3. Store

Timeline history is serialized to a local JSON file. Preferences and exclusions are serialized to a separate local JSON file.

No database server or remote storage service is required.

### 4. Search and display

The timeline, Favorites, Clipboard History, Today Summary, Insights, and Ask Memory Trail operate against the locally loaded timeline data.

### 5. Delete or change capture

You can pause capture, disable a capture type, add exclusions, or clear history from Settings.

## Local storage locations

Timeline data:

```text
%LOCALAPPDATA%\Memory Trail\timeline.json
```

Preferences and exclusions:

```text
%LOCALAPPDATA%\Memory Trail\preferences.json
```

Crash and non-fatal diagnostic information may be appended to:

```text
%LOCALAPPDATA%\Memory Trail\crash.log
```

These are normal files inside the signed-in Windows user's profile.

### Encryption note

Memory Trail does **not currently add its own application-level encryption layer** to `timeline.json`, `preferences.json`, or `crash.log`. Access therefore depends on Windows user permissions, device security, and any disk encryption configured on the PC.

## Data Memory Trail may capture

When **App Activity** capture is enabled, Memory Trail may store information Windows exposes for the active application, including:

- process name;
- friendly application name;
- executable path;
- foreground window title;
- browser tab/page title when exposed by Windows;
- Outlook email subject when available;
- Teams/chat/conversation title when available;
- File Explorer folder or file context;
- Word, Excel, PowerPoint, OneNote, Notepad, PDF, and other document titles;
- app-level fallback activity such as **Opened Microsoft Edge** when a meaningful title is unavailable;
- timestamp and locally generated category/activity metadata.

### Clipboard capture

Clipboard capture is **enabled by default** in the current Windows build.

When enabled, Memory Trail may store:

- copied text;
- a shortened preview for timeline display;
- the foreground process/application associated with the clipboard event when available;
- the executable path when available;
- an HTTP/HTTPS URL if the copied text itself is recognized as a URL.

Clipboard text longer than 20,000 characters is ignored by the current collector.

If you do not want copied text retained in Memory Trail, disable **Settings → Capture → Remember copied text**.

## Privacy exclusions

The default exclusion list contains:

- `CredentialUIBroker`
- `1Password`
- `Bitwarden`

You can add or remove process names in **Settings → Privacy exclusions**.

Before Memory Trail writes a foreground-app activity item, it checks the active process against the exclusion list. Clipboard capture also checks the foreground process against the exclusion list before writing the clipboard item when a process name is available.

Excluded processes are therefore not intentionally written to the timeline by these collectors.

## What Memory Trail does not intentionally capture

Memory Trail is not designed to:

- record the screen or take screenshots;
- record audio or video;
- capture typed keystrokes;
- act as a keylogger;
- upload captured timeline history to a Memory Trail cloud service;
- synchronize captured timeline history between computers;
- send captured timeline history to analytics or advertising providers;
- require a Memory Trail account;
- require an external connector for capture, storage, display, Insights, or Ask Memory Trail.

## Network boundary

The core capture/storage/search workflow is local.

Memory Trail does not require network access to:

- capture foreground activity;
- capture clipboard text;
- write or read timeline history;
- display the timeline;
- calculate Insights;
- search with Ask Memory Trail.

The app includes a **View Memory Trail on GitHub** action in Settings. Selecting it launches your default browser to GitHub. That is a deliberate, user-initiated navigation outside the app and is separate from the captured-history workflow.

### Update checks

Memory Trail Version 1 includes manual and optional automatic update checks. When enabled, the app requests release metadata from GitHub. Update checks are separate from the capture/storage/search pipeline and do **not** attach timeline entries, clipboard text, window titles, file paths, or other captured activity to the request.

Automatic update checks are enabled by default and run once on each Memory Trail launch. They can be disabled under **Settings → Updates**.

The current Memory Trail GitHub repository is private. Anonymous GitHub API requests cannot read private release metadata. If the environment variable `MEMORY_TRAIL_GITHUB_TOKEN` contains an authorized GitHub token, Memory Trail can use it only for the release-metadata request. Memory Trail does not write that token into its preferences file.

If the release feed is unavailable, the updater fails safely and the local capture workflow continues normally.

## Diagnostic logs

If startup or a non-fatal component error occurs, Memory Trail may write exception details to `crash.log` under Local AppData. This file is local and is not automatically uploaded by Memory Trail.

Because exception text can sometimes include file paths or system details, users who share a crash log should review it before sending it to another person.

## Retention

Memory Trail does not currently apply an automatic cloud retention policy because captured history is not stored in a Memory Trail cloud service.

Local timeline entries remain in `timeline.json` until they are removed through the app's history controls, overwritten through normal local file operations, or the local file is deleted.

## User controls

### Pause all capture

**Settings → Pause all capture** stops app-activity and clipboard collection.

### App Activity

**Settings → Remember app switches and focused windows** controls foreground application/window logging.

### Clipboard

**Settings → Remember copied text** controls clipboard-text history.

### Privacy exclusions

Enter one Windows process name per line. Memory Trail checks the foreground process against this exclusion list before writing supported activity.

### Clear history

**Settings → Clear history** clears the local timeline collection.

### Clear App Cache

**Clear App Cache** resets cached capture state and Memory Trail preferences to their defaults. It does not delete timeline history; use **Clear history** separately when you want to remove activity history.

### Start with Windows

Startup is optional and can be enabled or disabled in Settings.

### Update decisions and staged packages

When an update is available, Memory Trail lets the user **Update**, **Remind Me in 24 Hours**, or **Skip This Version**. These choices are stored only in the local preferences file.

Choosing **Update** downloads the public GitHub release ZIP to `%LOCALAPPDATA%\Memory Trail\Updates`, verifies its SHA-256 digest when GitHub provides one, and stages the extracted files locally. Memory Trail remains usable during the download. The staged files are copied into the existing installation folder only after the running Memory Trail process exits.

### Automatic update checks

**Settings → Updates → Check for updates on each launch** controls the automatic GitHub release check performed when Memory Trail starts. Manual checks are available from Settings and the Memory Trail system-tray menu.

## Source-code verification

Memory Trail is open source. The Windows implementation is available at:

https://github.com/ram1424/Memory-Trail-Windows

Privacy-sensitive implementation is primarily located in:

- `Services/CaptureService.cs`
- `Services/TimelineStore.cs`
- `Services/PreferencesService.cs`
- `App.xaml.cs`
- `Views/MainWindow.xaml`
- `Views/MainWindow.xaml.cs`

The repository history can be used to review changes to capture, storage, privacy UI, and documentation over time.

## Important Windows limitation

Memory Trail can only work with information exposed by Windows and the active application. Different applications expose different amounts of context, so capture quality varies by app and Windows configuration.

## Changes to this document

Privacy behavior should be reviewed alongside source-code changes. This document should be updated whenever a change materially affects what Memory Trail captures, processes, stores, transmits, or exposes through user controls.
