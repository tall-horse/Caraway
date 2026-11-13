// Edge lines for Carved Kingdoms in URP. They are drawn over the finished frame, using the depth and normals textures.
// You get a darker line where a surface turns. If you want, you also get an outline where the depth jumps.
// A Full Screen Pass renderer feature uses this shader. Tools > Carved Kingdoms > Add the look to my URP renderer sets
// it up for you.
// Lines on detail smaller than they can draw, like battlements, timber work or a flag, would flicker as the camera
// moves. They fade where many crowd together, and TAA on the camera smooths the rest.
Shader "Carved Kingdoms/Edges"
{
    Properties
    {
        _Strength ("Edge lines", Range(0, 1)) = 1
        _Outline ("Outline around every piece", Range(0, 1)) = 0
        _Width ("Line width in pixels", Range(0.5, 3)) = 1.2
        _Ink ("Outline colour", Color) = (0.13, 0.1, 0.085, 1)
        _BusyFade ("Fade lines where they crowd", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "CarvedEdges"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float _Strength;
            float _Outline;
            float _Width;
            float4 _Ink;
            float _BusyFade;
            // set by CarvedLook: 1 turns the outline on, and the plain look leaves out every line
            float _CarvedOutline;
            float _CarvedPlain;

            float Eye(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            // The lines at uv: x is the outline, where the depth jumps, y is the edge line, where the surface turns
            // but the depth carries on smoothly.
            float2 Lines(float2 uv, float2 px)
            {
                float z = Eye(uv);
                float zr = Eye(uv + float2(px.x, 0.0));
                float zl = Eye(uv - float2(px.x, 0.0));
                float zu = Eye(uv + float2(0.0, px.y));
                float zd = Eye(uv - float2(0.0, px.y));
                // Outline: the neighbours that are more than a few centimetres nearer than this pixel.
                float limit = 0.1 + 0.0035 * z;
                float jump = max(2.0 * z - zr - zl, 2.0 * z - zu - zd);
                float outline = smoothstep(limit, limit * 1.8, jump);
                // Edge lines: only look towards +x and +y, so each edge is drawn once.
                float3 n = SampleSceneNormals(uv);
                float gap = 0.3 + 0.004 * z;
                float bend = 0.0;
                if (abs(zr - z) < gap) bend = max(bend, 1.0 - dot(n, SampleSceneNormals(uv + float2(px.x, 0.0))));
                if (abs(zu - z) < gap) bend = max(bend, 1.0 - dot(n, SampleSceneNormals(uv + float2(0.0, px.y))));
                float arris = smoothstep(0.18, 0.42, bend) * lerp(0.75, 0.22, smoothstep(40.0, 150.0, z));
                outline *= lerp(1.0, 0.74, smoothstep(50.0, 170.0, z));
                return float2(outline, arris);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                if (_CarvedPlain > 0.5) return half4(col, 1.0);
                float raw = SampleSceneDepth(uv);
            #if UNITY_REVERSED_Z
                if (raw <= 0.0) return half4(col, 1.0);
            #else
                if (raw >= 1.0) return half4(col, 1.0);
            #endif
                float2 px = _Width / _ScreenParams.xy;
                float outlined = max(_Outline, _CarvedOutline);
                float2 here = Lines(uv, px);
                float outline = here.x * outlined;
                float arris = here.y * _Strength;
                if (_BusyFade > 0.0 && max(outline, arris) > 0.0)
                {
                    // Look round a ring a few pixels out. A single line crosses it about twice. Where lines cross it
                    // far more often, the detail is smaller than the lines can draw. Fade them there.
                    float crowd = 0.0;
                    float r = 3.0 * _Width;
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 0.785398;
                        float2 there = Lines(uv + float2(cos(a), sin(a)) * r / _ScreenParams.xy, px);
                        crowd += step(0.35, max(there.x * outlined, there.y * _Strength));
                    }
                    float fade = 1.0 - _BusyFade * smoothstep(2.0, 4.5, crowd);
                    outline *= fade;
                    arris *= fade;
                }
                col = lerp(col, lerp(col * 0.42, _Ink.rgb, 0.35), saturate(arris));
                col = lerp(col, _Ink.rgb, saturate(outline * 0.92));
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
