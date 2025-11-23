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
using Box2D;
using ImGuiNET;
using ImGuizmoNET;
using Sparkle.CSharp;
using Sparkle.Editor.CSharp.EditorRuntime.Utilities;
using Veldrid;
using Time = Sparkle.Editor.CSharp.EditorRuntime.Utilities.Time;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Sparkle.Editor.EditorRuntime;

public class EditorInstance {
    private Cam3D _cam;
    private ImGuiController _controller;
    private ImmediateRenderer _renderer;
    public CommandList CommandList;
    

    public GraphicsDevice GraphicsDevice;
   

    private string text = "";
    

    public EditorInstance() {
        Instance = this;
    }

    public static EditorInstance Instance { get; private set; }

    public IWindow Window { get; private set; }

    public void Setup() {
        Logger.Info("Loading...");
        Time.Init();

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
            Vector3.UnitY, ProjectionType.Perspective, CameraMode.Free, 90, 0.1f);
        _cam.MouseSensitivity = 10.0f;

        _controller = new ImGuiController(GraphicsDevice, GraphicsDevice.SwapchainFramebuffer.OutputDescription,
            Window.GetWidth(), Window.GetHeight());
        Logger.Info("Completed loading.");
    }

    public void OnResize(Rectangle size) {
        ImGuizmo.Enable(true);
        ImGuizmo.SetRect(0, 0, size.Width, size.Height);
        GraphicsDevice.MainSwapchain.Resize((uint)size.Width, (uint)size.Height);
        _cam.Resize((uint)size.Width, (uint)size.Height);
        _controller.Resize(size.Width, size.Height);
    }

    public void Run() {
        while (Window.Exists) {
            Time.Update();
            Window.PumpEvents();
    
           
            if (!Window.Exists) break;
            Input.Begin();
            Update();
            Input.End();
            
            
            Draw(GraphicsDevice, CommandList);

       
        }


        OnClose();
    }

    private void Update() {
        _cam.Update(Time.Delta);
        view = _cam.GetView();
        proj = _cam.GetProjection();
        _controller.Update((float)Time.Delta);
    }

    private Matrix4x4 view;
    private Matrix4x4 proj;
    Matrix4x4 mat = Matrix4x4.Identity;
    public bool GetTransformData(Matrix4x4 matrix, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
    {
        // This is the standard System.Numerics method
        // It attempts to extract the components
        return Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
    }
   
    private void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        ImGui.Begin("TEST");
        ImGui.Text("TEST");
        ImGui.InputText("##textbox", ref text, 128);
        ImGui.End();
        
       
         
        
        ImGuizmo.Manipulate(ref view.M11, ref proj.M11, OPERATION.TRANSLATE, MODE.WORLD, ref mat.M11);

       
        commandList.Begin();
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.LightBlue.ToRgbaFloat());
        commandList.ClearDepthStencil(1.0f);

        _controller.Render(graphicsDevice, commandList);

        GetTransformData(mat, out Vector3 translation, out Quaternion rotation, out Vector3 scale);
        
        _cam.Begin();
        _renderer.DrawCubeWires(commandList, graphicsDevice.SwapchainFramebuffer.OutputDescription, new Transform() { Translation = translation, Rotation = rotation, Scale = scale}, Vector3.One, Color.Blue);
        _cam.End();
        
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
    }

    private void OnClose() {
        Logger.Warn("Shutting down...");
    }
}