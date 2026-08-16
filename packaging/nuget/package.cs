// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

#:project ../../../RedBolt/src/RedBolt/Build/Packaging/RedBolt.ExternalModule.Packaging.csproj

using System.Runtime.CompilerServices;
using RedBolt.ExternalModule.Packaging;

return ExternalModulePackager.Run(args, new ExternalModulePackageManifest
{
    Name = "RedBolt.IK",
    BasePackages = ["RedBolt.Core", "RedBolt.Utility"],
    BuildSteps =
    [
        new("IK/RedBolt.IK.csproj", Arguments: "-p:BuildProjectReferences=false"),
        new("IK.Tools/RedBolt.IK.Tools.csproj", Arguments: "-p:BuildProjectReferences=false")
    ],
    ManagedPackages =
    [
        new(
            "RedBolt.IK",
            "Inverse-kinematics solvers and scene components for RedBolt.",
            [new("build/bin/Release/linux-x64/core/RedBolt.IK.dll", "lib/net10.0/RedBolt.IK.dll")],
            [
                new("RedBolt.Core", PackageVersions.RedBolt)
            ]),
        new(
            "RedBolt.IK.Tools",
            "Animation retargeting utilities for RedBolt IK developer workflows.",
            [new("build/bin/Release/linux-x64/core/RedBolt.IK.Tools.dll", "lib/net10.0/RedBolt.IK.Tools.dll")],
            [
                new("RedBolt.IK", PackageVersions.Module),
                new("RedBolt.Utility", PackageVersions.RedBolt)
            ])
    ],
    Consumer = new(
        "RedBolt.IK",
        """
        using System;
        using RedBolt.IK;
        Console.WriteLine(typeof(IKSolver).FullName);
        """)
}, SourcePath());

static string SourcePath([CallerFilePath] string path = "") => path;
