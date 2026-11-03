# Memory Trail 1.0 (Build 2)

## What's new

- **Two-second startup experience** — the loading screen now targets two seconds and then hands off as soon as the app is ready.
- **Check for Updates** — available from the Memory Trail system-tray menu and from Settings.
- **Automatic daily update checks** — enabled by default, with a user-controlled toggle in Settings.
- **Update available prompt** — shows the current version, available version, release notes, and a direct **Download Update** action.
- **What's New window** — returning users see a polished changelog once after launching an upgraded build.
- **Version-aware UI** — About and window branding now use the running build's version information.
- **Privacy disclosure for update checks** — update requests retrieve GitHub release metadata only and do not include captured timeline, clipboard, window-title, or file-path data.

## Privacy

Memory Trail's capture, timeline, Insights, and Ask Memory Trail workflows remain local. Automatic update checks are a separate network request to GitHub for release metadata and can be disabled in Settings.

> **Private repository note:** the current GitHub repository is private. Anonymous in-app release checks cannot read private releases. The updater supports a `MEMORY_TRAIL_GITHUB_TOKEN` environment variable for authorized private-repository checks, and will otherwise fail safely without sending captured activity data.
