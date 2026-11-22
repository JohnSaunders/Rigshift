# Security

Rigshift relaunches itself as administrator to run a short, closed list of steps:
`netsh` port proxies, `taskkill /f wslservice.exe`, `taskkill /f /t` of the current
account's `herdr.exe` processes, and `ipconfig /flushdns`. The
elevated helper never reads the config, so a config file cannot choose what runs
elevated. That boundary is the part of this project most worth attacking, and a
report about it is welcome.

## Reporting a vulnerability

Use GitHub's private reporting form for this repository: **Security → Report a
vulnerability** — <https://github.com/JohnSaunders/Rigshift/security/advisories/new>.

Please do not open a public issue for a security problem.

A useful report says which version you are running (`About Rigshift…` shows it),
the config that reproduces the problem, what you expected instead, and — if you
have one — a proof of concept.

## What to expect

This is a small project maintained by one person. A reply may take a few days and
a fix longer, but you will get an answer either way. A reasonable window for a fix
to reach a release is 90 days. If you intend to publish sooner, say so in the
report and we can agree on a date.

## Supported versions

The latest `0.1.x` release. Earlier releases are not fixed.

## Scope

In scope: the elevation boundary, the config parser, the installer and
uninstaller, and anything that lets a config file run something the user did not
ask for.

Out of scope: how the programs you ask Rigshift to start or stop behave — including
`taskkill /f` ending a process and losing its unsaved work. That is documented
behaviour, not a defect.
