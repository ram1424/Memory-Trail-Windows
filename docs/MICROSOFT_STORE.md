# Microsoft Store / MSIX Packaging

Memory Trail Version 1.4 includes a dedicated Microsoft Store MSIX build.

## Partner Center identity

Keep the existing reserved Memory Trail identity unchanged:

- **Package/Identity/Name:** `RomanZakharov.MemoryTrail`
- **Package/Identity/Publisher:** `CN=E557088E-D2D5-43E9-9789-6E873A1C61AA`
- **Package/Properties/PublisherDisplayName:** `Roman Zakharov`

These values are stored in `Packaging/MSIX/StoreIdentity.json` and validated during every Store build.

## Version 1.4

The application, GitHub release, and Microsoft Store package are all aligned to **Memory Trail Version 1.4**.

GitHub and user-facing artifacts use:
- tag: `v1.4`
- release name: `Memory Trail Version 1.4`
- ZIP: `Memory-Trail-Windows-1.4-x64.zip`
- MSIX filename: `Memory-Trail-1.4-x64.msix`
- MSIX package version: **1.4.0.0**

## Build the Store package

GitHub → Actions → **Build Microsoft Store MSIX** → **Run workflow**.

The Store build:
1. publishes the Windows x64 application,
2. packages it as `Memory-Trail-1.4-x64.msix`,
3. verifies the Partner Center package identity,
4. verifies the monotonic Store manifest version,
5. uploads the workflow artifact,
6. attaches the MSIX to the matching `v1.4` GitHub release.

Microsoft Store signs/re-signs the package during publishing. The unsigned Store MSIX should not be used for normal direct sideload distribution.
