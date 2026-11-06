## Version 1.3 highlights

### Visual quality and typography
- Standardized all human-readable Windows UI text on **Segoe UI Variable Text** with Segoe UI only as fallback.
- Increased undersized labels and supporting text, strengthened secondary-text contrast, and removed visual effects that softened WPF text.
- Retained ClearType, display text formatting, fixed hinting, pixel snapping, layout rounding, and Per-Monitor-V2 DPI awareness.

### Today
- Aligned **Top Apps Today** with **Today at a Glance**.
- Added real app icons, activity counts, and rank badges to Top Apps Today.
- Added **Back to Top** for long timelines.
- Removed artificial activity visibility limits while retaining list virtualization for performance.

### Ask Memory Trail
- Redesigned shortcut questions into a clearer 3×2 layout with proper Windows vector icons.
- Increased search/result typography and contrast.
- Standardized Ask results to equal-height rows with consistent timestamp alignment and truncation.
- Added real app icons to every result.
- Removed fixed result caps so the full matching local history can be returned.
- Added **Back to Top** and kept recycling virtualization enabled for large result sets.

### Navigation
- Enlarged and bolded Today, Ask Memory Trail, Favorites, Clipboard History, Insights, and Settings.
- Increased primary navigation icon sizes and improved icon/text alignment.
- Strengthened contrast and spacing so primary navigation stands out from filters.

### What’s New and secondary windows
- Corrected What’s New to use Segoe UI Variable.
- Increased small text sizes and contrast in What’s New and removed a heavy shadow that softened text.
- Standardized onboarding, privacy, update, splash, history, and cache windows on Segoe UI Variable.

### Reliability
- Fixed the Windows build failure caused by an ambiguous UI Automation `Condition.TrueCondition` reference.
- Expanded Microsoft Store workflow triggers so capture/model changes rebuild the Store MSIX.
- Verified both direct-download ZIP and Store MSIX release paths.

## Microsoft Store submission

- Product: existing **Memory Trail** Store listing
- Display version: **1.3**
- MSIX package version: **1.3.0.0**
- Architecture: **x64**
- Package identity: **RomanZakharov.MemoryTrail**
- Publisher identity remains unchanged
- Upload `Memory-Trail-1.3-x64.msix` to a new update submission in Partner Center

## Privacy

Captured timeline history remains local to the PC. Ask Memory Trail continues to search local history without sending captured activity to a cloud search service. Store/update requests do not include timeline entries, clipboard content, window titles, or file paths.

See `docs/PRIVACY.md` for the complete privacy and data-handling description.
