## Version 1.2 Build 7 highlights

- Sharper, crisper typography throughout the Windows UI.
- Switched WPF text rendering to standard Segoe UI for stronger Windows font hinting.
- Increased text contrast slightly to reduce washed-out lettering.
- Reinforced ClearType, display text formatting, fixed hinting, layout rounding, and pixel snapping.
- Removed text-softening container shadows from key panels and dialogs.
- Retains Per-Monitor-V2 high-DPI support and the existing Memory Trail design.

## Version 1.2 Build 6 highlights

- Fixed New Outlook capture so the timeline uses the active reading-pane subject instead of Copilot/UI chrome labels.
- Captures sender information from the same message shown in the reading pane.
- Prevents New Outlook from inheriting stale Classic Outlook COM selections.
- Restored Outlook sender/subject details in Activity Details and double-click details behavior.
- Retains the Insights overlap/high-DPI layout fix.
- Release identity corrected to Version 1.2 Build 6 across About, updater, ZIP, and MSIX.

## Version 1.0.5 highlights

- Full in-app Memory Trail Privacy Policy
- Clear disclosure of locally captured data and local storage
- GitHub update-check and network-metadata transparency
- User controls, retention, security boundaries, and source-code transparency
- Privacy Policy and Source Code actions grouped in Settings

## Previous 1.0.4 highlights

- Automatic app restart after an update finishes downloading
- Silent staged installation followed by automatic relaunch
- No manual close or reopen required after choosing Update
- Clearer update-dialog explanation of the restart behavior
- Existing SHA-256 package verification retained

## Previous 1.0.3 highlights

- Beautiful Clear Cache confirmation window
- True vector Outlook icon in Today → Newest First
- Outlook-family process normalization for classic and new Outlook
- Categorized, scan-friendly changelog format

## Core Memory Trail experience

- Privacy-first local activity capture and timeline storage
- Today timeline with Live Activity state
- Ask Memory Trail local search
- Browser, email, chat, file, document, app, and clipboard context when available
- Favorites, Clipboard History, manual memories, Insights, and filters
- Privacy exclusions and independent capture controls
- Two-second startup target with approved Memory Trail branding
- Launch-time update checking with Update / Remind Me in 24 Hours / Skip This Version
- Background update download and SHA-256 verification
- Silent staged installation with automatic Memory Trail relaunch
- Polished up-to-date confirmation screen for manual checks
- Per-Monitor-V2 DPI awareness and crisp ClearType rendering

## Versioning

Production releases use semantic patch versioning. Each normal push to `main` advances the patch number.

## Privacy

Captured timeline history remains local to the PC. Update checks are a separate request for GitHub release metadata and do not include timeline entries, clipboard content, window titles, or file paths.

See `docs/PRIVACY.md` for the complete privacy and data-handling description.
