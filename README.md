# RedBolt.IK

Inverse-kinematics support for [RedBolt](https://github.com/RedBoltFramework/RedBolt).

The repository is split by shipping intent:

- `RedBolt.IK` contains runtime solvers and scene components. Games reference this assembly directly.
- `RedBolt.IK.Tools` contains animation-retargeting infrastructure intended for asset tooling.
- `RedBolt.IK.Editor` is an automatically discovered Editor companion. When present, it registers IK reflection metadata and adds the retargeting transformer to the default asset pipeline.

The Editor companion uses RedBolt's standard plugin system. Core has no IK-specific interface or dependency; removing the companion removes the integration.

See [doc/BUILDING.md](doc/BUILDING.md) for build commands.
