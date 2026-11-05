# Microsoft Store / MSIX Packaging

Memory Trail includes a dedicated Microsoft Store MSIX build.

## Use the MSIX submission path

In Partner Center, use an **MSIX or PWA app** product. MSIX packages are uploaded directly to Partner Center; they do not use the MSI/EXE **Package URL** field.

Microsoft Store signs MSIX packages during Store submission, so a CA-trusted signing certificate is not required for the Store-submitted MSIX.

## Partner Center identity

The Store package is now pinned to the reserved **Memory Trail** Partner Center identity:

- **Package/Identity/Name:** `RomanZakharov.MemoryTrail`
- **Package/Identity/Publisher:** `CN=E557088E-D2D5-43E9-9789-6E873A1C61AA`
- **Package/Properties/PublisherDisplayName:** `Roman Zakharov`

These values are stored in `Packaging/MSIX/StoreIdentity.json` and are validated against the generated manifest during every MSIX build.

## Build the Store package

Open GitHub → Actions → **Build Microsoft Store MSIX** → **Run workflow**. No identity fields need to be entered manually.

The Store build is compiled with the `MicrosoftStore` distribution channel. That build disables Memory Trail’s GitHub self-updater and relies on Microsoft Store for package updates. The normal direct-download build keeps the existing updater.

## Store package output

The workflow generates `Memory-Trail-1.2-x64.msix`, validates the Partner Center identity in the manifest, uploads it as a workflow artifact, and attaches it to the matching GitHub release.

The package is intentionally not developer-signed for Store submission; Microsoft Store signs/re-signs MSIX packages during the publishing process. Do not use the unsigned Store package for direct sideload distribution outside Microsoft Store.

## Versioning

Memory Trail 1.2 is packaged as **1.2.0.0**. Future Store updates must use increasing four-part MSIX versions. Keep the existing Partner Center identity unchanged so updates target the current Memory Trail listing.
