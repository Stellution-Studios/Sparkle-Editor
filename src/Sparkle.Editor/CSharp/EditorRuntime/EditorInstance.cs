using System.Diagnostics;
using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using ImGuiNET;
using Sparkle.Editor.CSharp.EditorRuntime.Utilities;
using Veldrid;

namespace Sparkle.Editor.EditorRuntime;

public class EditorInstance {
    private Cam3D _cam;
    private ImGuiController _controller;
    private ImmediateRenderer _renderer;
    public CommandList CommandList;

    private readonly Stopwatch deltaTime = Stopwatch.StartNew();

    public GraphicsDevice GraphicsDevice;

    private string text = "";

    public EditorInstance() {
        Instance = this;
    }

    public static EditorInstance Instance { get; private set; }

    public IWindow Window { get; private set; }

    public void Setup() {
        Logger.Info("Loading...");

        var options = new GraphicsDeviceOptions {
            Debug = true,
            HasMainSwapchain = true,
            SwapchainDepthFormat = PixelFormat.D32FloatS8UInt,
            SyncToVerticalBlank = true,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SwapchainSrgbFormat = false
        };

        Window = Bliss.CSharp.Windowing.Window.CreateWindow(WindowType.Sdl3, 1280, 720, "Sparkle Editor",
            WindowState.Resizable, options, Bliss.CSharp.Windowing.Window.GetPlatformDefaultBackend(),
            out GraphicsDevice);
        Logger.Info("Window created.");

        Window.Resized += () =>
            OnResize(new Rectangle(Window.GetX(), Window.GetY(), Window.GetWidth(), Window.GetHeight()));

        CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
        Logger.Info("CommandList created.");

        GlobalResource.Init(GraphicsDevice);
        Logger.Info("Initialized Global Resources.");

        if (Window is Sdl3Window)
            Input.Init(new Sdl3InputContext(Window));
        else
            throw new Exception("Sdl3Window not supported.");
        Logger.Info("Initialized Input.");

        _renderer = new ImmediateRenderer(GraphicsDevice);

        _cam = new Cam3D(new Vector3(0, 2, -4), new Vector3(0, 0, 0), (float)Window.GetWidth() / Window.GetHeight(),
            Vector3.UnitY, ProjectionType.Perspective, CameraMode.Orbital, 90, 0.1f);

        _controller = new ImGuiController(GraphicsDevice, GraphicsDevice.SwapchainFramebuffer.OutputDescription,
            Window.GetWidth(), Window.GetHeight());
        Logger.Info("Completed loading.");
    }

    public void OnResize(Rectangle size) {
        GraphicsDevice.MainSwapchain.Resize((uint)size.Width, (uint)size.Height);
        _cam.Resize((uint)size.Width, (uint)size.Height);
        _controller.Resize(size.Width, size.Height);
    }

    public void Run() {
        while (Window.Exists) {
            Window.PumpEvents();
            Input.Begin();

            if (!Window.Exists) break;

            Update();
            _controller.Update(1.0f / deltaTime.ElapsedTicks);
            Draw(GraphicsDevice, CommandList);

            Input.End();

            deltaTime.Restart();
        }


        OnClose();
    }

    private void Update() {
        _cam.Update(1.0f / deltaTime.ElapsedTicks);
    }

    private void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        ImGui.Begin("TEST");
        ImGui.Text("TEST");
        ImGui.InputText("##textbox", ref text, 128);
        ImGui.End();

        commandList.Begin();
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.LightBlue.ToRgbaFloat());
        commandList.ClearDepthStencil(1.0f);

        _controller.Render(graphicsDevice, commandList);
        // _cam.Begin();
        //_renderer.DrawCubeWires(commandList, graphicsDevice.SwapchainFramebuffer.OutputDescription, new Transform() { Translation = new Vector3(0,0,-2) }, Vector3.One, Color.Blue);
        // _cam.End();
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
    }

    private void OnClose() {
        Logger.Warn("Shutting down...");
    }
}