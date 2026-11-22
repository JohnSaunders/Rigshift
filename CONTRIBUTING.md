# Contributing

Issues and pull requests are welcome.

## Setup

Rigshift is a Windows tray app. Development needs Windows 10 or 11 and the
[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
dotnet build
dotnet test
```

Launch `Rigshift\bin\Debug\net10.0-windows\Rigshift.exe` (or the Release build). Use the
executable rather than `dotnet run`: elevation relaunches `Environment.ProcessPath`, which
points at `dotnet.exe` under `dotnet run`.

## Config

Copy [`Rigshift/rigshift.example.json`](Rigshift/rigshift.example.json) to
`%APPDATA%\Rigshift\rigshift.json` and edit that file. `rigshift.json` is gitignored.
Anything personal stays out of the repository.

The loader checks the per-user file, then `rigshift.json` beside the executable, then the
built-in default. The [configuration guide](docs/configuration.md) is the field reference.

## Pull requests

- Keep a change focused on one behaviour.
- Add or update tests when you change planning, config loading, or tint selection.
  `ActionRunner` is pure and should stay free of I/O.
- The step list is closed. New behaviour composes the existing step types, and an
  unclassified step type throws rather than defaulting. See
  [design notes](docs/design-notes.md) for why, and for the elevation boundary a
  change must not weaken.
- CI runs `dotnet build`, `dotnet test`, and gitleaks.

`Rigshift.slnx` opens in Rider or Visual Studio.
