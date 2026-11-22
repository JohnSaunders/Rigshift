# Changelog

Notable changes to Rigshift. Versions match the git tags, and the released installer and
portable build are on the [releases page](https://github.com/JohnSaunders/Rigshift/releases).

## 0.1.0

First public release.

- Loadouts in a tray menu: pick one and Rigshift runs its steps. Toggle loadouts stack, and
  switching one off stops what it started unless another enabled loadout still needs it.
- Step kinds: `app`, `wsl`, `wslService` and `herdr`.
- Administrator work stays inside a short-lived `Rigshift.exe --admin <steps>` process that
  never reads the config, and accepts four cases only: `netsh` port proxies,
  `taskkill /f wslservice.exe`, `taskkill /f /t` of your own `herdr.exe`, and
  `ipconfig /flushdns`.
- A settings window for the common fields. The config lives in `%APPDATA%\Rigshift\rigshift.json`.
- Built-in loadouts AI, Dev and Game. Game stops Node, the local model processes and WSL,
  and flushes the DNS cache.
- Per-machine installer (Inno Setup) and a self-contained portable zip, both x64.
