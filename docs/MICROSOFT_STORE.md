# Microsoft Store / MSIX Packaging

Memory Trail includes a dedicated Microsoft Store MSIX build.

## Use the MSIX submission path

In Partner Center, use an **MSIX or PWA app** product. MSIX packages are uploaded directly to Partner Center; they do not use the MSI/EXE **Package URL** field.

Microsoft Store signs MSIX packages during Store submission, so a CA-trusted signing certificate is not required for the Store-submitted MSIX.

## Required Partner Center identity values

Open **Partner Center → Memory Trail → Product management → Product identity** and copy these values exactly:

1. **Package/Identity/Name**
2. **Package/Identity/Publisher**
3. **Package/Properties/PublisherDisplayName**

The manifest values are case-sensitive where applicable and must match Partner Center.

## Build the Store package

Open GitHub → Actions → **Build Microsoft Store MSIX** → **Run workflow**, then paste the three Product identity values.

The Store build is compiled with the `MicrosoftStore` distribution channel. That build disables Memory Trail’s GitHub self-updater and relies on Microsoft Store for package updates. The normal direct-download build keeps the existing updater.

## Development smoke package

Packaging-related pushes also create an unsigned development MSIX with a development-only identity. It validates the MakeAppx pipeline only; do not submit it to Partner Center.

## Versioning

Memory Trail 1.0.5 is packaged as **1.0.5.0**. Future Store updates must use increasing four-part MSIX versions.
