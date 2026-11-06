# Memory Trail for Windows

<p align="center">
  <img src="Assets/MemoryTrailIconMaster.png" width="168" alt="Memory Trail app icon">
</p>

<p align="center"><strong>Your workday, remembered locally.</strong></p>

Memory Trail is a privacy-first Windows activity timeline that helps you retrace what you were doing across apps, browsers, email, chat, files, documents, and the clipboard. It turns useful local Windows context into a searchable chronological history without requiring a Memory Trail account, cloud timeline sync, or external data connector.

**Current release: Memory Trail Version 1.4**

## Download

[Download the latest Memory Trail release](https://github.com/ram1424/Memory-Trail-Windows/releases/latest)

- Windows 10 or Windows 11
- x64
- Self-contained build; no separate .NET installation required
- Microsoft Store MSIX is also published with the release

## What Memory Trail Does

### Today
- Captures foreground app and window activity into a chronological timeline
- Shows the newest activity first with a live-capture indicator
- Surfaces app/window context such as browser pages, Outlook mail context, conversations, files, documents, and supported productivity activity
- Includes **Today at a Glance**, **Top Apps Today**, **Key Takeaways**, and recent files/documents
- Shows real app icons throughout the timeline and dashboard
- Supports long histories with virtualized scrolling and **Back to Top**

### Ask Memory Trail
- Searches your locally stored history using natural phrases
- Finds activity by time, app, filename, subject, page, email, conversation, or nearby context
- Includes shortcut questions for common searches
- Shows consistent result rows with the originating app icon
- Returns the full matching local history without an artificial result cap
- Search is local; captured history is not sent to a cloud search service

### Favorites
- Save important timeline entries
- Return to important memories without searching the entire history

### Clipboard History
- Optionally capture copied text and links
- Search and review clipboard activity alongside the rest of your trail
- Clipboard capture can be disabled independently

### Insights
- See activity by app
- Review active time and activity patterns
- Identify peak activity periods and focus streaks
- Review category distribution and key takeaways
- Calculations are generated from the local Memory Trail timeline

### Settings & Controls
- Pause/resume capture
- Enable or disable clipboard capture
- Exclude selected apps/processes
- Open the local data folder
- Export activity to JSON
- Clear the last week, last month, or all history
- Run from the system tray
- Optionally start Memory Trail with Windows
- Check for updates
- View privacy information and source-code references

## Privacy First

Memory Trail is designed around a simple principle: **your captured activity history stays on your Windows PC.**

Captured activity is sourced from local Windows APIs and UI Automation, processed locally, and stored under the signed-in Windows user profile.

Memory Trail does **not** require:
- a Memory Trail account
- cloud timeline sync
- analytics SDKs
- advertising SDKs
- external data connectors
- remote AI processing of your captured timeline

Local data includes timeline history, preferences, exclusions, and optional clipboard history. See [docs/PRIVACY.md](docs/PRIVACY.md) for the complete privacy and data-handling description.

## Screenshots

### Today
![Memory Trail Today view](docs/images/screenshots/today-ui.png)

### Ask Memory Trail
![Ask Memory Trail](docs/images/screenshots/ask-memory-trail-ui.png)

### Settings & Privacy
![Memory Trail privacy settings](docs/images/screenshots/privacy-settings-ui.png)

### First-run Tutorial
![Memory Trail onboarding](docs/images/screenshots/onboarding-privacy-ui.png)

_All screenshots use generic demo data._

## Version 1.4

Version 1.4 is the official public baseline for Memory Trail for Windows. It includes the complete local activity timeline, Ask Memory Trail, Favorites, Clipboard History, Insights, privacy controls, local storage/export, system-tray operation, Windows startup support, and both direct-download and Microsoft Store distribution.

See [docs/CHANGELOG.md](docs/CHANGELOG.md) for the full Version 1.4 feature changelog.

## Distribution

The GitHub release provides:
- `Memory-Trail-Windows-1.4-x64.zip` — direct-download build
- `Memory-Trail-1.4-x64.msix` — Microsoft Store submission package

The Microsoft Store build relies on Microsoft Store for update delivery. The direct-download build uses Memory Trail's GitHub update flow.

## Source

The source code in this repository is available for review so users and administrators can inspect how capture, storage, privacy controls, updates, and packaging are implemented.
