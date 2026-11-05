# Memory Trail Changelog

A concise record of production changes. Each release is organized by area so updates are easy to scan.

## Version 1.1

### ✨ Insights
- **Reference-style dashboard** — rebuilt Insights with four summary cards, an hourly activity pattern chart, peak-time and focus-streak metrics, Top Items, category distribution, and a Quick Summary panel.
- **Real local data** — Insights is calculated from the user's local Memory Trail timeline rather than demo values.

### 🎨 Visual Quality
- **Crisper Windows typography** — uses standard Segoe UI, stronger text contrast, ClearType/display formatting, layout rounding, and pixel snapping for sharper WPF lettering.
- **Less text softening** — key text-container shadows were removed where they caused WPF to rasterize lettering.

### 🗂 Data & History
- **Storage location** — Settings now shows where Memory Trail's capture data is stored locally.
- **Open and export** — users can open the local data folder or export activity to a readable JSON file.
- **Flexible history clearing** — clear the last 7 days, last 30 days, or all history.
- **Playful confirmation graphics** — each history range has its own witty confirmation and success state.

### 🔄 Updates
- **Friendlier update experience** — up-to-date, update-available, and post-update/changelog screens now use polished, witty Memory Trail messaging.
- **Microsoft Store handoff** — Store builds clearly hand manual update checks to Microsoft Store without pretending to perform a separate GitHub check.

### 🪟 Windows
- **Start with Windows restored** — the setting is interactive again for both direct-download and Microsoft Store builds.
- **Version 1.1** — About, GitHub release, ZIP, update metadata, and Store MSIX use the 1.1 release identity.

---

## Version 1.2 Build 7

### 🎨 Visual Quality
- **Crisper Windows typography** — switched the WPF text stack to standard Segoe UI for stronger Windows hinting and sharper glyph edges.
- **Higher text contrast** — darkened primary, secondary, and tertiary text tones slightly to reduce the washed-out appearance without changing the overall UI palette.
- **ClearType reinforcement** — enabled ClearType hints, fixed text hinting, display-mode formatting, pixel snapping, and layout rounding across shared text controls.
- **Sharper layered panels** — removed container-level drop shadows that caused WPF to rasterize text-heavy panels and soften lettering.

### 🖥 Display Scaling
- **High-DPI consistency** — retains Per-Monitor-V2 DPI awareness and strengthens rendering behavior for common 100%, 125%, 150%, and 200% Windows scaling levels.
- **UI design preserved** — spacing, structure, branding, and approved app icon remain unchanged.

---

## Version 1.2 Build 6

### 🛠 Fixes
- **New Outlook subject capture** — captures the subject from the active message reading pane instead of Copilot, Focused Inbox, navigation, or message-list labels.
- **Correct sender matching** — captures sender metadata from the same active reading pane and avoids stale Classic Outlook COM selections.
- **Activity Details restored** — sender/subject details are visible again in the right-side details pane, and double-click reopens that pane.
- **Insights layout** — retains the spacing fix that prevents Top Items from clipping into Activity Patterns.

### 📦 Release
- **Version 1.2 Build 6** — About, ZIP release, update metadata, and Store MSIX all use the same explicit build number.

---


## Version 1.0.5

### 📦 Distribution
- **Microsoft Store MSIX packaging** — added a dedicated x64 MSIX build pipeline for Store submission.
- **Store-managed updates** — Microsoft Store builds disable Memory Trail’s GitHub self-updater so Store owns package updates.
- **Partner Center identity support** — the MSIX manifest uses the exact Package Name, Publisher, and Publisher Display Name assigned by Microsoft.
- **Final Partner Center identity** — Store packages are now pinned to `RomanZakharov.MemoryTrail` and the Microsoft-assigned publisher identity.
- **Manifest verification** — every Store build validates Name, Publisher, Publisher Display Name, and four-part package version before publication.
- **Store release attachment** — the final MSIX is attached to the matching GitHub release after the release tag is confirmed to point to the same commit.

### 🔒 Privacy
- **Full in-app Privacy Policy** — added a dedicated, scrollable privacy-policy window accessible directly from Settings.
- **Local-first disclosures** — documents what Memory Trail may capture, where data is stored, and what is never included in update-check requests.
- **Update transparency** — explains GitHub release checks, ordinary network metadata, package downloads, digest verification, staging, installation, and relaunch behavior.
- **User controls and retention** — documents pause/capture controls, exclusions, history deletion, Clear App Cache behavior, update preferences, and local data retention.
- **Security boundaries** — clarifies that locally stored data is protected by the user's Windows account, device security, file permissions, backups, and disk encryption.
- **Source transparency** — retains an explicit path to review Memory Trail's source implementation.

### ✨ Experience
- **Privacy actions grouped together** — Settings now presents **View Privacy Policy** and **Review Source Code** together inside the Privacy card.
- **Readable policy layout** — added a polished Memory Trail-branded policy viewer with clear sections, effective date, and local-first summary.

---

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
