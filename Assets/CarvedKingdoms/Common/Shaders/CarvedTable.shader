// A walnut table top for the demo scenes. It has planks, seams between them and grain running along x.
// The main light lights it softly.
Shader "Carved Kingdoms/Table"
{
    Properties
    {
        _Light ("Light walnut", Color) = (0.36, 0.23, 0.15, 1)
        _Dark ("Dark walnut", Color) = (0.24, 0.15, 0.1, 1)
        _Plank ("Plank width (m)", Float) = 1.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Light;
                float4 _Dark;
                float _Plank;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 w = input.positionWS;
                float plank = floor(w.z / _Plank);
                float seam = smoothstep(0.0, 0.012, abs(frac(w.z / _Plank) - 0.5) * _Plank - 0.54 + 0.012);
                float g = Noise(float2(w.x * 0.6 + plank * 13.0, w.z * 18.0)) * 0.6 + Noise(float2(w.x * 3.0, w.z * 60.0 + plank)) * 0.4;
                float3 walnut = lerp(_Dark.rgb, _Light.rgb, g) * (0.9 + 0.2 * Hash(float2(plank, 3.0)));
                walnut *= lerp(0.55, 1.0, seam);
                Light main = GetMainLight(TransformWorldToShadowCoord(w));
                float3 n = normalize(input.normalWS);
                float lit = saturate(dot(n, main.direction)) * lerp(0.4, 1.0, main.shadowAttenuation);
                return half4(walnut * (main.color * lit + SampleSH(n)), 1.0);
            }
            ENDHLSL
        }
    }
}
