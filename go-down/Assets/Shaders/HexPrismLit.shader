Shader "GoDown/HexPrismLit"
{
    // 自带光照的六棱柱着色器：光照方向固定在世界空间，配合代码生成的带斜切边(bevel)的
    // 六棱柱网格。六边形绕 Z 轴旋转时，斜切边的世界空间法线随之转动，高光沿边扫过，
    // 呈现“带厚度的立体六边形、旋转时光影保持正确”的效果。
    // 兼容 URP 2D Renderer（Pass 用 Universal2D，光照在片元里自行计算，不依赖管线光照 Pass）。
    Properties
    {
        _BaseColor   ("Base Color", Color)      = (1, 0.78, 0.2, 1)
        _LightDir    ("Light Dir (World)", Vector) = (0.35, 0.65, -0.7, 0)
        _LightColor  ("Light Color", Color)     = (1, 1, 1, 1)
        _Ambient     ("Ambient", Range(0,1))    = 0.45
        _SpecPower   ("Spec Power", Range(1,128))   = 24
        _SpecStrength("Spec Strength", Range(0,2))  = 0.5
        _RimColor    ("Rim Color", Color)       = (1, 1, 1, 1)
        _RimPower    ("Rim Power", Range(0.5,8))    = 3
        _RimStrength ("Rim Strength", Range(0,2))   = 0.25
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "HexPrism"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex HexVertex
            #pragma fragment HexFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _BaseColor;
            float4 _LightDir;
            float4 _LightColor;
            float  _Ambient;
            float  _SpecPower;
            float  _SpecStrength;
            float4 _RimColor;
            float  _RimPower;
            float  _RimStrength;

            Varyings HexVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                OUT.positionCS = pos.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 HexFragment(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(_LightDir.xyz);
                // 正交 2D 相机沿 +Z 看，朝相机的视线方向为 -Z
                float3 V = float3(0, 0, -1);
                float3 H = normalize(L + V);

                float diffuse = saturate(dot(N, L));
                float spec = pow(saturate(dot(N, H)), _SpecPower) * _SpecStrength;
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;

                float3 col = _BaseColor.rgb * (_Ambient + diffuse * _LightColor.rgb);
                col += spec * _LightColor.rgb;
                col += rim * _RimColor.rgb;

                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
