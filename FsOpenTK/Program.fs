open OpenTK
open OpenTK.Graphics
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.GraphicsLibraryFramework
open System.Diagnostics
open System.Runtime.InteropServices
let debugProc =
  new DebugProc(fun src dtype did sev len mes par -> Debug.WriteLine(Marshal.PtrToStringAnsi(mes,len)) )

type Game(setting , nativeSetting:NativeWindowSettings) =
  inherit GameWindow(setting , nativeSetting)
  do
    GL.Enable(EnableCap.DebugOutput)
    GL.Enable(EnableCap.DebugOutputSynchronous)
    GL.DebugMessageCallback(debugProc , nativeint 0)

  let vertices = [|
     0.5f ; 0.5f; 0.0f; 1.0f;1.0f;
     0.5f; -0.5f; 0.0f; 1.0f;0.0f;
    -0.5f; -0.5f; 0.0f; 0.0f;0.0f;
    -0.5f;  0.5f; 0.0f; 0.0f;1.0f;
  |]
  let indices = [|
    0u; 1u; 3u;
    1u; 2u; 3u;
  |]
  let floatSize cnt = sizeof<float32> * cnt
  let mutable vbo = 0
  let mutable vao = 0
  let mutable ebo = 0
  let mutable frameCnt = 0

  let shader = new Shader.Shader("Shaders/shader.vert", "Shaders/shader.frag")
  let size = nativeSetting.Size
  let postProc = OTLPostProc.testInvert size.X size.Y
  override t.OnLoad() =
    GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f)
    vbo <- GL.GenBuffer()
    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo)
    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof< float32>, vertices , BufferUsageHint.StaticDraw )
    vao <- GL.GenVertexArray()
    GL.BindVertexArray(vao)
    let vloc = shader.GetAttribLoc("aPosition")
    GL.EnableVertexAttribArray(vloc)
    // 最初の3要素が頂点位置
    GL.VertexAttribPointer(vloc , 3 , VertexAttribPointerType.Float , false , floatSize 5  , 0)
    //let texloc = shader.GetAttribLoc("aTexCoord")
    //GL.EnableVertexAttribArray(texloc)
    //// 後半2要素がUV
    //GL.VertexAttribPointer(texloc , 2 , VertexAttribPointerType.Float , false , floatSize 5  , floatSize 3 )

    ebo <- GL.GenBuffer()
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
    GL.BufferData(BufferTarget.ElementArrayBuffer,indices.Length * sizeof<uint32>, indices ,BufferUsageHint.StaticDraw)

    shader.Use()
    base.OnLoad()

  override t.OnResize(e:ResizeEventArgs) =
    GL.Viewport( 0 , 0 , e.Width , e.Height)
    OTLPostProc.onReshape postProc.FBID e.Width e.Height
    base.OnResize(e)

  override t.OnUpdateFrame(e:FrameEventArgs) =
    //let input = Keyboard.GetState()
    base.OnUpdateFrame(e)
  member t.DrawScene() =
    shader.Use()
    GL.BindVertexArray vao
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
    GL.DrawElements(PrimitiveType.Triangles , indices.Length , DrawElementsType.UnsignedInt , 0)
  override t.OnRenderFrame(e:FrameEventArgs) =
    GL.Clear(ClearBufferMask.ColorBufferBit);
    frameCnt <- frameCnt + 1
    if frameCnt < 10 then
      t.DrawScene()
    else
      OTLPostProc.drawPostProc postProc t.DrawScene

    base.SwapBuffers();
    base.OnRenderFrame(e);

  override t.OnUnload() =
    // GL.UseProgram(0)
    base.OnUnload()


[<EntryPoint>]
let main argv =
  let setting = new GameWindowSettings()
  let nativeSetting = new NativeWindowSettings()
  nativeSetting.Size <- new Vector2i(800 ,600)

  use game = new Game(setting,nativeSetting)
  game.Run()
  0
