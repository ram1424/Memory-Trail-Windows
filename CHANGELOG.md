# Changelog

## 1.0 (Build 2)

- Reduced the startup/loading screen target to **2 seconds**.
- Added **Check for Updates** to the Memory Trail system-tray menu and Settings.
- Added automatic update checks once every 24 hours when enabled.
- Added an update-available window with release notes and a **Download Update** action.
- Added a polished **What's New** changelog window shown once after an upgrade for returning users.
- Added version-aware About/window branding.
- Added explicit privacy disclosure that update checks contact GitHub only for release metadata and do not send captured timeline, clipboard, window-title, or file-path data.
- Added safe handling for private GitHub release feeds; authorized private-feed checks can use `MEMORY_TRAIL_GITHUB_TOKEN`.

## 1.0 (Build 1)

- Promoted the Windows edition to **Memory Trail 1.0 (Build 1)**.
- Overhauled the Windows UI with the Soft Blue design system, dimensional filter icons, polished summary/insights cards, and macOS-aligned layout proportions.
- Rebuilt the first-run tutorial with privacy-first messaging, refined iconography, darker blue-gray background, and persistent blue Continue button states.
- Added a polished startup/loading screen with local/private status messaging.
- Reworked the application icon pipeline around the approved Memory Trail artwork.
- Added the custom Hide/Show Panels control and refined filter count alignment.
- Hardened Windows activity logging with a foreground-window event hook, polling fallback, UI Automation title fallback, application-level fallback events, and automatic collector recovery.
- Corrected capture status messaging so the app no longer reports Capturing when App Activity is disabled.
- Added session-based active-time calculation and dynamic timeline/category filtering.
- Added Ask Memory Trail, Favorites, clipboard history, manual memories, details panel, Insights, and local privacy controls.
- Expanded privacy documentation and added an in-app About Memory Trail link to the GitHub repository.
- Updated release automation so older releases/tags are removed and only the current Build 1 release remains published.

## Previous development builds

Pre-1.0 development builds are superseded by **1.0 (Build 1)** and are removed from the public Releases page by the release workflow.
