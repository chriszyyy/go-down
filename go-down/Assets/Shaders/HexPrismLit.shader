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
        _Ambient     ("Ambient", Range(0,1))    = 0.62
        _HalfLambert ("Half Lambert (soften)", Range(0,1)) = 1
        _EdgeLight   ("Edge Light (bevel frame)", Range(0,1)) = 0.28
        _EdgeWhiten  ("Edge Whiten (bevel toward white)", Range(0,1)) = 0.6
        _FaceDarken  ("Face Darken (flat center)", Range(0,1)) = 0.18
        _SpecPower   ("Spec Power", Range(1,128))   = 24
        _SpecStrength("Spec Strength", Range(0,2))  = 0.35
        _RimColor    ("Rim Color", Color)       = (1, 1, 1, 1)
        _RimPower    ("Rim Power", Range(0.5,8))    = 3
        _RimStrength ("Rim Strength", Range(0,2))   = 0.18
        [Header(Gem)]
        _Gem         ("Gem Enable (0/1)", Range(0,1)) = 0
        _GemFresnel  ("Gem Fresnel (crystal rim)", Range(0,3)) = 1.2
        _GemSparkle  ("Gem Sparkle Strength", Range(0,3)) = 1.4
        _GemSparklePower ("Gem Sparkle Tightness", Range(1,256)) = 90
        _GemDispersion ("Gem Dispersion (rainbow)", Range(0,1)) = 0.35
        _GemTint     ("Gem Inner Brightness", Range(0,1)) = 0.5
        _RainbowSkin ("Rainbow Skin", Range(0,1)) = 0
        _TrimLight   ("Trim Light (highlight strips)", Range(0,2)) = 0.55
        _TrimWhiten  ("Trim Whiten", Range(0,1)) = 0.38
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
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float  trim       : TEXCOORD1;
                float2 posOS      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _BaseColor;
            float4 _LightDir;
            float4 _LightColor;
            float  _Ambient;
            float  _HalfLambert;
            float  _EdgeLight;
            float  _EdgeWhiten;
            float  _FaceDarken;
            float  _SpecPower;
            float  _SpecStrength;
            float4 _RimColor;
            float  _RimPower;
            float  _RimStrength;
            float  _Gem;
            float  _GemFresnel;
            float  _GemSparkle;
            float  _GemSparklePower;
            float  _GemDispersion;
            float  _GemTint;
            float  _RainbowSkin;
            float  _TrimLight;
            float  _TrimWhiten;

            Varyings HexVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                OUT.positionCS = pos.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.trim = IN.color.r;
                OUT.posOS = IN.positionOS.xy;
                return OUT;
            }

            half4 HexFragment(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(_LightDir.xyz);
                // 正交 2D 相机沿 +Z 看，朝相机的视线方向为 -Z
                float3 V = float3(0, 0, -1);
                float3 H = normalize(L + V);

                // 半 Lambert 包裹光照：背光面不再全黑，整体更亮、不“浓”
                float ndl = dot(N, L);
                float diffuse = lerp(saturate(ndl), ndl * 0.5 + 0.5, _HalfLambert);

                float spec = pow(saturate(dot(N, H)), _SpecPower) * _SpecStrength;
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;

                float3 baseCol = _BaseColor.rgb;
                if (_RainbowSkin > 0.5)
                {
                    float hue = frac(IN.posOS.x * 0.35 + IN.posOS.y * 0.62 + 0.62);
                    float3 rainbowCol = float3(
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.00)),
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.33)),
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.66)));
                    baseCol = lerp(baseCol, rainbowCol, 0.88);
                }

                float3 col = baseCol * (_Ambient + diffuse * _LightColor.rgb);

                // edge: 正面 N.xy≈0 -> edge≈0；斜面 N.xy 较大 -> edge≈1
                float edge = saturate(length(N.xy) * 1.4142);
                // 中间平面压暗（edge≈0 处乘 1-_FaceDarken，斜边保持）
                col *= lerp(1.0 - _FaceDarken, 1.0, edge);
                // 斜切边亮边框：边框颜色朝白色提亮，让暗色（红/蓝/紫）也有明显亮边
                float3 edgeCol = lerp(baseCol, float3(1, 1, 1), _EdgeWhiten);
                col += edgeCol * edge * _EdgeLight;

                col += spec * _LightColor.rgb;
                col += rim * _RimColor.rgb;

                // ---------------- 宝石/钻石质感 ----------------
                if (_Gem > 0.5)
                {
                    float ndv = saturate(dot(N, V));
                    // 菲涅尔：边缘透亮、中间略透，水晶质感
                    float fres = pow(1.0 - ndv, _GemFresnel);

                    // 多面闪光：用世界法线生成高频闪烁，随旋转在棱面上走
                    float facet = pow(saturate(dot(N, H)), _GemSparklePower);
                    // 叠加一层偏移高光，制造多个闪点
                    float3 H2 = normalize(L + V + float3(0.35, -0.2, 0.0));
                    facet += pow(saturate(dot(N, H2)), _GemSparklePower) * 0.7;
                    float sparkle = facet * _GemSparkle;

                    // 色散：边缘按朝向分出彩虹（钻石火彩）
                        float hue = frac(N.x * 0.5 + N.y * 0.5 + 0.5);
                    float3 disp = float3(
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.00)),
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.33)),
                        0.5 + 0.5 * cos(6.2831 * (hue + 0.66)));
                    float3 dispCol = lerp(float3(1,1,1), disp, _GemDispersion) * fres;

                    // 菲涅尔透亮边（仅边缘，不冲淡中间底色）+ 尖锐闪光
                    // 不再全局提亮内部，保留深色高饱和
                    col += dispCol * (0.25 + 0.25 * _GemTint) * fres;
                    col += sparkle * _LightColor.rgb;

                    // 高光专用窄面：内环/外环/连接线都是实际小面，用顶点色 r 标记。
                    // 高光偏浅本色而不是纯白，避免生硬。
                    float3 trimCol = lerp(baseCol, float3(1, 1, 1), _TrimWhiten);
                    col += trimCol * saturate(IN.trim) * _TrimLight;
                }

                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
