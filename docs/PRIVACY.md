# Memory Trail Privacy Policy

**Effective date:** November 3, 2026  
**Last updated:** November 3, 2026

Memory Trail is designed as a **privacy-first, locally run Windows application**. Its core purpose is to help you review your recent computer activity without sending your captured activity history to a remote service.

## 1. What Memory Trail captures

Depending on the features you enable and the information Windows makes locally available, Memory Trail may capture and store information such as:

- The applications and windows you use.
- Window titles and related local app context.
- Process names and executable paths used to identify applications.
- Local file or document names and paths when they are part of captured activity.
- Email or conversation subject/title information that is visible locally in supported applications.
- Clipboard text when clipboard capture is enabled.
- Memories, favorites, notes, and other information you intentionally add inside Memory Trail.
- Local timestamps and categories used to organize your activity timeline.

Memory Trail does **not intentionally capture passwords or credentials**. Sensitive applications can be excluded from capture, and default exclusions are provided for common credential-related processes.

## 2. Where captured data is processed and stored

Captured activity is processed on your Windows PC and stored locally on that computer.

Memory Trail does **not** upload, sync, or transmit your captured timeline history, clipboard history, window titles, document paths, conversation titles, or manually added memories to Memory Trail servers. Memory Trail does not require a Memory Trail cloud account for its capture and storage workflow.

## 3. Cloud services, analytics, advertising, and external connectors

Memory Trail does not use cloud sync for captured activity, advertising SDKs, behavioral advertising, third-party analytics services, or external data connectors for its core capture and storage workflow.

**Memory Trail does not sell your captured activity data.**

## 4. Update checks

If automatic update checks are enabled, Memory Trail contacts GitHub when the app launches to retrieve public Memory Trail release metadata. You may also initiate an update check manually.

An update request may include ordinary network information that is inherently visible to GitHub, such as your IP address, standard connection metadata, and a Memory Trail user-agent/version string.

Memory Trail does **not** include your timeline entries, clipboard contents, window titles, document paths, conversation titles, or manually added memories in update-check requests.

When you choose to update, Memory Trail may download the published Windows update package from GitHub, verify the package digest when available, stage the update under your local application-data folder, install it, and relaunch the app.

GitHub's handling of requests to its service is governed by GitHub's own privacy terms.

## 5. Your controls

Memory Trail gives you controls to:

- Pause all capture.
- Enable or disable app/window activity capture.
- Enable or disable clipboard capture.
- Exclude Windows processes from capture.
- Clear your activity history.
- Clear the app cache and reset Memory Trail preferences without deleting activity history.
- Enable or disable automatic update checks.
- Skip an available update or postpone an update reminder.

## 6. Data retention

Memory Trail keeps locally captured activity until you remove it, clear the activity history, remove the local application data, or uninstall/delete the application's local data.

**Clear App Cache** resets cached capture state and preferences but does not delete your activity history.

Removing Memory Trail itself may not automatically remove every local data file created by the app. If you want to remove locally stored Memory Trail data, delete the Memory Trail data stored in your Windows Local AppData location after closing the app.

## 7. Security

Memory Trail is designed to keep captured activity local to your PC. However, locally stored data is protected by the security of your Windows account, device, file-system permissions, backups, disk encryption, and any other software that can access your user profile.

If another person or program has access to your Windows account or local files, that person or program may also be able to access Memory Trail data.

## 8. Windows, third-party applications, and external services

Memory Trail reads information made locally available by Windows and supported applications. Memory Trail does not control the privacy practices of Windows, GitHub, Microsoft Outlook, Microsoft Teams, web browsers, or other third-party applications and services you use.

Those products may independently collect or process information under their own privacy policies.

## 9. Children's privacy

Memory Trail is a general-purpose productivity application and is not designed specifically for children. Memory Trail does not operate a remote account system that knowingly collects children's captured activity.

## 10. Changes to this policy

This policy may be updated when Memory Trail's features, storage behavior, update system, or privacy controls change.

Material privacy-related changes will be described in the Memory Trail changelog and, when appropriate, surfaced in the app.

## 11. Source-code transparency

Memory Trail's source code is available for review. Users and administrators can inspect how local capture, storage, privacy controls, and update checks are implemented.

## 12. Contact

For privacy questions, feedback, or requests related to Memory Trail, use the contact methods published with the Memory Trail project or its GitHub repository.

**Do not include passwords, credentials, private timeline content, clipboard contents, or other sensitive personal information in a public GitHub issue.**

## Summary

> **Memory Trail is built around a simple principle: your captured activity belongs on your computer.**

The app's core activity history is processed and stored locally, and it is not sent to a Memory Trail cloud service.
