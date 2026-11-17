using Lab5.Core;
using OpenTK.Graphics.OpenGL4;

namespace Lab5.App;

public sealed class SceneRenderer : IDisposable
{
    int program, vao, originalBuffer, resultBuffer, scratchBuffer;
    int originalCount, resultCount, viewLocation, colorLocation;
    bool initialized;
    public void Initialize()
    {
        if (initialized) return;
        const string vertex = """
            #version 330 core
            layout(location=0) in vec2 position;
            uniform vec4 view;
            void main() { gl_Position=vec4((position-view.xy)/view.zw,0,1); }
            """;
        const string fragment = """
            #version 330 core
            uniform vec4 ink;
            out vec4 color;
            void main() { color=ink; }
            """;
        int Compile(ShaderType type, string text)
        {
            int shader = GL.CreateShader(type); GL.ShaderSource(shader, text); GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0) throw new InvalidOperationException(GL.GetShaderInfoLog(shader));
            return shader;
        }
        int vs = Compile(ShaderType.VertexShader, vertex), fs = Compile(ShaderType.FragmentShader, fragment);
        program = GL.CreateProgram(); GL.AttachShader(program, vs); GL.AttachShader(program, fs); GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(program));
        GL.DeleteShader(vs); GL.DeleteShader(fs);
        vao = GL.GenVertexArray(); originalBuffer = GL.GenBuffer(); resultBuffer = GL.GenBuffer(); scratchBuffer = GL.GenBuffer();
        viewLocation = GL.GetUniformLocation(program, "view"); colorLocation = GL.GetUniformLocation(program, "ink");
        initialized = true;
    }
    public void Begin(Vec center, double halfWidth, double halfHeight)
    {
        Initialize();
        GL.ClearColor(1,1,1,1); GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.DepthTest); GL.UseProgram(program); GL.BindVertexArray(vao);
        GL.Uniform4(viewLocation, (float)center.X, (float)center.Y, (float)halfWidth, (float)halfHeight);
    }
    public void RestrictArea(Box area,Vec camera,double scaleX,double scaleY,int width,int height)
    {
        int left=(int)Math.Floor(Math.Clamp(width*.5+(area.Left-camera.X)*scaleX-3,0,width));
        int right=(int)Math.Ceiling(Math.Clamp(width*.5+(area.Right-camera.X)*scaleX+3,0,width));
        int bottom=(int)Math.Floor(Math.Clamp(height*.5+(area.Bottom-camera.Y)*scaleY-3,0,height));
        int top=(int)Math.Ceiling(Math.Clamp(height*.5+(area.Top-camera.Y)*scaleY+3,0,height));
        GL.Enable(EnableCap.ScissorTest);GL.Scissor(left,bottom,right-left,top-bottom);
    }
    public void SetOriginal(Segment[] segments)
    {
        Initialize(); var vertices = Lines(segments);
        Upload(originalBuffer, vertices); originalCount = vertices.Length / 2;
    }
    public void SetResult(IReadOnlyList<Segment> segments)
    {
        var vertices = Lines(segments); Upload(resultBuffer, vertices); resultCount = vertices.Length / 2;
    }
    public void DrawOriginal()
    {
        float gray=originalCount>20000?.82f:.5f;
        DrawBuffer(originalBuffer,originalCount,PrimitiveType.Lines,gray,gray,gray,1);
    }
    public void DrawResult() => DrawBuffer(resultBuffer, resultCount, PrimitiveType.Lines,0,.32f,.85f,1);
    public void Draw(float[] vertices, PrimitiveType primitive, float r, float g, float b, float alpha = 1)
    {
        Upload(scratchBuffer, vertices); DrawBuffer(scratchBuffer, vertices.Length/2, primitive,r,g,b,alpha);
    }
    static void Upload(int buffer, float[] data)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
    }
    void DrawBuffer(int buffer, int count, PrimitiveType primitive, float r,float g,float b,float alpha)
    {
        if (count == 0) return;
        if(alpha<1)GL.Enable(EnableCap.Blend);else GL.Disable(EnableCap.Blend);
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer); GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float,false,2*sizeof(float),0);
        GL.Uniform4(colorLocation,r,g,b,alpha); GL.DrawArrays(primitive,0,count);
    }
    static void Write(float[] vertices, int offset, Segment s)
    {
        vertices[offset]=(float)s.A.X; vertices[offset+1]=(float)s.A.Y;
        vertices[offset+2]=(float)s.B.X; vertices[offset+3]=(float)s.B.Y;
    }
    public static float[] Lines(IReadOnlyList<Segment> segments)
    {
        var result = new float[segments.Count*4];
        for(int i=0;i<segments.Count;i++) Write(result,i*4,segments[i]);
        return result;
    }
    public static float[] Outlines(IEnumerable<Vec[]> rings)
    {
        var result = new List<float>();
        foreach(var ring in rings) for(int i=0;i<ring.Length;i++)
        {
            Vec a=ring[i],b=ring[(i+1)%ring.Length];
            result.Add((float)a.X); result.Add((float)a.Y); result.Add((float)b.X); result.Add((float)b.Y);
        }
        return result.ToArray();
    }
    public void Dispose()
    {
        if (!initialized) return;
        GL.DeleteBuffer(originalBuffer); GL.DeleteBuffer(resultBuffer); GL.DeleteBuffer(scratchBuffer);
        GL.DeleteVertexArray(vao); GL.DeleteProgram(program); initialized=false;
    }
}
