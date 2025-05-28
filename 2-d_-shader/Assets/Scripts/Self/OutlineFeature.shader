Shader "OutlineFearture"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0,1,1,1)
        _OutlineSize ("Outline Thickness", Range(0,0.005)) = 0.002
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass // outlined renderer drawing pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
                half2 offsets[8] : TEXCOORD1;
            };

            TEXTURE2D_X(_OutlineMask);
            SAMPLER(sampler_linear_clamp_OutlineMask);

            half4 _OutlineColor;
            half _OutlineSize;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                const half aspectRatio = _ScreenParams.x / _ScreenParams.y;
                const half diagonalCo = 0.707;
                output.offsets[0] = half2(-1, aspectRatio) * _OutlineSize * diagonalCo;
                output.offsets[1] = half2(0, aspectRatio) * _OutlineSize;
                output.offsets[2] = half2(1, aspectRatio) * _OutlineSize * diagonalCo;
                output.offsets[3] = half2(-1, 0) * _OutlineSize;

                output.offsets[4] = half2(1, 0) * _OutlineSize;
                output.offsets[5] = half2(-1, -aspectRatio) * _OutlineSize * diagonalCo;
                output.offsets[6] = half2(0, -aspectRatio) * _OutlineSize;
                output.offsets[7] = half2(1, -aspectRatio) * _OutlineSize * diagonalCo;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                const half kernelX[8] = {
                    -1, 0, 1,
                    -2,     2,
                    -1, 0, 1
                };
                const half kernelY[8] = {
                    -1, -2, -1,
                    0,        0,
                    1,  2,   1
                };
                half gx = 0;
                half gy = 0;
                half mask = 0;
                for (int i = 0; i < 8; i++)
                {
                    mask = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, input.uv + input.offsets[i]).a;
                    gx += mask * kernelX[i];
                    gy += mask * kernelY[i];
                }
                const half alpha = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, input.uv ).a;
                half4 col = _OutlineColor;
                col.a = saturate(abs(gx) + abs(gy)) * (1 - alpha);
                //half4 col = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, input.uv);
                return col;
            }
            ENDHLSL
        }
    }
}