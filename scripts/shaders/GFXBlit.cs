using System;
using Godot;

public partial class GFXBlit : IDisposable
{
    public const string ShaderFragSrc = @"
#version 450
layout (location = 0) out vec4 FragColor;
layout (location = 0) in vec2 uv;

layout (binding = 0) uniform sampler2D tex;

void main()
{
    FragColor = texture(tex, uv.xy).rgba;
}
";

    public const string ShaderVertSrc = @"
#version 450
layout (location = 0) in vec3 pos;
layout (location = 0) out vec2 uv;

void main()
{
    gl_Position = vec4(pos.x, pos.y, pos.z, 1.0);
    uv = (gl_Position.xy * 0.5) + 0.5;
}
";

    private static RenderingDevice dev;

    private static long vertexFormat;
    private static Rid vertexBuffer;
    private static Rid vertexArray;

    public string Name { get; private set; }
    public Texture2Drd TempTex => rdTex;
    public Rid RdTextureRid => texture;

    private bool valid;
    private Texture2Drd rdTex;

    private Rid srcRid;
    private Rid texture;
    private Rid sampler;
    private Rid framebuffer;
    private Rid shader;
    private Rid rasterPipeline;

    public GFXBlit(Texture2D src, string name = "")
    {
        Init();
        Name = name;
        valid = GodotObject.IsInstanceValid(dev) && GodotObject.IsInstanceValid(src);
        if (valid)
        {
            srcRid = RenderingServer.TextureGetRdTexture(src.GetRid());
            bool res = CreateTextureSamplerFramebuffer(default) && CreateShaderPipeline();
            if (res)
            {
                rdTex = new Texture2Drd()
                {
                    TextureRdRid = texture,
                };
            }
            else
            {
                Log.PrintErr("Error creating Texture, Sampler, Framebuffer, or Shader Pipeline.");
            }
        }
        else
        {
            Log.PrintErr("Source Texture or Rendering Device is invalid.");
        }
    }

    public GFXBlit(Texture2D src, Rid dst, int slice, string name = "")
    {
        Init();
        Name = name;
        valid = GodotObject.IsInstanceValid(dev) && GodotObject.IsInstanceValid(src);
        if (valid)
        {
            srcRid = RenderingServer.TextureGetRdTexture(src.GetRid());
            bool res = CreateTextureSamplerFramebuffer(dst, slice) && CreateShaderPipeline();
            if (res)
            {
                rdTex = null;
            }
            else
            {
                Log.PrintErr("Error creating Texture, Sampler, Framebuffer, or Shader Pipeline.");
            }
        }
        else
        {
            Log.PrintErr("Source Texture or Rendering Device is invalid.");
        }
    }

    public GFXBlit(Rid src, Rid dst, int slice, string name = "")
    {
        Init();
        Name = name;
        srcRid = src;
        valid = GodotObject.IsInstanceValid(dev) && srcRid.IsValid;
        if (valid)
        {
            bool res = CreateTextureSamplerFramebuffer(dst, slice) && CreateShaderPipeline();
            if (res)
            {
                rdTex = null;
            }
            else
            {
                Log.PrintErr("Error creating Texture, Sampler, Framebuffer, or Shader Pipeline.");
            }
        }
        else
        {
            Log.PrintErr("Source Texture or Rendering Device is invalid.");
        }
    }

    ~GFXBlit()
    {
        Dispose();
    }

    public static void Init()
    {
        if (GodotObject.IsInstanceValid(dev))
            return;
        dev = RenderingServer.GetRenderingDevice();
        if (!GodotObject.IsInstanceValid(dev))
            return;
        if (!CreateMesh())
        {
            DeInit();
            dev = null;
            return;
        }
    }

    public static void DeInit()
    {
        if (!GodotObject.IsInstanceValid(dev))
            return;
        dev.FreeRid(vertexArray);
        dev.FreeRid(vertexBuffer);
    }

    private static bool CreateMesh()
    {
        vertexFormat = dev.VertexFormatCreate(new()
        {
            new RDVertexAttribute()
            {
                Format = RenderingDevice.DataFormat.R32G32B32Sfloat,
                Frequency = RenderingDevice.VertexFrequency.Vertex,
                Location = 0,
                Offset = 0,
                Stride = 3 * sizeof(float),
            },
        });
        if (vertexFormat == RenderingDevice.InvalidId)
        {
            Log.PrintErr("Vertex Format Invalid.");
            return false;
        }
        float[] dataFl = new[]
        {
            -1f, -1f, 0f,
            1f, -1f, 0f,
            1f, 1f, 0f,

            1f, 1f, 0f,
            -1f, 1f, 0f,
            -1f, -1f, 0f,
        };
        byte[] data = new byte[dataFl.Length * sizeof(float)];
        Buffer.BlockCopy(dataFl, 0, data, 0, data.Length);
        vertexBuffer = dev.VertexBufferCreate((uint)data.Length, data);
        if (!vertexBuffer.IsValid)
        {
            Log.PrintErr("Vertex Buffer Invalid.");
            return false;
        }
        vertexArray = dev.VertexArrayCreate((uint)(dataFl.Length / 3), vertexFormat, new() { vertexBuffer });
        if (!vertexArray.IsValid)
        {
            Log.PrintErr("Vertex Array Invalid.");
            return false;
        }
        return true;
    }

