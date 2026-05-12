# Buffaly SMS Tools

Buffaly SMS Tools contains the Buffaly SMS tool facade, provider HTTP client/contracts, helper logic, and tests for SMS send/read workflows.

Buffaly is a field-tested runtime for high-trust agents, developed by Matt Furnari. This repository is part of the public `buffaly-ai` source release and is intended for inspection, debugging, plugin/tool development, partner integration, and LLM-assisted understanding.

## How this fits into Buffaly

It demonstrates a typed external-system tool where Buffaly calls a constrained service boundary instead of embedding secrets directly into agent logic.

## What is in this repository

- SMS facade
- Provider HTTP client
- Raw/typed contract mapping
- Helper tests
- Integration/acceptance harnesses

## Repository map

- `Buffaly.Agent.Tools.Sms.Tests/Buffaly.Agent.Tools.Sms.Tests.csproj`
- `Buffaly.Agent.Tools.Sms/Buffaly.Agent.Tools.Sms.csproj`

## Build

This repository is source-visible first. The installer is still the recommended path for normal use, but the source is here so developers and partners can inspect behavior, debug integrations, and build plugins/tools.

```powershell
# From this repository root
dotnet restore buffaly.sms.sln
dotnet build buffaly.sms.sln --configuration Release
```

Some repositories include partner/closed support binaries under `lib/` so the public source can compile without immediately open-sourcing every historical dependency. More dependencies may be opened over time as time allows.

## Configuration and secrets

SMS provider credentials and API keys must come from user secrets/environment/deployment configuration. Do not commit phone numbers, message bodies, tokens, or customer communications.

If you add examples, keep them as placeholders. Never commit PHI, customer data, credentials, OAuth tokens, API keys, bearer tokens, connection strings with passwords, private browser state, or live run/session artifacts.

## What is intentionally not included

Private SMS provider credentials, live message data, PHI, and customer-specific routing rules are not included.

Some domain packs, healthcare workflows, customer-specific connectors, deployment assets, implementation playbooks, sensitive demos/data, and private operational configuration remain separate from the public core.

## Using this source

The source is provided to make Buffaly inspectable and useful for builders who want to understand the runtime, debug integrations, or create plugins and tools. For most users, the installer/runtime package is the fastest path. If you are building proprietary products, redistributing Buffaly, or need supported deployment terms, use the commercial licensing route below.

## Licensing

Buffaly core is GPLv3 by default. If your organization needs different terms for proprietary use, redistribution, or supported deployment, contact us for commercial licensing.

Buffaly is developed by Matt Furnari.

See [LICENSING.md](LICENSING.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## Commercial licensing

Commercial licensing is available for organizations that need different terms for proprietary use, redistribution, private embedding, hosted product use, or supported deployment. Open a GitHub issue in this repository with the label `commercial-licensing` to start that discussion.

## Contributions

Major external code contributions are expected to require a Contributor License Agreement (CLA). Small documentation fixes, typo fixes, and issue reports may be handled without a CLA at the maintainer's discretion.
