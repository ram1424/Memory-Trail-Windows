# Changelog

## Version 1.0.1

- Established semantic patch versioning for production releases.
- Every future push to `main` automatically advances the patch version: `1.0.2`, `1.0.3`, and so on.
- Restored the complete six-page first-run tutorial for users coming from development builds.
- Kept tutorial completion independent from patch updates so the tutorial does not replay on every release.
- Improved activity-count readability with white count badges, quick-filter pills, and summary cards.
- Retained the organized repository layout and Version 1 update/check-for-updates experience.
- Checks for updates on each app launch when automatic update checks are enabled, replacing the previous 24-hour throttle.
- Selected Today-view filter counts now switch to white for stronger contrast on the blue active tab.
- Reworked launch-time updates with an **Update / Remind Me in 24 Hours / Skip This Version** prompt.
- Added background ZIP download, GitHub SHA-256 verification, local staging, and silent install after Memory Trail exits.
- Added internal build-commit identity so a refreshed 1.0.1 release can be detected even when the visible semantic version is unchanged.
- Added a polished “You’re All Caught Up!” screen when a manual update check finds no newer release.
- Reduced Today-filter work by avoiding unnecessary count/summary recomputation and full live-marker scans.
- Replaced tutorial Outlook executable-icon lookup with a deterministic Outlook-branded fallback so the icon renders reliably.


## Version 1.0.0 — First Official Release

Memory Trail Version 1.0.0 was the first official Windows release.

- Privacy-first, local activity capture and local timeline storage.
- Today timeline with newest-first activity and Live Activity state.
- Ask Memory Trail for searching locally stored websites, conversations, documents, files, apps, and time-aware activity.
- Browser, Outlook, Teams/chat, file/document, application, and clipboard context when exposed by Windows.
- Favorites, Clipboard History, manual memories, Insights, filters, and activity details.
- App/process exclusions, pause/resume controls, independent App Activity and Clipboard capture controls.
- Polished Memory Trail loading experience with a two-second startup target.
- Built-in **Check for Updates** and optional automatic daily release checks.
- Polished **What's New** experience presented on launch after a version change.
- Redesigned Settings privacy presentation aligned with the Memory Trail macOS visual language.
- **Clear App Cache** replaces the previous Reset Preferences wording while preserving activity history.
- Restored the complete six-page first-run tutorial for Version 1, including users upgrading from development builds.
- Improved activity-count readability with white count badges, quick-filter pills, and summary cards.
- Source code and privacy documentation are available for review.

## Earlier development builds

All earlier numbered builds were development iterations leading to **Version 1.0.0** and are superseded by the first official release.
