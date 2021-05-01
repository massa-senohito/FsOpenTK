module OTLPostProc
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open System.Diagnostics

let tex2d = TextureTarget.Texture2D
let rendBuf = RenderbufferTarget.Renderbuffer
let frameBuf = FramebufferTarget.Framebuffer
let arrayBuf = BufferTarget.ArrayBuffer

let genColorBuf width height=
  //post用テクスチャユニット
  GL.ActiveTexture(TextureUnit.Texture0)
  let tex = GL.GenTexture()
  // OpenGL は操作対象のtexをBindして操作する
  GL.BindTexture( tex2d , tex)
  let linear = ref <| int All.Linear
  let edge = ref <| int All.ClampToEdge
  GL.TexParameterI(tex2d ,TextureParameterName.TextureMagFilter , linear)
  GL.TexParameterI(tex2d ,TextureParameterName.TextureMinFilter , linear)
  GL.TexParameterI(tex2d ,TextureParameterName.TextureWrapS, edge)
  GL.TexParameterI(tex2d ,TextureParameterName.TextureWrapT, edge)
  let pixel = [||]
  GL.TexImage2D(tex2d , 0, PixelInternalFormat.Rgba , width , height ,0 ,PixelFormat.Rgba , PixelType.UnsignedByte , pixel)
  // 操作対象を戻す
  GL.BindTexture(tex2d , 0)
  tex

let genDepthBuf width height =
  let depth = GL.GenRenderbuffer()
  GL.BindRenderbuffer( rendBuf , depth)
  GL.RenderbufferStorage(rendBuf , RenderbufferStorage.DepthComponent16 , width , height)
  GL.BindRenderbuffer( rendBuf , 0)
  depth

type FrameBufferID =
  {
    FBO : int
    RBO : int
    FB  : int
  }
let makeFBID fbo rbo fb = {FBO = fbo ; RBO = rbo ; FB = fb}

let genFrame width height=
  let fbo = genColorBuf width height
  let rbo = genDepthBuf width height
  let buf = [|0|]
  GL.GenFramebuffers(1,buf)
  GL.BindFramebuffer(frameBuf , buf.[0])
  GL.FramebufferTexture2D   (frameBuf , FramebufferAttachment.ColorAttachment0 , tex2d   , fbo , 0 )
  GL.FramebufferRenderbuffer(frameBuf , FramebufferAttachment.DepthAttachment  , rendBuf , rbo)
  let error = GL.CheckFramebufferStatus(frameBuf)
  if error <> FramebufferErrorCode.FramebufferComplete then
    Debug.WriteLine <| nameof(GL.CheckFramebufferStatus) + " " + string error
  GL.BindFramebuffer(frameBuf , 0)
  makeFBID fbo rbo buf.[0]

let onReshape (fbid : FrameBufferID) width height =
  GL.BindTexture(tex2d , fbid.FBO)
  let pixel = [||]
  GL.TexImage2D(tex2d , 0, PixelInternalFormat.Rgba , width , height ,0 ,PixelFormat.Rgba , PixelType.UnsignedByte , pixel)
  GL.BindTexture(tex2d , 0)
  GL.BindRenderbuffer( rendBuf , fbid.RBO)
  GL.RenderbufferStorage(rendBuf , RenderbufferStorage.DepthComponent16 , width , height)
  GL.BindRenderbuffer( rendBuf , 0)

let release (fbid : FrameBufferID) =
  GL.DeleteRenderbuffer(fbid.RBO)
  GL.DeleteTexture(fbid.FBO)
  GL.DeleteFramebuffer(fbid.FB)

let inline arraySize<'a>(a:'a array) = sizeof<'a> * a.Length

let genPostRect () =
  let fboVerts =
    [|
     1.0f; 1.0f;
     1.0f;-1.0f;
    -1.0f;-1.0f;
    -1.0f; 1.0f;
    |]
  let vbo = GL.GenBuffer()
  GL.BindBuffer(arrayBuf , vbo)
  GL.BufferData(arrayBuf , arraySize fboVerts , fboVerts , BufferUsageHint.StaticDraw)
  GL.BindBuffer(arrayBuf , 0)
  let indices =
    [|
      0u; 1u; 3u;
      1u; 2u; 3u;
    |]
  let ebo = GL.GenBuffer()
  let elemArrayBuf = BufferTarget.ElementArrayBuffer
  GL.BindBuffer( elemArrayBuf , ebo)
  GL.BufferData( elemArrayBuf , arraySize indices , indices , BufferUsageHint.StaticDraw)
  vbo,ebo

type PostInfo =
  {
    FBID : FrameBufferID
    Shader : Shader.Shader
    BO    : int * int
  }
  member t.VAO = fst t.BO
  member t.EBO = snd t.BO
let makePostInfo fbid shader bo =
  {
    FBID = fbid ; Shader = shader ; BO = bo
  }
let tryBind (fbid:int) =
  try
    GL.BindFramebuffer(frameBuf, fbid)
    let err = GL.GetError()
    if err <> ErrorCode.NoError then
      raise <| exn (string err)
  with e->Debug.WriteLine <| "bindError " + (string fbid)
let mutable cnt = 0

let drawPostProc(info : PostInfo) onDraw =
  OTLLogger.debugLog "drawPostProc" "PostProc"
  let fbid = info.FBID
  let shader = info.Shader
  cnt <- cnt + 1
  //if cnt > 10 then
  tryBind fbid.FBO
  onDraw()
  tryBind 0
  shader.Use()
  GL.BindTexture(tex2d , fbid.FBO)
  shader.SetInt "tex" 0
  let attrib = shader.GetAttribLoc "v_coord"
  GL.EnableVertexAttribArray(attrib)
  GL.BindBuffer(arrayBuf , info.VAO)
  GL.VertexAttribPointer(attrib , 2 , VertexAttribPointerType.Float , false , 0 , nativeint 0)
  GL.BindBuffer(BufferTarget.ElementArrayBuffer, info.EBO)
  GL.DrawElements(PrimitiveType.Triangles , 6 ,DrawElementsType.UnsignedInt , 0)
  GL.DisableVertexAttribArray(attrib)
  //GL.

let testInvert width height =
  let fbid = genFrame width height
  let shader = new Shader.Shader("Shaders/post.vert", "Shaders/postInvert.frag")
  let vbo = genPostRect()
  makePostInfo fbid shader vbo
