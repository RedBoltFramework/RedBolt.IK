// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

using RedBolt.IK.Editor;
using RedBolt.Plugins;

[assembly: PluginModule("RedBolt.IK.Editor", typeof(IKEditorPlugin),
    ReloadPolicy = PluginReloadPolicy.ProcessResident, AutoLoad = true)]

namespace RedBolt.IK.Editor;

using System;

using RedBolt.Editor.Core;
using RedBolt.Utility;

using EditorProject = RedBolt.Editor.Project.Project;

/// <summary>Developer-only IK reflection and asset-pipeline integration for the standard RedBolt Editor.</summary>
public sealed class IKEditorPlugin : EditorPluginApplication
{
}

[EditorExtension("IK.DefaultRetargetTransformer")]
internal static class IKDefaultRetargetTransformerExtension
{
    public static IDisposable Apply(EditorProject project) =>
        project.RegisterDefaultAssetTransformer<RetargetAnimationsTransformer>();
}
