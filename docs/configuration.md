# Rigshift configuration

This is the field reference and the behaviour that the [README](../README.md) leaves out. It matches `main` as of this writing: stopping an `app` always uses `taskkill /f`. There is no `close` field yet.

## Lookup

Config is re-read every time the tray menu opens. The first file that exists is used:

1. `%APPDATA%\Rigshift\rigshift.json` — `Environment.SpecialFolder.ApplicationData`. Settings and hand editing both write this file. A new build does not replace it.
2. `rigshift.json` beside `Rigshift.exe` — optional portable file. Skipped when the per-user file exists.
3. Built-in defaults — same loadouts as [`Rigshift/rigshift.example.json`](../Rigshift/rigshift.example.json).

A missing file is silent. A file that exists but cannot be parsed falls back to the built-in defaults, shows a balloon, and does not continue to the next location. `Open Config…` opens the file that won; when none exists, it copies the example into the per-user path and opens that.

`//` comments and trailing commas are accepted when reading. Settings always saves `%APPDATA%\Rigshift\rigshift.json` as indented JSON, without comments or trailing commas. When that save replaces an existing user file, the previous bytes are kept as `rigshift.json.bak`. `rigshift.json` is gitignored.

Paths in `path` are expanded with environment variables when the process starts.

## Top level

| Field | Meaning |
|---|---|
| `idleColor` | Tray tint when no toggle loadout is enabled. Default `#8A8A8E`. |
| `definitions` | Named things that loadouts reference. |
| `loadouts` | An array. Menu order follows it. |

## Definitions

| Field | Applies to | Meaning |
|---|---|---|
| `kind` | all | `app`, `wsl`, `wslService`, or `herdr`. |
| `processName` | `app`, `wslService`, `herdr` | Process name without `.exe`. For `herdr`, the core process (`hc`). |
| `path` | `app`, `herdr` | Optional. When set, enabling launches it. An `app` definition cannot take arguments; if you need flags, point `path` at a shortcut. |
| `windowTitle` | `wslService` | Console window title, used to close it on teardown. |
| `port` | `wslService` | Port to proxy. |
| `wslCommand` | `wslService` | Command line run inside WSL. |
| `portProxy` | `wslService` | Whether to manage a `netsh` loopback proxy. |
| `requires` | any | Definitions to start first. Transitive. |

There is no Windows Service kind. `wslService` is a process inside WSL, with an optional loopback port proxy.

Values that reach a command line are checked before the step runs. `processName`,
`windowTitle` and the WSL process pattern refuse a double quote and a percent
sign; `wslCommand` refuses a double quote only, because bash uses `%` freely
(`date +%s`). A refused step is skipped and the tray names the value it refused.
Nothing is rewritten silently: `cmd.exe` ends a quoted region at a quote and
expands `%NAME%` even inside quotes, so neither character can be passed through as
data.

`herdr` starts like `app`. Teardown kills `processName` as the user, then kills every `herdr.exe` owned by the current account, pane process trees included, through the elevated helper. A `herdr` server started over SSH by an administrator account runs elevated, so an unelevated kill cannot reach it. Teardown always shows a UAC prompt.

The built-in AI loadout uses native Windows Ollama (`app`), not `wslService`. Hosting a server inside WSL is one use of `wslService`. The start sequence should not be reordered: `wsl pkill -f "<processName> serve"`; wait 1s; `start "<windowTitle>" /min wsl -e bash -c "<wslCommand>"`; wait 3s; read the WSL IP from the first token of `wsl hostname -I`; delete then add the `netsh` v4-to-v4 loopback proxy.

## Loadouts

| Field | Meaning |
|---|---|
| `name` | Menu label. |
| `toggle` | `true` is a switch that stays on until clicked again. `false` is a button that runs once. |
| `color` | Tray tint, `#RRGGBB`. Used for toggles. |
| `start` | Started on enable. |
| `killOnDisable` | Torn down on uncheck, subject to reference counting. |
| `kill` | Torn down by a run-once loadout. Ignores reference counting. |
| `hardShutdown` | After `wsl --shutdown`, also kill `wslservice.exe`. |
| `flushDns` | Run `ipconfig /flushdns` once at the end. |

## Icon tinting

