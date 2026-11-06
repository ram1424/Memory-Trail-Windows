# Microsoft Store / MSIX Packaging

Memory Trail Version 1.0 includes a dedicated Microsoft Store MSIX build.

## Partner Center identity

Keep the existing reserved Memory Trail identity unchanged:

- **Package/Identity/Name:** `RomanZakharov.MemoryTrail`
- **Package/Identity/Publisher:** `CN=E557088E-D2D5-43E9-9789-6E873A1C61AA`
- **Package/Properties/PublisherDisplayName:** `Roman Zakharov`

These values are stored in `Packaging/MSIX/StoreIdentity.json` and validated during every Store build.

## Version 1.0 reset

The public application/display release is **Memory Trail Version 1.0**.

GitHub and user-facing artifacts use:
- tag: `v1.0`
- release name: `Memory Trail Version 1.0`
- ZIP: `Memory-Trail-Windows-1.0-x64.zip`
- MSIX filename: `Memory-Trail-1.0-x64.msix`

### Internal Store package version

Microsoft Store requires every package version submitted to an existing listing to be numerically higher than previous packages. Therefore the Version 1.0 Store build uses **1.4.0.0** internally in the MSIX manifest while the app's display/informational version remains **1.0**.

This internal version is a Store packaging requirement only; it does not change the public Memory Trail release name.

## Build the Store package

GitHub → Actions → **Build Microsoft Store MSIX** → **Run workflow**.

The Store build:
1. publishes the Windows x64 application,
2. packages it as `Memory-Trail-1.0-x64.msix`,
3. verifies the Partner Center package identity,
4. verifies the monotonic Store manifest version,
5. uploads the workflow artifact,
6. attaches the MSIX to the matching `v1.0` GitHub release.

Microsoft Store signs/re-signs the package during publishing. The unsigned Store MSIX should not be used for normal direct sideload distribution.
