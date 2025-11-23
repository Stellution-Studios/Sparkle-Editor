using System.Diagnostics;
using System.Numerics;
using System.Xml;
using Assimp;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Bliss.CSharp.Windowing.Events;
using ImGuiNET;
using MiniAudioEx;
using Sparkle.CSharp;
using Sparkle.Editor.CSharp.EditorRuntime.Utilities;
using Veldrid;
using Veldrid.OpenGL;

namespace Sparkle.Editor.EditorRuntime;

public class EditorInstance
{
    private IWindow _window;
    
    public GraphicsDevice GraphicsDevice;
    public CommandList CommandList;
    private ImmediateRenderer _renderer;
    private ImGuiController _controller;
    public EditorInstance()
    {
        Setup();
        Run();
    }
    
    public void Setup()
    { 
        Logger.Info("Loading...");
        
        GraphicsDeviceOptions options = new GraphicsDeviceOptions()
        {
            Debug = true,
            HasMainSwapchain = true,
            SwapchainDepthFormat = PixelFormat.D32FloatS8UInt,
            SyncToVerticalBlank = true,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SwapchainSrgbFormat = false,
        };
        
        _window = Window.CreateWindow(
            WindowType.Sdl3,
            1280,
            720,
            "Sparkle Editor",
            WindowState.Resizable,
            options,
            Window.GetPlatformDefaultBackend(),
            out GraphicsDevice);
        Logger.Info("Window created.");

        _window.Resized += () =>
            OnResize(new Rectangle(_window.GetX(), _window.GetY(), _window.GetWidth(), _window.GetHeight()));
        
        CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
        Logger.Info("CommandList created.");
        
        GlobalResource.Init(GraphicsDevice);
        Logger.Info("Initialized Global Resources.");

        if (_window is Sdl3Window)
        {
            Input.Init(new Sdl3InputContext(_window));
        }
        else
        {
            throw new Exception("Sdl3Window not supported.");
        }
        Logger.Info("Initialized Input.");
        
        _renderer = new ImmediateRenderer(GraphicsDevice);

        _cam = new Cam3D(new Vector3(0,2,-4), new Vector3(0, 0, 0), (float)_window.GetWidth() / _window.GetHeight(),
            Vector3.UnitY, ProjectionType.Perspective, CameraMode.Orbital, 90, 0.1f, 10000.0f);

        _controller = new ImGuiController(GraphicsDevice, GraphicsDevice.SwapchainFramebuffer.OutputDescription,
            _window.GetWidth(), _window.GetHeight());
        Logger.Info("Completed loading.");

    }

    public void OnResize(Rectangle size)
    {
        GraphicsDevice.MainSwapchain.Resize((uint)size.Width, (uint)size.Height);
        _cam.Resize((uint)size.Width, (uint)size.Height);
        _controller.Resize(size.Width, size.Height);
    }

    private Stopwatch deltaTime = Stopwatch.StartNew();
    public void Run()
    {
        while (_window.Exists)
        {
           
            _controller.Update(1.0f/deltaTime.ElapsedTicks);;
          
            _window.PumpEvents();
            Input.Begin();

            if (!_window.Exists)
            {
                break;
            }

            Update();
            Draw(GraphicsDevice, CommandList);
          
            deltaTime.Restart();
        }

       
        OnClose();
    }

    private Cam3D _cam;

    private void Update()
    {
        _cam.Update(1.0f/deltaTime.ElapsedTicks);
    }

    private void Draw(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        ImGui.Begin("TEST");
        ImGui.Text("TEST");
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

    private void OnClose()
    {
        Logger.Warn("Shutting down...");
    }
    
}