The tray icon ships as a flat white silhouette (`Assets\rigshift.ico`) and is recoloured at runtime. The executable uses the same glyph in the accent cyan, `#35C9E8` (`Assets\rigshift-app.ico`). **Last enabled wins:** enabling a second toggle retints to that colour, while the first stays active. On a cold start there is no session history, so config order decides and the last-listed active loadout wins. Anything missing or malformed falls back to `idleColor`.

## How state is decided

The checkmark is session-scoped intent, not a live probe of processes, WSL, or ports. Quitting and relaunching resets every toggle to off, even if the process is still running.

Toggle loadouts stack. `killOnDisable` skips anything another currently enabled loadout still lists in its `start`, including definitions reached through `requires`. When that suppresses every step, the tray says so instead of prompting for elevation, and the loadout is labelled `(in use)` in the menu. In the built-in config, AI and Dev share no definitions, so unchecking one does not keep the other running.

A one-shot loadout ignores reference counting. It clears the checkmark of every switch it hits: a switch is hit when the loadout kills anything that switch starts or requires, and `hardShutdown` hits every `wsl` and `wslService` definition. With the built-in config, Game hits both AI and Dev.

## Elevation

The tray runs unelevated. Only four kinds of step need administrator rights, each in a short-lived `Rigshift.exe --admin <steps>` process that never reads the config: `netsh` port proxies, `taskkill /f wslservice.exe`, `taskkill /f /t` of the current user's `herdr.exe`, and `ipconfig /flushdns`.

A loadout that only starts or stops your own programs shows no UAC prompt. You get at most one prompt per action, and only when an admin step is required. The user steps run first and are never rolled back, so declining that prompt skips only the administrator steps: the balloon names the part that was not done, and the switch still reflects the steps that ran. A loadout with no user steps is left untouched, and its balloon says so. The helper accepts only that closed list of steps, so `path` and `wslCommand` never run elevated.

## Built-in AI notes

Ollama's listen address comes from `OLLAMA_HOST`. Ollama's default is `127.0.0.1:11434`. Rigshift does not set it.

Launched bare, `ollama app.exe` opens its main window. `--hide` starts it in the tray only, but an `app` definition cannot take arguments, so the flag has to live in a shortcut:

```powershell
$dir = Join-Path $env:APPDATA 'Rigshift'
New-Item -ItemType Directory -Force $dir | Out-Null
$s = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $dir 'ollama-hidden.lnk'))
$s.TargetPath = Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama app.exe'
$s.Arguments = '--hide'
$s.WorkingDirectory = Join-Path $env:LOCALAPPDATA 'Programs\Ollama'
$s.Save()
```

Point the `ollama` definition's `path` at `%APPDATA%\Rigshift\ollama-hidden.lnk`.

## Portable zip at login

The setup program can add **Start Rigshift when anyone signs in**. For the portable zip, shortcut the exe into your Startup folder. A shortcut still works after you replace the folder with a newer unzip; a copied exe would not.

```powershell
$startup = [Environment]::GetFolderPath('Startup')
$exe = 'C:\path\to\Rigshift.exe'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $startup 'Rigshift.lnk'))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = (Split-Path $exe)
$shortcut.IconLocation = $exe
$shortcut.Save()
```

If you used this shortcut before switching to the installer, delete `Rigshift.lnk` from your Startup folder. The installer's sign-in shortcut lives in the all-users Startup folder and replaces it.

## Known limitations

- **Stopping an `app` is `taskkill /f`.** No save prompt. The built-in Game loadout deliberately does not kill Rider, WebStorm or VS Code, because those hold unsaved work; a loadout you write can. See `kill` under [Loadouts](#loadouts).
- **Process matching is coarse.** Killing `node` ends every Node process.
- **`wsl --shutdown` returns before the VM is gone.** A re-enable immediately after a disable may race with WSL still tearing down.
- **State follows what was done, not a probe.** If an admin step fails or a prompt is declined after the per-user steps already ran, the switch still reflects those per-user steps and a balloon names what was left undone. Re-toggling re-applies the full start or kill plan.
- **A portable install folder must not be user-writable.** If `Rigshift.exe` lives somewhere the Users group can write, a later prompt you approve could run a swapped-in executable as administrator. The setup program installs under `C:\Program Files\Rigshift`, which is already read-only for Users.
