## What changed

<!-- One behaviour per change, as CONTRIBUTING.md asks. Say what a user would notice. -->

## Checks

- [ ] `dotnet build -c Release` is clean, with no new warnings
- [ ] `dotnet test` passes
- [ ] If this changes documented behaviour, `README.md`, `docs/configuration.md`
      and `Rigshift/rigshift.example.json` agree with the code
- [ ] The diff carries no personal paths, hostnames, tokens or other identifiers
- [ ] Nothing here weakens the elevation boundary: the admin path still takes only
      the closed step list and still reads no config
