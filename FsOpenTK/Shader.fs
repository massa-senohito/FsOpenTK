module Shader
open System.IO
open System.Diagnostics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics

type Shader(vPath: string , fPath :string) =
  let compileShader (shader:int) =
    GL.CompileShader shader
    let code = ref 0
    GL.GetShader(shader,ShaderParameter.CompileStatus , code)
    if enum<All> code.Value <> All.True then
      let infoLog = GL.GetShaderInfoLog(shader)
      raise <|exn("Error Compile Shader :" + infoLog)
  let linkProgram (program : int) =
    GL.LinkProgram(program)
    let code = ref 0
    GL.GetProgram(program, GetProgramParameterName.LinkStatus, code)
    if enum<All> code.Value <> All.True then
      let infoLog = GL.GetProgramInfoLog(program)
      raise <|exn("Error LinkShader :" + infoLog)

  let vSrc = File.ReadAllText vPath
  let vShader = GL.CreateShader(ShaderType.VertexShader)
  let fSrc = File.ReadAllText fPath
  let fShader = GL.CreateShader(ShaderType.FragmentShader)
  let mutable handle = 0
  let mutable unifLoc = None
  do
    GL.ShaderSource(vShader , vSrc)
    compileShader vShader
    GL.ShaderSource(fShader , fSrc)
    compileShader fShader
    handle <- GL.CreateProgram()
    GL.AttachShader(handle , vShader)
    GL.AttachShader(handle , fShader)
    linkProgram handle
    GL.DetachShader(handle , vShader)
    GL.DetachShader(handle , fShader)
    GL.DeleteShader(fShader)
    GL.DeleteShader(vShader)
    GL.ValidateProgram(handle)
    let validate = ref 0
    GL.GetProgram(handle , GetProgramParameterName.ValidateStatus , validate)

    let numUnif = ref 0
    GL.GetProgram(handle , GetProgramParameterName.ActiveUniforms , numUnif)
    let kv =
      [for i in [0..numUnif.Value - 1] ->
        let typ = ref 0
        let actUnif = ref ActiveUniformType.Int
        let key = GL.GetActiveUniform(handle , i , typ , actUnif)
        let loc = GL.GetUniformLocation(handle , key)
        key,loc] |> Map.ofList

    unifLoc <- Some kv
    GL.GetProgram(handle , GetProgramParameterName.ActiveAttributes, numUnif)
    let kv =
      [for i in [0..numUnif.Value - 1] ->
        let typ = ref 0
        let actUnif = ref ActiveAttribType.FloatVec2
        let key = GL.GetActiveAttrib(handle , i , typ , actUnif)
        let loc = GL.GetAttribLocation(handle , key)
        key,loc] |> Map.ofList
    ()

  member t.Use() =
    GL.UseProgram handle
  member t.UnifLoc = unifLoc.Value
  member t.GetAttribLoc name =
    GL.GetAttribLocation( handle , name )
  member t.SetV3 name (v:Vector3) =
    GL.Uniform3(t.UnifLoc.[name] , v)
  member t.SetInt name (i:int) =
    GL.Uniform1(t.UnifLoc.[name] , i)

