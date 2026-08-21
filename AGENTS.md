# RedBolt.IK repository

## Scope

- `src/RedBolt/IK` is the game-shipping runtime module and depends only on RedBolt Core.
- `src/RedBolt/IK.Tools` contains developer-facing animation-retargeting utilities.
- `src/RedBolt/IK.Editor` is the optional Editor companion and integrates through the RedBolt plugin system.
- `src/RedBolt/Sample` and `bin/Data.IK` belong to this module.

Do not add an IK abstraction layer to RedBolt Core. Runtime consumers use `RedBolt.IK` directly, while Editor-only integration belongs in `RedBolt.IK.Editor`.

## Dependencies

The sibling `../RedBolt` repository is required. Shared MSBuild, packaging, Android, WebAssembly, and test infrastructure stays in RedBolt. Third-party source dependencies use CPM; do not add git submodules.

## Verification

Run one build invocation at a time and follow the build-concurrency and platform setup instructions in `../RedBolt/AGENTS.md`.
