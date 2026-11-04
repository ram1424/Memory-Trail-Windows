# Memory Trail Changelog

A concise record of production changes. Each release is organized by area so updates are easy to scan.

## Version 1.0.4

### 🔄 Updates
- **Automatic refresh after update** — once an update finishes downloading and verification succeeds, Memory Trail now restarts itself automatically to apply the update.
- **Hands-off relaunch** — the hidden updater waits for the running app to close, replaces the staged files, and launches the updated Memory Trail executable automatically.
- **No manual reopening required** — users who choose **Update** no longer need to close or relaunch the app themselves.

### ✨ Experience
- **Clear update messaging** — the update dialog now explains that Memory Trail will briefly restart itself when the background download is ready.
- **Staged update recovery** — if an update was already downloaded, a manual update check can immediately restart and apply it.

### 🛡 Reliability
- **Single installer scheduling** — prevents the app shutdown path from accidentally starting a second updater process after the automatic restart flow has already been scheduled.
- **Existing safety retained** — downloaded update ZIPs continue to be SHA-256 verified before installation.

---

## Version 1.0.3

### ✨ Experience
- **Beautiful Clear Cache dialog** — replaced the plain confirmation message with a compact, polished Memory Trail modal inspired by the approved visual.
- **Safer cache confirmation** — the dialog clearly explains that temporary cache/preferences are reset while activity history remains intact.

### 🛠 Fixes
- **Outlook timeline icon rebuilt** — Today → Newest First now uses a true WPF vector Outlook icon instead of bitmap/executable extraction.
- **Outlook capture normalization** — classic Outlook, new Outlook (`olk`), and Outlook-family process names are normalized to **Microsoft Outlook** before timeline display.

### 🎨 Quality
- **Crisp vector artwork** — the Outlook icon remains sharp at all Windows scaling levels and monitor DPIs.
- **Changelog layout refreshed** — release notes now use categorized, scan-friendly bullet points going forward.

---

## Version 1.0.2

### 🛠 Fixes
- **Update message clipping** — fixed the background-download explanation on the update dialog at higher DPI/scaling.
- **Outlook icon handling** — bypassed unreliable packaged executable icon extraction for Outlook.

### 🎨 Quality
- **High-DPI rendering** — enabled Per-Monitor-V2 DPI awareness.
- **Sharper typography** — applied ClearType/display text formatting, fixed hinting, layout rounding, and pixel snapping.
- **Higher-resolution icons** — increased generated fallback artwork to a 4× render target.
- **Image clarity** — enabled high-quality image scaling across the app.

---

## Version 1.0.1

### ✨ Experience
- **Full first-run tutorial restored** for users upgrading from development builds.
- **Update experience** — added Update, Remind Me in 24 Hours, and Skip This Version.
- **Up-to-date confirmation** — added the polished “You’re All Caught Up!” screen.
- **Clear App Cache** — replaced the previous Reset Preferences terminology.

### ⚡ Performance
- **Faster Today filters** — removed unnecessary count/summary recomputation during filter switching.
- **Lighter live activity refresh** — reduced full timeline scans.

### 🛠 Fixes
- **Selected filter contrast** — selected Today filter counts now render white on the active blue tab.
- **Activity-count readability** — changed count badges, filter pills, and summary cards to white.
- **Tutorial Outlook icon** — replaced executable lookup with deterministic Outlook artwork.

### 🔄 Updates
- **Semantic patch releases** begin at 1.0.1.
- **Launch checks** — update checks run on each app launch when enabled.
- **Background staging** — downloads, verifies, and stages updates while the app remains usable.
- **Silent installation** — staged updates install after Memory Trail exits.
- **Same-version build detection** — build commit identity distinguishes refreshed packages with the same visible version.

---

## Version 1.0.0 — First Official Release

### 🚀 Core
- **Privacy-first local activity capture** with locally stored timeline history.
- **Today timeline** with newest-first activity and Live Activity state.
- **Ask Memory Trail** local search across websites, conversations, documents, files, apps, and time-aware activity.
- **Favorites, Clipboard History, manual memories, Insights, filters, and activity details.**

### 🔒 Privacy
- **Independent capture controls** for app activity and clipboard history.
- **Process exclusions** and pause/resume capture.
- **Local-first design** with source code and privacy documentation available for review.

### 🎨 Experience
- **Two-second startup target** with Memory Trail branding.
- **Settings privacy presentation** aligned with the Memory Trail macOS visual language.
- **First-run tutorial** and **What’s New** experience.

---

## Earlier development builds

Earlier numbered builds were development iterations leading to **Version 1.0.0** and are superseded by the production release line.
