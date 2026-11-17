<img src="assets/tsudev-logo-144.png" alt="tsudev" height="56">

# Tsu Swico

[Tiếng Việt](README.md) · **English**

Check Windows and Office licensing status and collect a hardware inventory with **a single `.exe`** - no .NET Runtime
to install. A product of [tsudev](https://tsudev.com). Product page: [swico.tsudev.com](https://swico.tsudev.com)
(downloads, guides, sample reports you can open in the browser). The user interface and reports are in Vietnamese.

[![CI](https://github.com/tsudev-org/tsuswico/actions/workflows/ci.yml/badge.svg)](https://github.com/tsudev-org/tsuswico/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/tsudev-org/tsuswico)](https://github.com/tsudev-org/tsuswico/releases/latest)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/16837/badge)](https://www.bestpractices.dev/projects/16837)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/tsudev-org/tsuswico/badge)](https://scorecard.dev/viewer/?uri=github.com/tsudev-org/tsuswico)

**Offline by design.** Every scan runs fully without Internet access or an account. The only network call is the
check for a newer version, which can be turned off with `--no-update-check`. Scan data never leaves the machine
([PRIVACY.md](PRIVACY.md)).

## What it does

- Reads Windows and Office activation status and flags signs of unauthorized activation (KMS emulators, known
  activator traces) using an updatable rule set (`detection-rules.json`).
- Collects hardware and software inventory: CPU, memory, disks, installed programs, UWP packages, services, scheduled
  tasks, startup entries.
- Writes four formats at once: **HTML** (to read), **JSON** (to integrate), **XLSX** and **CSV** (to process further).
- Ready for RMM and GPO: `--silent`, an installer that supports `/VERYSILENT`, and exit codes that separate tool
  health from the verdict.

## Install

```powershell
winget install --id tsudev.SWICO -e          # installer, Windows 10/11
scoop bucket add tsudev https://github.com/tsudev-org/scoop-bucket
scoop install tsudev/tsuswico                # portable
```

Or download from [Releases](https://github.com/tsudev-org/tsuswico/releases): the installer
(`tsuswico_<version>_x64-setup.exe`), the portable zip, or the bare `swico.exe`. All three contain the same program.
The winget id `tsudev.SWICO` is kept after the rename to Tsu Swico so existing installs keep receiving updates.

### Verify downloads

Starting with the release after 26.8.18.2, `SHA256SUMS.txt` is signed with **Sigstore keyless** signing and every file
carries GitHub **build provenance**:

```bash
cosign verify-blob SHA256SUMS.txt --bundle SHA256SUMS.txt.sigstore.json \
  --certificate-identity-regexp '^https://github.com/tsudev-org/tsuswico/\.github/workflows/release\.yml@' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
sha256sum -c SHA256SUMS.txt --ignore-missing
gh attestation verify tsuswico_<version>_x64-setup.exe -R tsudev-org/tsuswico
```

Authenticode signing of the `.exe` is pending approval by SignPath Foundation ([docs/SIGNING.md](docs/SIGNING.md)).

## Usage

```text
swico.exe                          Full scan, open the report when done
swico.exe --scope license          Licensing only
swico.exe --scope hardware         Hardware only
swico.exe --silent --sfc           Deep scan, no browser (GPO/RMM)
swico.exe -o D:\Reports --silent   Write output to a folder
swico.exe --rules .\new-rules.json Scan with an updated rule set
swico.exe --no-verdict-exit        Verdict does not affect the exit code
swico.exe --help                   All options
```

The tool asks for Administrator rights because reading licensing data requires them.

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Completed, no issue found |
| 1 | Completed, some data could not be read (main report still complete) |
| 2 | Fatal error, no report written |
| 3 | Invalid command-line arguments |
| 10 | Verdict: **warning** (for example, Office not activated) |
| 20 | Verdict: **critical** (signs of unauthorized activation) |
| 30 | **Update required** before scanning (`--silent` only) |
| 130 | Cancelled with Ctrl+C |

Precedence when several apply: `2 > 3 > 130 > 20 > 10 > 1 > 0`. If your RMM treats every non-zero code as a failure,
add `--no-verdict-exit`.

## Build from source

Requires the .NET 8 SDK (pinned in `global.json`). The scanner runs on Windows; the core library and the 180+ unit
tests also run on Linux.

```bash
dotnet test tests/unittests -c Release
pwsh ./build.ps1          # Windows: build, test, publish/swico.exe, installer if Inno Setup is present
```

## Contributing and security

Bug reports and suggestions: [issues](https://github.com/tsudev-org/tsuswico/issues). Report vulnerabilities privately
as described in [`.well-known/security.txt`](website/public/.well-known/security.txt) on the product site.

## License

[Apache-2.0](LICENSE). You may fork and distribute modified versions, but you **may not use the names "tsudev" or
"Tsu Swico"** to publish them (section 6 of the license). See [NOTICE](NOTICE). Reports are technical data for
reference, not a legal conclusion ([EULA.txt](EULA.txt) section 3).
