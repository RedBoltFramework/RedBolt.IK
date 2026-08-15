# Building RedBolt.IK

RedBolt.IK is an external RedBolt module and expects the `RedBolt` repository in the sibling directory. Dependencies are resolved through RedBolt's CPM-based build; no git submodule is used.

Build the default desktop configuration:

```sh
dotnet build RedBolt.IK.slnx --configuration Debug
```

Build only the runtime assembly against an existing engine build:

```sh
dotnet build src/RedBolt/IK/RedBolt.IK.csproj \
  --configuration Debug --no-restore \
  -p:RedBoltKeepParentRuntimeAssemblies=true
```

Use the same platform and architecture selection properties documented by the main RedBolt repository for Android, WebAssembly, and Windows builds.
