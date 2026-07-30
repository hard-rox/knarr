# Privacy Policy

Last updated: 2026-07-30

## Overview

Knarr is a local desktop application that provides a graphical interface for first-party operating-system container CLIs. Knarr is designed to act as a transparent orchestration layer: actions taken in the UI are translated into CLI commands on the local machine.

This policy explains what information Knarr processes, where that information lives, and what controls users have.

## Scope

This policy applies to:

- The Knarr desktop application source code in this repository
- Data handled by the application on a user's device during normal use

This policy does not apply to:

- External tools, operating systems, or vendor CLIs used alongside Knarr
- Third-party websites, package registries, or services you access separately

## Information Knarr Processes

### 1. User-provided and environment data

To perform requested actions, Knarr may process information that is already present on your machine or entered by you, including:

- Container/image names and metadata
- Command arguments and options you choose
- CLI output (standard output and error output)
- Host environment details required to choose the correct platform CLI (for example, operating system)

### 2. Application configuration data

Knarr may store local configuration data necessary for app behavior, such as:

- UI preferences (for example, theme selection)
- Non-sensitive app state used to improve usability

### 3. Diagnostic and operational information

Knarr may display and process operational details in the UI for transparency and troubleshooting, including:

- The exact command text executed through the underlying CLI
- Exit codes and command output shown in the interface

## What Knarr Does Not Intend to Do by Default

Based on the project design, Knarr is intended to run locally and does not require a hosted account or cloud backend for core behavior.

- No required user account for normal use
- No built-in advertising profile
- No sale of personal information

If future releases introduce optional telemetry, cloud sync, or account-backed features, this policy should be updated before those features are enabled.

## How Data Is Used

Knarr uses processed data only to:

- Execute user-requested container management actions
- Render relevant status and output in the UI
- Persist local preferences and state needed for normal app operation

## Legal Basis and User Rights (General)

Depending on your jurisdiction, you may have rights regarding personal data, including rights to access, correct, delete, or restrict processing of certain information.

Because Knarr is generally local-first software, most actionable control is exercised directly by you on your own device (for example, clearing local app data or removing the app).

## Data Sharing and Disclosure

Knarr is designed so that your data remains on your device during normal usage. Knarr does not intentionally broker your data to data brokers or advertisers.

Data can still be exposed outside Knarr when you explicitly use external systems, for example:

- When underlying vendor CLIs interact with remote container registries or services
- When you choose to share logs, screenshots, or command output

## Data Retention

Knarr retains data locally only as needed for application function and user experience.

- Runtime command output is shown for user visibility
- Any persisted local settings remain until changed, cleared, or the app is removed

Retention of data handled by external tools or services is governed by those systems, not by Knarr.

## Security

Knarr follows a transparency-oriented architecture and relies on OS-level security boundaries plus the security posture of the underlying CLI tools.

No software can guarantee absolute security. You should:

- Keep your OS and CLI tooling updated
- Use least-privilege accounts where practical
- Review commands before running sensitive operations

## Children

Knarr is a developer tool and is not directed to children.

## International Use

Because Knarr is open-source software that can be used globally, users are responsible for ensuring their own use complies with applicable local privacy and data-protection laws.

## Open-Source Transparency

Knarr is open source. You can inspect code and propose privacy improvements through repository contributions.

## Changes to This Policy

This policy may change over time as the project evolves. Material privacy-impacting updates should be reflected in this file with an updated "Last updated" date.

## Contact

For questions or concerns about this policy, open an issue in this repository so maintainers can respond publicly and transparently.
