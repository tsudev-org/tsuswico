# Privacy statement

**Applies to:** `Tsu Swico` (the `swico.exe` CLI). The "TsuDEV account" section applies from the first release that
ships `swico login` (`26.11.2201`).
**Updated:** 22/11/2026 · [Tiếng Việt](PRIVACY.md) (the Vietnamese text is authoritative)

## Summary

- **Not signed in (the default, with every scanning feature):** the software **sends no data about your computer
  anywhere**. It makes exactly **one** network connection - asking GitHub whether a newer version exists - and that
  connection can be turned off with `--no-update-check`. It creates no files besides the reports you ask for.
- **Signed in to a TsuDEV account (optional, `swico login`):** adds connections to `tsudev.com` to fetch TsuDEV Pro
  entitlements. They **only happen after you sign in yourself**, never carry scan results, and stop when you run
  `swico logout`. The full list is in "TsuDEV account".

## Data that is read

Licensing state (Windows/Office activation, partial product keys, licensing channel), hardware (system, board, CPU,
RAM, disks, serial numbers), installed software, Defender state and detections, DISM/SFC results (only on request),
and the computer, domain and signed-in user names. None of it is used as a device identifier or sent to `tsudev.com`,
even when signed in.

## Network while not signed in - exactly one connection

On start-up the tool sends **one** GET request to
`https://api.github.com/repos/tsudev-org/tsuswico/releases/latest`. Sent: the public IP address (unavoidable for any
HTTP request) and `User-Agent: TsuSwico/<version>`. **Not sent:** anything the tool collects. If you choose "Update",
the installer and `SHA256SUMS.txt` of that release are downloaded and the hash is **verified before running**.
`swico.exe --no-update-check` turns this off; while not signed in the tool then makes **no network connection at all**.

## TsuDEV account (optional)

Signing in only serves **TsuDEV Pro** entitlements (syncing/backing up reports across computers - under construction,
see `docs/adr/0002-luu-cuc-bo-pro-dong-bo.md`). Nothing is lost without an account.

### How sign-in works

`swico login` opens the computer's **default browser** at `https://tsudev.com/oidc/auth` (OpenID Connect,
Authorization Code + PKCE). You type your password **on tsudev.com's page in the browser** - `swico.exe` never sees it
and embeds no browser of its own. The page sends the result back to a temporary port on your own computer
(`http://127.0.0.1:<random port>/callback`, closed right after).

### Additional connections (only when signed in)

| When | Where | What is sent |
|---|---|---|
| `swico login` | `POST https://tsudev.com/oidc/token` | One-time sign-in code, PKCE verifier, `client_id` = `tsuswico` |
| `swico login`, `swico account`, start of an **interactive** scan when the saved entitlement is older than 24 hours | `POST https://tsudev.com/oidc/token` | Refresh token (exchanged for a new one - the old one stops working) |
| same | `GET https://tsudev.com/oidc/jwks` | Nothing but IP + `User-Agent` (public keys to verify signatures) |
| same | `GET https://tsudev.com/api/me/entitlements` | Access token + **installation ID** (`X-Tsu-Installation` header) |
| `swico logout` | `POST https://tsudev.com/oidc/token/revocation` | Refresh token (so tsudev.com revokes it) |

Every connection carries the public IP and `User-Agent: TsuSwico/<version>`.

There is **never** an account connection with `--silent` or `--json-only` (RMM/GPO deployment), even when signed in.
If the network fails or tsudev.com does not answer, the tool behaves as the free edition and **keeps scanning**.

The **installation ID** is 128 random bits generated at the first sign-in on this computer - **not** derived from serial
numbers, MAC addresses, the Windows product key or any scan data. tsudev.com stores only an HMAC of it to count the
devices using the account over 30 days (`devices_30d`); records inactive for 90 days are deleted. Tsu Swico only gets
the **number** back, never a device list.

**Scan results are NOT sent** over any of these connections. When report syncing (Pro) ships it will be end-to-end
encrypted, and this document **must** be updated before that release.

### What is stored on the computer

Only after signing in, in `%LOCALAPPDATA%\tsudev\tsuswico\` of the Windows account running the tool:
`refresh-token.dpapi` (refresh token, **encrypted with Windows DPAPI** - only this Windows account on this computer can
decrypt it), `account.json` (TsuDEV account ID `sub` and sign-in time - no e-mail, no name), `entitlement.jws` and
`jwks.json` (signed entitlement valid for at most 7 days, and the public keys to check it offline), `installation-id`,
and `state.json` (when the "multiple devices without Pro" notice was last shown, plus the Pro end date this computer last saw and the one already reported as expired, so the renewal reminder and the expiry notice show correctly). No password is stored. Nothing is
written to the registry.

### Deleting it

`swico.exe logout` revokes the refresh token at tsudev.com (when online) and **deletes the whole folder above**,
including the installation ID. Scan reports are not deleted. The server session also expires after 30 days without
use; you can sign out remotely on tsudev.com's "Devices and sessions" page.

## Where scan data goes

**Only to the disk of that computer**, in the folder you choose (by default next to the executable), as HTML, JSON,
XLSX and CSV. Scan results **never** leave the computer - signed in or not. A **log file** is only created when you add
`--log-file <file>`; it holds the lines printed on screen (including the computer name and output folder), step names
and durations, the exit code and error details - **no** scan results and **no** tokens. The tool never sends it
anywhere.

## Verify it yourself

The source is public. All code that touches the network lives in **two files**:
`src/Tsudev.Audit.Windows/UpdateAdapters.cs` (update check and download) and `src/Tsudev.Audit.Core/Pro/ProHttp.cs`
(the account connections above; only called when signed in - `ProAccount.cs`, guarded by the `ProOfflineGuardTests`
tests). Threat model: [`docs/THREAT_MODEL.md`](docs/THREAT_MODEL.md).