    private bool CreateTextureSamplerFramebuffer(Rid rid, int slice = -1)
    {
        var srcFmt = dev.TextureGetFormat(srcRid);
        if (rid.IsValid)
        {
            if (slice == -1)
            {}
            else
            {
                texture = dev.TextureCreateSharedFromSlice(new RDTextureView()
                {
                    FormatOverride = RenderingDevice.DataFormat.Max,
                }, rid, (uint)slice, 0, 1, RenderingDevice.TextureSliceType.Slice2D);
            }
        }
        else
        {
            texture = dev.TextureCreate(new RDTextureFormat()
            {
                Width = srcFmt.Width,
                Height = srcFmt.Height,
                Depth = 1,
                Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
                UsageBits = RenderingDevice.TextureUsageBits.ColorAttachmentBit | RenderingDevice.TextureUsageBits.SamplingBit,
            }, new RDTextureView()
            {
                FormatOverride = RenderingDevice.DataFormat.Max,
            });
        }
        if (!texture.IsValid)
            return false;
        sampler = dev.SamplerCreate(new RDSamplerState());
        if (!sampler.IsValid)
            return false;
        framebuffer = dev.FramebufferCreate(new() { texture });
        if (!framebuffer.IsValid)
            return false;
        return true;
    }

    private bool CreateShaderPipeline()
    {
        var src = new RDShaderSource()
        {
            Language = RenderingDevice.ShaderLanguage.Glsl,
            SourceFragment = ShaderFragSrc,
            SourceVertex = ShaderVertSrc,
        };
        var spriv = dev.ShaderCompileSpirVFromSource(src, false);
        Log.PrintS("Fragment:", spriv.CompileErrorFragment);
        Log.PrintS("Vertex:", spriv.CompileErrorVertex);
        shader = dev.ShaderCreateFromSpirV(spriv, Name);
        if (!shader.IsValid)
        {
            Log.PrintErr("Shader Invalid.");
            return false;
        }

        var format = dev.FramebufferGetFormat(framebuffer);
        if (format == RenderingDevice.InvalidId)
        {
            Log.PrintErr("Screen Framebuffer Format Invalid.");
            return false;
        }
        var pipelineState = new RDPipelineRasterizationState();
        var sampleState = new RDPipelineMultisampleState();
        var stencilState = new RDPipelineDepthStencilState();
        var colorState = new RDPipelineColorBlendState()
        {
            Attachments = new()
            {
                new RDPipelineColorBlendStateAttachment(),
            },
        };
        rasterPipeline = dev.RenderPipelineCreate(shader, format, vertexFormat, RenderingDevice.RenderPrimitive.Triangles, pipelineState, sampleState, stencilState, colorState);
        if (!rasterPipeline.IsValid)
        {
            Log.PrintErr("Render Pipeline Invalid.");
            return false;
        }

        return true;
    }

    private void Draw()
    {
        if (!valid)
            return;
        Rid vpRdRid = srcRid;

        var uniforms = dev.UniformSetCreate(new()
        {
            new RDUniform()
            {
                _Ids = new() { sampler, vpRdRid },
                Binding = 0,
                UniformType = RenderingDevice.UniformType.SamplerWithTexture,
            },
        }, shader, 0);

        var clearColors = new[] { Colors.Black };
        dev.DrawCommandBeginLabel(Name, Color.FromHsv(GD.Randf(), 1f, 0.5f));
        var drawList = dev.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearAll, clearColors);
        if (drawList == RenderingDevice.InvalidId)
        {
            Log.PrintErr("Draw List Invalid.");
            dev.FreeRid(uniforms);
            return;
        }
        dev.DrawListBindRenderPipeline(drawList, rasterPipeline);
        dev.DrawListBindUniformSet(drawList, uniforms, 0);
        dev.DrawListBindVertexArray(drawList, vertexArray);
        dev.DrawListDraw(drawList, false, 1);
        dev.DrawListEnd(RenderingDevice.BarrierMask.Raster);
        dev.DrawCommandEndLabel();

        dev.FreeRid(uniforms);
    }

    public Texture2Drd BlitToTemp()
    {
        Draw();
        return rdTex;
    }

    /// <summary>
    /// As of Godot 4.2.1 and older, this is broken and will not work.
    /// </summary>
    public void BlitToScreen()
    {
        if (!valid || !DisplayServer.WindowCanDraw())
            return;
        Rid vpRdRid = srcRid;

        var uniforms = dev.UniformSetCreate(new()
        {
            new RDUniform()
            {
                _Ids = new() { sampler, vpRdRid },
                Binding = 0,
                UniformType = RenderingDevice.UniformType.SamplerWithTexture,
            },
        }, shader, 0);

        dev.DrawCommandBeginLabel(Name, Color.FromHsv(GD.Randf(), 1f, 0.5f));
        var drawList = dev.DrawListBeginForScreen((int)DisplayServer.MainWindowId, clearColor: Colors.Red);
        if (drawList == RenderingDevice.InvalidId)
        {
            Log.PrintErr("Draw List Invalid.");
            dev.FreeRid(uniforms);
            return;
        }
        dev.DrawListBindRenderPipeline(drawList, rasterPipeline);
        dev.DrawListBindUniformSet(drawList, uniforms, 0);
        dev.DrawListBindVertexArray(drawList, vertexArray);
        dev.DrawListDraw(drawList, false, 1);
        dev.DrawListEnd(RenderingDevice.BarrierMask.Raster);
        dev.DrawCommandEndLabel();

        dev.FreeRid(uniforms);
    }

    public void Dispose()
    {
        if (valid)
        {
            if (rasterPipeline.IsValid)
                dev.FreeRid(rasterPipeline);
            if (shader.IsValid)
                dev.FreeRid(shader);
            if (framebuffer.IsValid)
                dev.FreeRid(framebuffer);
            if (sampler.IsValid)
                dev.FreeRid(sampler);
            if (texture.IsValid)
                dev.FreeRid(texture);
            valid = false;
        }
    }
}
