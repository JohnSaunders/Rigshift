# Design notes

Why the parts a contributor might otherwise tidy up are the way they are. The
behaviour itself is in the [configuration guide](configuration.md); this is the
reasoning behind it.

## The step list is closed

`Step` (`Rigshift/Execution/Step.cs`) is a closed set of records: start a process,
end one, close a window by title, run a WSL command, add a loopback port proxy,
and so on. A config file composes those steps; it never supplies a command line of
its own. That is the property that keeps a user-writable file from becoming a
script.

`StepClass.RequiresElevation` is exhaustive on purpose — an unclassified step type
throws rather than defaulting. Adding a step means adding a record, a case in
`WindowsStepExecutor`, and a line in `StepClass`. Everything lands in one reviewable
place: `WindowsStepExecutor` is the only class that shells out.

The one place config text reaches a command line is a `wslService` definition and
the process names an `app` definition acts on. `ShellText` checks that text first.
A double quote ends the quoted region `cmd.exe` is parsing, and a percent sign is
expanded even inside quotes; `cmd.exe` offers no backslash escape for a quote, so
the value cannot be passed through as data. It is refused instead, and the tray
names the value and the reason. `wslCommand` is bash text — bash uses `%` freely
(`date +%s`) — so only the quote is refused there.

## The elevation boundary

The tray runs unelevated. Five step kinds need administrator rights:
`PortProxyAdd`, `PortProxyRemove`, `KillWslService`, `FlushDns` and `KillHerdr`
(`StepClass`). `StartWslService` is **not** among them: its command text comes from
the writable config and must never run elevated.

`KillHerdr` is the broadest of the five. It runs
`taskkill /f /t /fi "USERNAME eq <account>" /im "herdr.exe"`: a fixed image name, a
filter built from the account the tray is already running as, and `/t` so pane
process trees go too. Nothing in it comes from the config, which is why it can be a
fixed token. It has to be elevated because a herdr server started over SSH by an
administrator account runs elevated, and an unelevated kill cannot reach it. The
cost is that tearing down a `herdr` definition always raises one prompt.

Elevation re-launches `Rigshift.exe --admin <tokens>`. Any argument at all sends
the process down the helper path — `Program.cs` returns there before the tray
exists, which is why the single-instance mutex is taken on the tray path only. The
helper reads no config:

- the tokens carry no paths, process names, window titles or command text — only a
  closed vocabulary: `proxy-add:<port>`, `proxy-remove:<port>`, `kill-wslservice`,
  `flush-dns`, `kill-herdr`;
- at most 16 of them;
- a port must be 1–65535, written as plain decimal digits with no sign and no
  leading zero (`AdminPayload.TryParsePort` does not use `int.TryParse`, which
  would accept signs and spaces);
- anything else exits `64` before a step runs.

The payload is not authenticated, and safety does not depend on it being so. It
depends on the vocabulary being closed: a stranger who works out how to run
`rigshift.exe --admin` can ask for those five things and nothing else — things they
could already do with tools Windows ships. `kill-herdr` is the only token that names
a process, and the name is compiled in rather than supplied.

## Stopping a process means `taskkill /f`

There is no `close` field and no graceful close path. Stopping an `app` is a forced
termination with no save prompt, for the built-in loadouts and for a user's own
alike. The README and the configuration guide state the consequence rather than
softening it. A graceful close would be a behaviour change, not a bug fix.

The built-in `Game` loadout therefore omits `rider`, `webstorm` and `vscode` from
its kill list on purpose. It frees `node` and the local-model stack — which is what
pressing "Game" is for — without ending a document someone has open. Its
definitions still exist as examples, so a user who wants the whole purge can name
them in their own loadout in one line of JSON.

## Config lookup, and re-reading

The first file that exists wins: `%APPDATA%\Rigshift\rigshift.json`, then
`rigshift.json` beside the executable, then the built-in defaults. That order is
what lets a portable copy work without a service.

The file is re-read whenever the tray menu opens, so a hand edit takes effect
without a restart. Settings always writes the per-user path — not the file it
loaded — and keeps the previous bytes as `rigshift.json.bak`.

## Distribution

Releases are not code-signed, so Windows SmartScreen warns on first run. The README
says so plainly rather than leaving the reader to wonder whether something is wrong,
and `release.yml` publishes `SHA256SUMS.txt` next to the installer and the zip so a
download can be verified where a signature would otherwise be the answer. The file
is written with LF endings and no BOM because `sha256sum` on other platforms reads a
carriage return as part of the file name.

## One instance

A second launch exits quietly. The tray takes the named mutex
`Rigshift.SingleInstance` before it does anything else.

## Theming

`Rigshift/Settings/Theme.cs` is the only place a colour literal for window chrome
lives. Loadout colours come from the config and are data, not theme.
