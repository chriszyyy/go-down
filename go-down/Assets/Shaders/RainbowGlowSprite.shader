Shader "GoDown/RainbowGlowSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        // Per-instance (MaterialPropertyBlock)
        _HueOffset("Hue Offset", Range(0,1)) = 0
        _Speed("Speed", Float) = 1.2
        _Scale("Scale", Float) = 2.5
        _WaveFreq("Wave Freq", Float) = 10
        _WaveAmp("Wave Amp", Float) = 0.15
        _Glow("Glow", Float) = 1.6
        _Additive("Additive", Float) = 0.9
        _PulseSpeed("Pulse Speed", Float) = 3.0
        // 0 = gradient runs along UV.y (vertical in UV space). 1 = swap axes (gradient along UV.x).
        // Useful when the renderer is rotated so the visual length axis differs from UV.y.
        _AxisSwap("Axis Swap (0=Y, 1=X)", Float) = 0

        // Legacy sprite properties (kept for compatibility)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Rainbow"
            Tags { "LightMode"="Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment RainbowFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            float _HueOffset;
            float _Speed;
            float _Scale;
            float _WaveFreq;
            float _WaveAmp;
            float _Glow;
            float _PulseSpeed;
            float _AxisSwap;

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            half4 RainbowFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float alpha = mainTex.a;

                // _AxisSwap=0 → gradient axis = uv.y, wave axis = uv.x.
                // _AxisSwap=1 → gradient axis = uv.x, wave axis = uv.y (use when renderer is rotated 90°).
                float gradAxis = lerp(i.uv.y, i.uv.x, _AxisSwap);
                float waveAxis = lerp(i.uv.x, i.uv.y, _AxisSwap);

                float t = (_Time.y * _Speed) + (gradAxis * _Scale);
                t += sin((waveAxis * _WaveFreq) + (_Time.y * _Speed * 2.0)) * _WaveAmp;
                t = frac(t + _HueOffset);

                float pulse = 0.65 + 0.35 * sin((_Time.y * _PulseSpeed) + (i.uv.x + i.uv.y) * 6.2831);

                float3 grad = HsvToRgb(float3(t, 1.0, 1.0));
                float3 rgb = grad * (_Glow * pulse);

                // Respect sprite mask/shape via alpha; keep RGB independent from palette tint.
                return half4(rgb, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "RainbowAdd"
            Tags { "LightMode"="Universal2D" }

            Blend One One
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment RainbowAddFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            float _HueOffset;
            float _Speed;
            float _Scale;
            float _WaveFreq;
            float _WaveAmp;
            float _Additive;
            float _PulseSpeed;
            float _AxisSwap;

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            half4 RainbowAddFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float alpha = mainTex.a;

                float gradAxis = lerp(i.uv.y, i.uv.x, _AxisSwap);
                float waveAxis = lerp(i.uv.x, i.uv.y, _AxisSwap);

                float t = (_Time.y * _Speed) + (gradAxis * _Scale);
                t += sin((waveAxis * _WaveFreq) + (_Time.y * _Speed * 2.0)) * _WaveAmp;
                t = frac(t + _HueOffset);

                float pulse = 0.65 + 0.35 * sin((_Time.y * _PulseSpeed) + (i.uv.x + i.uv.y) * 6.2831);

                float3 grad = HsvToRgb(float3(t, 1.0, 1.0));
                float3 rgb = grad * (_Additive * pulse) * alpha;

                return half4(rgb, 1.0);
            }
            ENDHLSL
        }

        // Forward pass for non-2D renderers / previews.
        Pass
        {
            Name "RainbowForward"
            Tags { "LightMode"="UniversalForward" "Queue"="Transparent" "RenderType"="Transparent" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment RainbowFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            float _HueOffset;
            float _Speed;
            float _Scale;
            float _WaveFreq;
            float _WaveAmp;
            float _Glow;
            float _PulseSpeed;
            float _AxisSwap;

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            half4 RainbowFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float alpha = mainTex.a;

                float gradAxis = lerp(i.uv.y, i.uv.x, _AxisSwap);
                float waveAxis = lerp(i.uv.x, i.uv.y, _AxisSwap);

                float t = (_Time.y * _Speed) + (gradAxis * _Scale);
                t += sin((waveAxis * _WaveFreq) + (_Time.y * _Speed * 2.0)) * _WaveAmp;
                t = frac(t + _HueOffset);

                float pulse = 0.65 + 0.35 * sin((_Time.y * _PulseSpeed) + (i.uv.x + i.uv.y) * 6.2831);

                float3 grad = HsvToRgb(float3(t, 1.0, 1.0));
                float3 rgb = grad * (_Glow * pulse);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
