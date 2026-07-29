# Releasing Knarr

This guide defines the manual release process for Knarr. It documents the intended Windows SignPath
Foundation signing path and macOS Apple notarization path; neither packaging nor release automation is
configured in the repository yet.

## Release policy

Knarr releases must be built from reviewed source in the canonical
[hard-rox/knarr](https://github.com/hard-rox/knarr) repository. Only first-party artifacts built from
that source and its approved release process may be submitted for code signing.

Every source-control or signing authority holder must use multi-factor authentication. Contributions
from non-committers require a maintainer review before merge. The repository owner approves each
signing request until a public maintainer team is established.

Knarr does not transfer information to networked systems unless explicitly requested by its operator.
Releases must not introduce system changes without clear user-facing notice and must provide an
uninstallation path.

## Prerequisites

Before preparing a release:

1. Confirm the release commit is reviewed, merged, and has a clean working tree.
2. Assign one consistent product version to all first-party release artifacts and metadata.
3. Run the repository quality gates:
   ```sh
   dotnet format --verify-no-changes
   dotnet restore Knarr.slnx
   dotnet build Knarr.slnx -c Release --no-restore
   dotnet test Knarr.slnx -c Release --no-build
   ```
4. Prepare release notes, checksums, signature or notarization verification instructions, and an
   uninstall path.
5. Do not submit secrets, proprietary components, or binaries that are not built from this
   repository's source for signing.

## Initial public release

SignPath Foundation requires software to be publicly released in the form that will be signed. Before
applying for signing, publish an initial downloadable release on the project's
[GitHub releases page](https://github.com/hard-rox/knarr/releases). The release page must describe
Knarr's functionality and link to the repository's [Code signing policy](../README.md#code-signing-policy).

Do not label that initial artifact as SignPath-signed unless it has completed the approved signing
request and signature verification steps below.

## Windows signing with SignPath Foundation

After SignPath Foundation approves the project and its artifact configuration:

1. Build the Windows installer and application binaries through the approved, reproducible release
   process.
2. Confirm product-name and version metadata meet the SignPath artifact restrictions for every
   first-party binary.
3. Submit only the approved release artifacts to SignPath.io.
4. Have the designated approver manually approve the signing request for that release.
5. Download the signed artifacts, verify their signatures, publish their checksums and release notes,
   then attach them to the GitHub release.

Every signing request requires manual approval. Do not attempt to bypass SignPath controls or sign
artifacts from unreviewed or untrusted sources.

## macOS signing and notarization

SignPath Foundation does not replace Apple's distribution requirements. A macOS release requires an
Apple Developer membership, a Developer ID Application certificate, and notarization credentials.

1. Build the macOS application and package from the reviewed release commit.
2. Sign the app and distributable package with the Developer ID certificate.
3. Submit the package to Apple's notarization service and wait for acceptance.
4. Staple the notarization ticket to the distributed artifact.
5. Verify the signature and notarization result before publishing the artifact, checksums, and release
   notes.

## Current implementation gaps

The checked-in project has no release packaging configuration, installer artifacts, application
product/version metadata, SignPath integration, Apple signing configuration, notarization setup, or
tag-triggered release workflow. The existing [CI workflow](../.github/workflows/ci.yml) validates pull
requests only.

Before the first release, implement those capabilities in a separate, reviewed packaging initiative.
That work should establish reproducible Windows and macOS artifacts, preserve the quality gates above,
and keep signing credentials exclusively in the selected secure CI or signing service.
