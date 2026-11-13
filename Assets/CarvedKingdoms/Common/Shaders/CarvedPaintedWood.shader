// Painted wood for Carved Kingdoms in URP. The colours are flat and come from the pack's palette. The team colour goes
// where the mesh marks it. Bare wood shows its grain, and the grain also shows faintly under the paint. Each part gets
// a small change of tone. The lighting is soft and wrapped. It uses URP's lights, shadows and ambient light, and also
// its ambient occlusion when the renderer has it.
//
// What the mesh carries:
//   UV0     the face's swatch on the palette
//   UV1     where it is along the wood's grain, in metres / 0.5
//   colour  data, not a colour
//           r is the face's shade of the team colour, halved. At 0 the face keeps its own colour.
//           g marks bare wood. 0.5 is the piece's own wood, 1 is a weapon's.
//           b is a number for each part.
//
// The tiles and buildings also carry painted patterns, like thatch, brick, roof tiles and water. This shader draws
// them. Their data:
//   UV2  where on the pattern, in metres
//   UV3  x picks the pattern and y is its size. A size of 0 means no pattern, just the palette's colour.
//   UV4  r and g of the second colour       UV5  x is its b
//   UV6  r and g of the paint colour        UV7  x is its b
// The colours in UV4 to UV7 are sRGB.
//
// The team colour comes from the material. Per object it can come from CarvedTeam, which sets a MaterialPropertyBlock.
//
// The plain look shows only the palette's colours and patterns, with no grain, no change between parts and no sheen.
// Tick Plain on a material, or let CarvedLook switch every material at once while the game runs.
Shader "Carved Kingdoms/Painted Wood"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Palette", 2D) = "white" {}
        [NoScaleOffset] _Grain ("Grain (r fine, g rings, b pores)", 2D) = "white" {}
        _TeamColor ("Team colour (alpha 0 keeps the neutral colours)", Color) = (0, 0, 0, 0)
        _WoodLight ("Bare wood, light", Color) = (0.8706, 0.7137, 0.5412, 1)
        _WoodDark ("Bare wood, dark", Color) = (0.7059, 0.4941, 0.3294, 1)
        _WoodGrain ("Bare wood's grain (r g b)", Vector) = (1, 0, 0, 0)
        _WeaponWoodLight ("Weapons' wood, light", Color) = (0.8627, 0.7922, 0.651, 1)
        _WeaponWoodDark ("Weapons' wood, dark", Color) = (0.6118, 0.4941, 0.3451, 1)
        _WeaponWoodGrain ("Weapons' wood's grain (r g b)", Vector) = (0, 1, 0, 0)
        _GrainScale ("Grain size (more than 1: finer)", Float) = 1
        _PaintGrain ("Grain under the paint", Range(0, 1)) = 0.09
        _PaintGrainKind ("Grain under the paint (r g b)", Vector) = (0, 0, 1, 0)
        _PaintGrainScale ("Grain spacing under the paint", Float) = 0.5
        _Variation ("Tone between parts", Range(0, 1)) = 0.5
        _Richness ("Deeper colour", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.12
        [Toggle] _Plain ("Plain look (palette colours only)", Float) = 0
        [Toggle] _Patterns ("Painted patterns (tiles and buildings; off for figures, whose models carry no pattern data)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _TeamColor;
            float4 _WoodLight;
            float4 _WoodDark;
            float4 _WoodGrain;
            float4 _WeaponWoodLight;
            float4 _WeaponWoodDark;
            float4 _WeaponWoodGrain;
            float _GrainScale;
            float _PaintGrain;
            float4 _PaintGrainKind;
            float _PaintGrainScale;
            float _Variation;
            float _Richness;
            float _Smoothness;
            float _Patterns;
            float _Plain;
        CBUFFER_END
        // set by CarvedLook for every material at once
        float _CarvedPlain;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_point_clamp);
            TEXTURE2D(_Grain);
            SAMPLER(sampler_Grain);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float2 uv4 : TEXCOORD4;
                float2 uv5 : TEXCOORD5;
                float2 uv6 : TEXCOORD6;
                float2 uv7 : TEXCOORD7;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
                float2 uv1 : TEXCOORD3;
                float4 masks : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float4 pattern : TEXCOORD6;      // xy where, z which pattern, w its size
                float4 second : TEXCOORD7;       // rgb, and a is not used
                float3 paint : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv0 = input.uv0;
                o.uv1 = input.uv1;
                o.masks = input.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.pattern = float4(input.uv2, input.uv3);
                o.second = float4(input.uv4, input.uv5.x, 0.0);
                o.paint = float3(input.uv6, input.uv7.x);
                return o;
            }

            float Hash(float x)
            {
                return frac(sin(x * 127.1 + 31.7) * 43758.5453);
            }

            // The painted patterns.
            float Hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Hash22(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash2(i), Hash2(i + float2(1, 0)), f.x), lerp(Hash2(i + float2(0, 1)), Hash2(i + float2(1, 1)), f.x), f.y);
            }
            float Mod(float x, float y) { return x - y * floor(x / y); }

            // Stripes at t, counted in repeats. `duty` is how much of each repeat the stripe covers. When a repeat
            // gets smaller than two pixels, the stripes fade to the average.
            float Bars(float t, float duty)
            {
                float w = fwidth(t);
                float m = 1.0 - smoothstep(duty * 0.5 - w * 0.75, duty * 0.5 + w * 0.75, abs(frac(t) - 0.5));
                return lerp(m, duty, smoothstep(0.3, 0.7, w));
            }

            float PatternMask(float id, float2 uv, float p)
            {
                float fine = max(fwidth(uv.x), fwidth(uv.y)) / p;
                float far = smoothstep(0.3, 0.7, fine);
                if (id < 0.5) return 0.0;
                if (id < 1.5) return Bars(uv.x / p, 0.5);                      // stripes side by side
                if (id < 2.5) return Bars(uv.y / p, 0.5);                      // stripes one above the other
                if (id < 3.5)                                                  // chequer
                {
                    float a = Bars(uv.x / p, 0.5);
                    float b = Bars(uv.y / p, 0.5);
                    return a + b - 2.0 * a * b;
                }
                if (id < 4.5)                                                  // petal frieze along the lower edge
                {
                    float x = abs(frac(uv.x / p) - 0.5) * 2.0;
                    float top = pow(max(1.0 - x * 1.12, 0.0), 0.55) * p * 1.5;
                    float m = smoothstep(0.0, fwidth(uv.y) * 1.5, top - uv.y) * step(0.0, uv.y);
                    return lerp(m, 0.5, far);
                }
                if (id < 5.5)                                                  // brick courses, in patches
                {
                    float course = floor(uv.y / p);
                    float lines = Bars(uv.y / p + 0.5, 0.12);
                    float joints = Bars((uv.x + Mod(course, 2.0) * p * 1.1) / (p * 2.2) + 0.5, 0.05);
                    float here = smoothstep(0.42, 0.56, Noise(uv * 0.55 + float2(3.7, 1.3)));
                    return max(lines, joints) * here * (1.0 - far) * 0.9;
                }
                if (id < 6.5)                                                  // reed, in strokes of uneven length
                {
                    float lane = floor(uv.x / p);
                    float broken = step(0.35, Noise(float2(lane * 7.3, uv.y * 1.6 / p * 0.2)));
                    return lerp(Bars(uv.x / p, 0.34) * broken, 0.22, far);
                }
                if (id < 7.5)                                                  // water, as zigzags
                {
                    float zig = abs(frac(uv.x / p) - 0.5) * 2.0;
                    return Bars(uv.y / (p * 0.62) + zig * 0.7, 0.3);
                }
                if (id > 13.5) return 0.0;                                     // bare wood, grain added further down
                if (id > 12.5)                                                 // waves, as short arcs here and there
                {
                    float2 g = uv / p;
                    float2 cell = floor(g);
                    float2 d = frac(g) - (Hash22(cell + 3.3) * 0.5 + 0.25);
                    float arc = abs(length(d + float2(0.0, 0.2)) - 0.2);
                    float span = 1.0 - smoothstep(0.1, 0.17, abs(d.x));
                    float m = (1.0 - smoothstep(0.035 - fine, 0.035 + fine, arc)) * span * step(-0.02, d.y) * step(0.3, Hash2(cell + 7.7));
                    return lerp(m, 0.06, far);
                }
                if (id < 8.5 || id > 11.5)                                     // dots (8) and leaf dabs (12)
                {
                    float2 g = uv / p;
                    float2 cell = floor(g);
                    float2 d = frac(g) - (Hash22(cell) * 0.6 + 0.2);
                    if (id > 11.5) d = float2(d.x * 0.7 + d.y * 0.3, d.y * 1.5 - d.x * 0.5);
                    float r = 0.26 * step(0.25, Hash2(cell + 9.1));
                    float m = 1.0 - smoothstep(r - fine, r + fine, length(d));
                    return lerp(m, 0.16, far);
                }
                if (id < 9.5) return max(Bars((uv.x + uv.y) / p, 0.26), Bars((uv.x - uv.y) / p, 0.26));   // basket weave
                if (id < 10.5) return Bars(uv.y / p + 0.5, 0.2);              // thin lines one above the other
                return Bars(uv.x / p + 0.5, 0.2);                              // thin lines side by side
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv0).rgb;
                float4 m = input.masks;
                bool team = m.r > 0.004 && _TeamColor.a > 0.5;
                if (team)
                    colour = _TeamColor.rgb * m.r * 2.0;
                bool plain = max(_Plain, _CarvedPlain) > 0.5;
                // Only draw patterns where the material says the models carry the pattern channels (UV3 to UV7).
                bool patterned = _Patterns > 0.5 && input.pattern.w > 0.0 && input.pattern.z < 13.5 && m.g < 0.25;
                float r1 = Hash(m.b * 61.0);
                float r2 = Hash(m.b * 13.0 + 0.5);
                float smooth = _Smoothness;
                if (plain)
                {
                    // The plain look: the paint and its pattern, and bare wood in one even tone.
                    if (patterned)
                    {
                        float3 base = team ? LinearToSRGB(colour) : input.paint;
                        colour = SRGBToLinear(saturate(lerp(base, input.second.rgb, PatternMask(input.pattern.z, input.pattern.xy, max(input.pattern.w, 0.01)) * 0.85)));
                    }
                    if (m.g > 0.25)
                        colour = m.g > 0.75 ? lerp(_WeaponWoodDark.rgb, _WeaponWoodLight.rgb, 0.6) : lerp(_WoodDark.rgb, _WoodLight.rgb, 0.6);
                }
                else
                {
                    if (patterned)
                    {
                        // The painted pattern, worked in sRGB. Start from the paint or the team's
                        // colour, and lay the pattern's second colour over it. Then take out a little saturation, warm
                        // it a touch and add a faint brush.
                        float3 base = team ? LinearToSRGB(colour) : input.paint;
                        float3 wash = lerp(base, input.second.rgb, PatternMask(input.pattern.z, input.pattern.xy, max(input.pattern.w, 0.01)) * 0.85);
                        float grey = dot(wash, float3(0.299, 0.587, 0.114));
                        wash = saturate(lerp(grey.xxx, wash, 0.92)) * float3(1.02, 1.0, 0.96);
                        float3 w = input.positionWS;
                        wash *= 0.97 + 0.05 * Noise(float2(w.x * 38.0 + w.z * 12.0, w.y * 90.0 + w.z * 30.0));
                        colour = SRGBToLinear(saturate(wash));
                    }
                    if (m.g > 0.25)
                    {
                        bool weapon = m.g > 0.75;
                        // Shift the grain for each part, so neighbouring parts don't share the same grain.
                        float3 g3 = SAMPLE_TEXTURE2D(_Grain, sampler_Grain, input.uv1 * _GrainScale + float2(r1, r2) * 3.0).rgb;
                        float g = dot(g3, weapon ? _WeaponWoodGrain.rgb : _WoodGrain.rgb);
                        colour = lerp(weapon ? _WeaponWoodDark.rgb : _WoodDark.rgb, weapon ? _WeaponWoodLight.rgb : _WoodLight.rgb, g);
                        smooth = _Smoothness * 1.6;
                    }
                    else if (_PaintGrain > 0.0)
                    {
                        // The grain shows faintly through the paint.
                        float2 at = input.uv1 * _GrainScale * _PaintGrainScale + float2(r1, r2) * 3.0;
                        float3 slow = SAMPLE_TEXTURE2D(_Grain, sampler_Grain, at * float2(0.09, 0.13) + float2(0.37, 0.71)).rgb;
                        at.y += 0.6 * (slow.r - 0.5) * 0.6;
                        at.y *= 1.0 + 0.6 * (slow.b - 0.5) * 0.8;
                        float3 g3 = SAMPLE_TEXTURE2D(_Grain, sampler_Grain, at).rgb;
                        colour *= 1.0 - _PaintGrain * (1.0 - dot(g3, _PaintGrainKind.rgb));
                    }
                    colour *= 1.0 + _Variation * 0.16 * (r1 - 0.5);
                    colour *= 1.0 + _Variation * 0.08 * (r2 - 0.5) * float3(1.0, 0.0, -1.0);
                    float luma = dot(colour, float3(0.2126, 0.7152, 0.0722));
                    colour = max(lerp(luma.xxx, colour, 1.0 + _Richness * 0.45) * (1.0 - _Richness * 0.12), 0.0);
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float3 n = inputData.normalWS;
                float3 v = inputData.viewDirectionWS;
                // Soft wrapped light. Shadows lift the shade instead of turning it black. A faint sheen shows at
                // grazing angles. The plain look's light wraps further round, and it has no sheen.
                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float2 wrap = plain ? float2(-0.35, 1.0) : float2(-0.2, 0.95);
                float shade = plain ? 0.55 : 0.42;
                float lit = smoothstep(wrap.x, wrap.y, dot(n, main.direction)) * lerp(shade, 1.0, main.shadowAttenuation * main.distanceAttenuation);
                float3 light = main.color * lit;
                float3 sheen = plain ? float3(0.0, 0.0, 0.0) : main.color * pow(saturate(dot(n, normalize(main.direction + v))), 6.0) * 0.025 * main.shadowAttenuation;
            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light l = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    light += l.color * smoothstep(wrap.x, wrap.y, dot(n, l.direction)) * l.distanceAttenuation * lerp(shade, 1.0, l.shadowAttenuation);
                LIGHT_LOOP_END
            #endif
                float ao = 1.0;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV).indirectAmbientOcclusion;
            #endif
                half4 c = half4(colour * (light + SampleSH(n) * ao) + sheen, 1.0);
                c.rgb = MixFog(c.rgb, input.fogFactor);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                float3 n = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 l = normalize(_LightPosition - p);
            #else
                float3 l = _LightDirection;
            #endif
                float4 c = TransformWorldToHClip(ApplyShadowBias(p, n, l));
            #if UNITY_REVERSED_Z
                c.z = min(c.z, UNITY_NEAR_CLIP_VALUE);
            #else
                c.z = max(c.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return c;
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings NormalsVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 NormalsFrag(Varyings input) : SV_Target
            {
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(normalize(input.normalWS));
                return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
            #else
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    // Outside URP you get the palette's colours with plain lighting, but no team colour or grain.
    FallBack "Diffuse"
}
