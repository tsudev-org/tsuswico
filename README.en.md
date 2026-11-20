<img src="assets/tsudev-logo-144.png" alt="TsuDEV" height="56">

# Tsu Swico

[Tiếng Việt](README.md) · **English**

Check Windows and Office licensing status and collect a hardware inventory with **a single `.exe`** - no .NET Runtime
to install. A product of [TsuDEV](https://tsudev.com). Product page: [swico.tsudev.com](https://swico.tsudev.com)
(downloads, guides, sample reports you can open in the browser). The user interface and reports are in Vietnamese.

[![CI](https://github.com/tsudev-org/tsuswico/actions/workflows/ci.yml/badge.svg)](https://github.com/tsudev-org/tsuswico/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/tsudev-org/tsuswico)](https://github.com/tsudev-org/tsuswico/releases/latest)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/16837/badge)](https://www.bestpractices.dev/projects/16837)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/tsudev-org/tsuswico/badge)](https://scorecard.dev/viewer/?uri=github.com/tsudev-org/tsuswico)

**Offline by design.** Every scan runs fully without Internet access or an account. While you are not signed in, the
only network call is the check for a newer version, which can be turned off with `--no-update-check`. Signing in to a
TsuDEV account (`swico login`) is optional and only fetches TsuDEV Pro entitlements. Scan data never leaves the machine
([PRIVACY.en.md](PRIVACY.en.md), [threat model](docs/THREAT_MODEL.md)).

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
(`tsuswico_<version>_windows-x64-setup.exe`), the portable zip, or the bare `swico.exe`. All three contain the same program.
The winget id `tsudev.SWICO` is kept after the rename to Tsu Swico so existing installs keep receiving updates.

### Verify downloads

Starting with the release after 26.8.18.2, `SHA256SUMS.txt` is signed with **Sigstore keyless** signing and every file
carries GitHub **build provenance**:

```bash
cosign verify-blob SHA256SUMS.txt --bundle SHA256SUMS.txt.sigstore.json \
  --certificate-identity-regexp '^https://github.com/tsudev-org/tsuswico/\.github/workflows/release\.yml@' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
sha256sum -c SHA256SUMS.txt --ignore-missing
gh attestation verify tsuswico_<version>_windows-x64-setup.exe -R tsudev-org/tsuswico
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
swico.exe --json-only > result.json  JSON to stdout only (no files written)
swico.exe --lang vi                Vietnamese interface (auto|vi|en)
swico.exe --log-file swico.jsonl   Structured log to send for support
swico.exe --help                   All options

swico.exe login                    Sign in to a TsuDEV account (optional)
swico.exe account                  Show the account, Pro plan and device count
swico.exe logout                   Sign out and delete account data on this computer
```

**TsuDEV account (optional).** `swico login` opens the default browser at tsudev.com (OIDC + PKCE; the result comes
back to a temporary `127.0.0.1` port on the same machine); the password is only ever typed on tsudev.com. The refresh
token is stored DPAPI-encrypted in `%LOCALAPPDATA%\tsudev\tsuswico\`. These three commands are interactive only - they
cannot be combined with `--silent`/`--json-only`, and `--silent`/`--json-only` scans never contact tsudev.com even when
signed in. Nothing is lost without an account. Full list of connections and files: [PRIVACY.en.md](PRIVACY.en.md).

**Language**, in order of precedence: `--lang` ▸ the `TSUSWICO_LANG` environment variable ▸ the Windows display
language (Vietnamese ⇒ `vi`, anything else ⇒ `en`). It covers the CLI, the update dialog, the HTML/XLSX reports and
the summary page. JSON keys are never translated, so integrations work the same in both languages.

**`--json-only`** (implies `--silent`): stdout carries exactly ONE JSON document wrapping every report produced;
progress lines and messages go to stderr; no files are written and no browser opens. Meant for RMM tools and scripts
reading from a pipe.

**`--log-file <file>`**: appends a JSON Lines log (one event per line: `run.start`, `update.check`, `step.ok`/`step.fail`
with durations, `report.done`, `run.fatal` with the stack trace, `run.end` with the exit code) - send this file when
asking for support instead of a screenshot. Nothing is written without this option.

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

[Apache-2.0](LICENSE). You may fork and distribute modified versions, but you **may not use the names "TsuDEV" or
"Tsu Swico"** to publish them (section 6 of the license). See [NOTICE](NOTICE). Reports are technical data for
reference, not a legal conclusion ([EULA.txt](EULA.txt) section 3).
