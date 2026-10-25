// Bloom da Pista: separa o que é bem claro, borra em duas escalas e soma de volta na imagem.
// Compilado para OpenGL (DesktopGL) com o mgfxc; veja GuitarPlay/Content/Effects/README.md.

#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

Texture2D SpriteTexture;
sampler2D SpriteSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D BloomNear;
sampler2D BloomNearSampler = sampler_state
{
    Texture = <BloomNear>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D BloomFar;
sampler2D BloomFarSampler = sampler_state
{
    Texture = <BloomFar>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

float Threshold = 0.6;
float Knee = 0.25;
float2 TexelStep;          // (1/largura, 0) ou (0, 1/altura) da textura borrada
float NearIntensity = 0.9;
float FarIntensity = 0.7;
float Vignette = 0.35;

struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 Uv : TEXCOORD0;
};

float Luma(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

// Mantém só o que passa do limiar, com um joelho suave para não "serrilhar" o brilho.
float4 BrightPS(PixelInput input) : COLOR0
{
    float3 c = tex2D(SpriteSampler, input.Uv).rgb;
    float l = Luma(c);
    float soft = clamp(l - Threshold + Knee, 0, 2 * Knee);
    soft = soft * soft / (4 * Knee + 0.0001);
    float k = max(soft, l - Threshold) / max(l, 0.0001);
    return float4(c * k, 1);
}

// Gaussiano de 9 amostras feito com 5 leituras bilineares.
float4 BlurPS(PixelInput input) : COLOR0
{
    float3 c = tex2D(SpriteSampler, input.Uv).rgb * 0.2270270270;
    c += tex2D(SpriteSampler, input.Uv + TexelStep * 1.3846153846).rgb * 0.3162162162;
    c += tex2D(SpriteSampler, input.Uv - TexelStep * 1.3846153846).rgb * 0.3162162162;
    c += tex2D(SpriteSampler, input.Uv + TexelStep * 3.2307692308).rgb * 0.0702702703;
    c += tex2D(SpriteSampler, input.Uv - TexelStep * 3.2307692308).rgb * 0.0702702703;
    return float4(c, 1);
}

float4 CombinePS(PixelInput input) : COLOR0
{
    float3 c = tex2D(SpriteSampler, input.Uv).rgb;
    c += tex2D(BloomNearSampler, input.Uv).rgb * NearIntensity;
    c += tex2D(BloomFarSampler, input.Uv).rgb * FarIntensity;
    float2 d = input.Uv - 0.5;
    c *= 1 - Vignette * dot(d, d) * 2;
    return float4(c, 1);
}

technique Bright { pass P0 { PixelShader = compile PS_SHADERMODEL BrightPS(); } };
technique Blur { pass P0 { PixelShader = compile PS_SHADERMODEL BlurPS(); } };
technique Combine { pass P0 { PixelShader = compile PS_SHADERMODEL CombinePS(); } };
