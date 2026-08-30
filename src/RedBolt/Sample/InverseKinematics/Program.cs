// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Sample.InverseKinematics;

using System;
using System.Collections.Generic;

using RedBolt;
using RedBolt.Bindings.DearImGui;
using RedBolt.Core;
using RedBolt.DearImGui;
using RedBolt.Engine;
using RedBolt.Graphics;
using RedBolt.IK;
using RedBolt.Input;
using RedBolt.Math;
using RedBolt.RenderPipeline;
using RedBolt.Scene;
using Widgets = RedBolt.SystemUI.Widgets;

internal static class InverseKinematicsProgram
{
    private static int Main()
    {
        try { return new InverseKinematicsApplication().Run(); }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

sealed class InverseKinematicsApplication : Application
{
    private readonly List<Node> _handles = [];
    private readonly List<IKLimbSolver> _limbSolvers = [];
    private readonly Dictionary<Node, Node> _handleJoints = [];
    private readonly Dictionary<Node, IKLimbSolver> _handleSolvers = [];
    private readonly HashSet<Node> _effectorHandles = [];
    private Scene? _scene;
    private Node? _characterNode;
    private IKSolver? _ikSolver;
    private Node? _cameraNode;
    private Octree? _octree;
    private OutlineGroup? _outlineGroup;
    private Node? _draggedHandle;
    private Vector3f _dragTargetPosition;
    private Texture2D[]? _controlPrompts;
    private Plane _dragPlane;

    public InverseKinematicsApplication()
    {
        RedBolt.IK.IK.RegisterIKLibrary(Context.Instance);
    }

    protected override void Setup(EngineParameters parameters)
    {
        parameters.ApplicationName = "RedBolt.Sample.InverseKinematics";
        parameters.Borderless = false;
        parameters.FullScreen = false;
        parameters.ResourcePaths = "SampleData.IK;CoreData;SampleData";
        parameters.ResourcePrefixPaths = ".;..";
        parameters.WindowMaximize = false;
#if !REDBOLT_PLATFORM_BROWSER
            parameters.WindowWidth = 1280;
            parameters.WindowHeight = 720;
#endif
        parameters.WindowTitle = "RedBolt Inverse Kinematics";
    }

    protected override void Start()
    {
        Context.RegisterSubsystem(new DearImGui((ImGuiConfigFlags)Engine.Parameters.DearImGuiFlags));
        SubscribeToEvent<DearImGuiEvents.EndRenderingDearImGuiEvent>(
            (ref DearImGuiEvents.EndRenderingDearImGuiEvent _) => DrawInstructions());
        SubscribeToEvent<IKEvents.IKPostSolveEvent>(
            (ref IKEvents.IKPostSolveEvent _) => OnIKPostSolve());

        CreateScene();
        Renderer.SetViewport(0, new Viewport(_scene!, _cameraNode!.GetComponent<Camera>()!));
        _controlPrompts =
        [
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_w.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_a.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_s.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_d.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/mouse_right.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/mouse_left.png")
        ];

        Input.SetMouseMode(MouseMode.Free);
        Input.SetMouseVisible(true);
    }

    private void CreateScene()
    {
        var cache = Resources;
        _scene = new Scene();
        _octree = _scene.CreateComponent<Octree>();
        _outlineGroup = _scene.CreateComponent<OutlineGroup>();
        _outlineGroup.SetColor(Color.Yellow);

        var zoneNode = _scene.CreateChild("Zone");
        var zone = zoneNode.CreateComponent<Zone>();
        zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
        zone.SetAmbientColor(new Color(0.5f, 0.5f, 0.5f));
        zone.SetFogColor(new Color(0.4f, 0.5f, 0.8f));
        zone.SetFogStart(100.0f);
        zone.SetFogEnd(300.0f);

        var lightNode = _scene.CreateChild("DirectionalLight");
        lightNode.SetDirection(new Vector3f(0.6f, -1.0f, 0.8f));
        var light = lightNode.CreateComponent<Light>();
        light.SetLightType(LightType.Directional);
        light.SetCastShadows(true);
        light.SetColor(new Color(0.5f, 0.5f, 0.5f));
        light.SetShadowBias(new BiasParameters(0.00025f, 0.5f));

        var characterNode = _scene.CreateChild("Character");
        _characterNode = characterNode;
        characterNode.SetRotation(new Quaternion(180.0f, Vector3f.Up));
        var character = characterNode.CreateComponent<AnimatedModel>();
        character.SetModel(cache.Get<Model>("ThirdParty/Quaternius/TechnicalDummy.glb.d/Models/Mannequin.mdl"));
        var characterMaterial = cache.Get<Material>("Materials/TechnicalDemo/Yellow.xml");
        character.SetMaterial(0, characterMaterial);
        character.SetMaterial(1, characterMaterial);
        character.SetCastShadows(true);
        _ikSolver = characterNode.CreateComponent<IKSolver>();
        _ikSolver.SetEnabled(false);

        CreateLimb(characterNode, "upperarm_l", "lowerarm_l", "hand_l",
            "LeftHandTarget", "LeftElbowTarget");
        CreateLimb(characterNode, "upperarm_r", "lowerarm_r", "hand_r",
            "RightHandTarget", "RightElbowTarget");
        CreateLimb(characterNode, "thigh_l", "calf_l", "foot_l",
            "LeftFootTarget", "LeftKneeTarget");
        CreateLimb(characterNode, "thigh_r", "calf_r", "foot_r",
            "RightFootTarget", "RightKneeTarget");
        _cameraNode = _scene.CreateChild("Camera");
        _cameraNode.SetPosition(new Vector3f(0.0f, 1.8f, -5.0f));
        _cameraNode.LookAt(new Vector3f(0.0f, 1.1f, 0.0f));
        _cameraNode.CreateComponent<FreeFlyController>();
        var camera = _cameraNode.CreateComponent<Camera>();
        camera.SetFarClip(100.0f);
        void UpdateShadowCascades() => light.SetShadowCascade(ShadowCascadeEstimator.Estimate(camera,
            new Vector2i(Graphics.Width, Graphics.Height), new RenderPipelineSettings(),
            new ShadowCascadeEstimateSettings(200.0f) { ShadowResolution = light.GetShadowResolution() }));
        UpdateShadowCascades();
        SubscribeToEvent<GraphicsEvents.ScreenModeEvent>(
            (ref GraphicsEvents.ScreenModeEvent _) => UpdateShadowCascades());
    }

    private void CreateLimb(
        Node characterNode,
        string firstBoneName,
        string secondBoneName,
        string thirdBoneName,
        string targetName,
        string bendTargetName)
    {
        var secondBone = characterNode.GetChild(secondBoneName, true)!;
        var thirdBone = characterNode.GetChild(thirdBoneName, true)!;
        var targetHandle = CreateHandle(characterNode, targetName, thirdBone);
        var bendHandle = CreateHandle(characterNode, bendTargetName, secondBone);

        var solver = characterNode.CreateComponent<IKLimbSolver>();
        solver.FirstBoneName = firstBoneName;
        solver.SecondBoneName = secondBoneName;
        solver.ThirdBoneName = thirdBoneName;
        solver.TargetName = targetName;
        solver.BendTargetName = bendTargetName;
        _limbSolvers.Add(solver);
        _handleSolvers.Add(targetHandle, solver);
        _handleSolvers.Add(bendHandle, solver);
        _effectorHandles.Add(targetHandle);
    }

    private Node CreateHandle(Node characterNode, string name, Node joint)
    {
        var handleNode = characterNode.CreateChild(name);
        handleNode.SetWorldPosition(joint.WorldPosition);
        handleNode.SetScale(0.16f);
        var handle = handleNode.CreateComponent<StaticModel>();
        handle.SetModel(Resources.Get<Model>("Models/Sphere.mdl"));
        handle.SetMaterial(Resources.Get<Material>("Materials/DefaultWhite.xml"));
        _handles.Add(handleNode);
        _handleJoints.Add(handleNode, joint);
        return handleNode;
    }

    protected override void Update(float timeStep)
    {
        if (Input.ExitRequested || Input.GetKeyPress(Key.Escape))
            Exit();

        if (_draggedHandle is null && Input.GetMouseButtonPress(MouseButton.Left)
            && !ImGui.GetIO().WantCaptureMouse)
            BeginDrag();

        if (_draggedHandle is not null)
        {
            if (!Input.GetMouseButtonDown(MouseButton.Left))
                EndDrag();
            else
                UpdateDrag();
        }
        else
            SyncHandlesToJoints();
    }

    private void BeginDrag()
    {
        var camera = _cameraNode!.GetComponent<Camera>()!;
        var ray = camera.GetScreenRayFromMouse();
        using var query = new RayOctreeQuery(ray, RayQueryLevel.RayTriangle, 100.0f, DrawableFlags.Geometry);
        _octree!.Raycast(query);
        Node? node = null;
        for (var i = 0; i < query.Result.Count; ++i)
        {
            var candidate = query.Result[i].Drawable?.Node;
            if (candidate is not null && _handles.Contains(candidate))
            {
                node = candidate;
                break;
            }
        }
        if (node is null)
            return;

        _draggedHandle = node;
        _dragTargetPosition = node.WorldPosition;
        _dragPlane = new Plane(_cameraNode.WorldDirection, node.WorldPosition);
        _outlineGroup!.ClearDrawables();
        _outlineGroup.AddDrawable(node.GetComponent<StaticModel>()!);
    }

    private void UpdateDrag()
    {
        var ray = _cameraNode!.GetComponent<Camera>()!.GetScreenRayFromMouse();
        var distance = ray.HitDistance(_dragPlane);
        if (!float.IsPositiveInfinity(distance))
        {
            _dragTargetPosition = ray.Origin + ray.Direction * distance;
            var delta = _dragTargetPosition - _draggedHandle!.WorldPosition;
            if (delta.LengthSquared <= MathDefs.Epsilon * MathDefs.Epsilon)
                return;

            var selectedSolver = _handleSolvers[_draggedHandle];
            for (var i = 0; i < _limbSolvers.Count; ++i)
                _limbSolvers[i].PositionWeight = ReferenceEquals(_limbSolvers[i], selectedSolver) ? 1.0f : 0.0f;
            _draggedHandle.SetWorldPosition(_dragTargetPosition);
            _ikSolver!.SetEnabled(true);
        }
    }

    private void EndDrag()
    {
        SyncHandlesToJoints();
        _ikSolver!.SetEnabled(false);
        _draggedHandle = null;
        _outlineGroup!.ClearDrawables();
    }

    private void SyncHandlesToJoints()
    {
        foreach (var (handle, joint) in _handleJoints)
            handle.SetWorldPosition(joint.WorldPosition);
    }

    private void OnIKPostSolve()
    {
        if (_draggedHandle is not null && _effectorHandles.Contains(_draggedHandle))
        {
            var draggedJoint = _handleJoints[_draggedHandle];
            var residual = _dragTargetPosition - draggedJoint.WorldPosition;
            if (residual.LengthSquared > 0.001f * 0.001f)
                _characterNode!.SetWorldPosition(_characterNode.WorldPosition + residual);
        }
        SyncHandlesToJoints();
        _ikSolver!.SetEnabled(false);
    }

    protected override void Stop()
    {
        _outlineGroup?.ClearDrawables();
        _handles.Clear();
        _limbSolvers.Clear();
        _handleJoints.Clear();
        _handleSolvers.Clear();
        _effectorHandles.Clear();
        _scene?.Dispose();
        _scene = null;
        _characterNode = null;
        _ikSolver = null;
        _cameraNode = null;
        _octree = null;
        _outlineGroup = null;
        _draggedHandle = null;
    }

    private void DrawInstructions()
    {
        var windowOpen = true;
        ImGui.SetNextWindowPos(new Vector2f(10.0f, 10.0f), ImGuiCond.ImGuiCondAlways);
        if (ImGui.Begin("Inverse Kinematics"u8, ref windowOpen,
            ImGuiWindowFlags.ImGuiWindowFlagsNoResize | ImGuiWindowFlags.ImGuiWindowFlagsNoSavedSettings))
        {
            for (var i = 0; i < 3; ++i)
            {
                ImGui.Image(_controlPrompts![i], PromptIconSize);
                ImGui.SameLine();
            }
            DrawPrompt(_controlPrompts![3], "Move"u8);
            DrawPrompt(_controlPrompts[4], "Look"u8);
            DrawPrompt(_controlPrompts[5], "Drag joints"u8);
        }
        ImGui.End();
    }

    private static void DrawPrompt(Texture2D texture, ReadOnlySpan<byte> label)
    {
        ImGui.Image(texture, PromptIconSize);
        ImGui.SameLine();
        Widgets.AlignTextToLastItem();
        ImGui.TextUnformatted(label);
    }

    private static readonly Vector2f PromptIconSize = new(24.0f, 24.0f);
}
