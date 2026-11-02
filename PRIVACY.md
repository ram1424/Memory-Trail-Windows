# Memory Trail Privacy & Data Handling

**Applies to:** Memory Trail for Windows 1.0 (Build 1)

Memory Trail is designed as a **privacy-first, local-first application**. Its purpose is to help you retrace your recent computer activity while keeping captured history on the Windows PC where the application runs.

This document describes what the Windows application captures, where it stores data, what it does not intentionally collect, and the controls available to you.

## Core privacy model

Memory Trail's timeline workflow is designed around four principles:

1. **Local capture** — activity is read from Windows APIs on the local computer.
2. **Local processing** — timeline construction and Ask Memory Trail searches run in the application.
3. **Local storage** — captured history and preferences are stored under the current Windows user's Local AppData folder.
4. **User control** — app activity, clipboard capture, exclusions, pause/resume, history deletion, and startup behavior are user-controlled.

## Local storage locations

Timeline data:

```text
%LOCALAPPDATA%\Memory Trail\timeline.json
```

Preferences and exclusions:

```text
%LOCALAPPDATA%\Memory Trail\preferences.json
```

Crash/non-fatal diagnostic logs may also be written under:

```text
%LOCALAPPDATA%\Memory Trail
```

These files are local files owned by the signed-in Windows user.

## Data Memory Trail may capture

When **App Activity** capture is enabled, Memory Trail may store information Windows exposes for the foreground application, including:

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
- timestamp and locally generated category metadata.

When **Clipboard** capture is enabled, Memory Trail may store copied **text** and related local metadata.

## Data Memory Trail does not intentionally capture

Memory Trail is not designed to:

- record the screen or take screenshots;
- record audio or video;
- capture typed keystrokes;
- act as a keylogger;
- upload captured timeline history to a Memory Trail cloud service;
- synchronize captured timeline history between computers;
- send captured timeline history to analytics or advertising providers;
- require a Memory Trail account;
- require an external connector for timeline capture, storage, display, or Ask Memory Trail.

Opening the project's GitHub page from **Settings → About Memory Trail** launches your default browser to GitHub. That user-initiated browser navigation is separate from the local capture/storage workflow.

## Default exclusions

The Windows application starts with these process exclusions:

- `CredentialUIBroker`
- `1Password`
- `Bitwarden`

You can add or remove process names in **Settings → Privacy exclusions**.

Excluded processes are not intentionally recorded by the activity collector.

## User controls

### Pause all capture

**Settings → Pause all capture** stops app-activity and clipboard collection.

### App Activity

**Settings → Remember app switches and focused windows** controls foreground application/window logging.

### Clipboard

**Settings → Remember copied text** controls clipboard-text history.

### Privacy exclusions

You can enter one Windows process name per line. Memory Trail checks the foreground process against this exclusion list before writing an activity item.

### Clear history

**Settings → Clear history** permanently clears the local timeline collection.

### Reset preferences

**Reset preferences** restores capture settings and default exclusions. It does not delete timeline history unless **Clear history** is used separately.

### Start with Windows

Startup is optional and can be enabled or disabled in Settings.

## Ask Memory Trail

Ask Memory Trail searches the locally stored timeline. The current Windows implementation performs its search against local Memory Trail data and does not require an external connector or cloud account.

## Capture reliability and fallback behavior

Memory Trail uses:

- the Windows foreground-window event system;
- periodic polling as a fallback;
- Windows UI Automation when useful for exposed window names;
- a self-healing collector health check.

If Windows does not expose a meaningful title, Memory Trail may store an application-level fallback event rather than silently dropping the activity.

## Source-code verification

Memory Trail is open source. You can review the Windows implementation here:

https://github.com/ram1424/Memory-Lane-Windows

Relevant privacy-sensitive code is primarily located in:

- `Services/CaptureService.cs`
- `Services/TimelineStore.cs`
- `Services/PreferencesService.cs`
- `MainWindow.xaml.cs`

## Important Windows limitation

Memory Trail can only work with information exposed by Windows and the active application. Different applications expose different amounts of context. This means capture quality can vary by app and Windows configuration.

## Changes to this document

Privacy behavior should be reviewed alongside source-code changes. The repository history provides a public record of modifications to the Windows implementation.
