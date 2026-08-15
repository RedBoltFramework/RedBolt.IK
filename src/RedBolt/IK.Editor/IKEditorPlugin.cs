// Copyright (c) 2026 RedBolt contributors.
// This work is licensed under the terms of the MIT license.

using RedBolt.IK.Editor;
using RedBolt.Plugins;

[assembly: PluginModule("RedBolt.IK.Editor", typeof(IKEditorPlugin),
    ReloadPolicy = PluginReloadPolicy.ProcessResident, AutoLoad = true)]

namespace RedBolt.IK.Editor;

using System;

using RedBolt.Core;
using RedBolt.Editor.Core;
using RedBolt.Plugins;
using RedBolt.Utility;

using EditorProject = RedBolt.Editor.Project.Project;

/// <summary>Developer-only IK reflection and asset-pipeline integration for the standard RedBolt Editor.</summary>
public sealed class IKEditorPlugin : PluginApplication
{
    protected override void Load()
    {
        _ikReflection = Context.RegisterAssemblyScoped(typeof(IKSolver).Assembly);
        _toolsReflection = Context.RegisterAssemblyScoped(typeof(RetargetAnimationsTransformer).Assembly);
        _registration = GetSubsystem<EditorPluginManager>().AddScopedPlugin<EditorProject>(
            "IK.DefaultRetargetTransformer",
            static project => project.RegisterDefaultAssetTransformer<RetargetAnimationsTransformer>());
    }

    protected override void Unload()
    {
        _registration?.Dispose();
        _registration = null;
        _toolsReflection?.Dispose();
        _toolsReflection = null;
        _ikReflection?.Dispose();
        _ikReflection = null;
    }

    private IDisposable? _registration;
    private AssemblyRegistration? _toolsReflection;
    private AssemblyRegistration? _ikReflection;
}
