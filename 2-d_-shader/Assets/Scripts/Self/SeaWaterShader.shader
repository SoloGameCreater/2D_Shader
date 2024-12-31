Shader "Custom/WaterSurface"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _HsvShift ("Hue Shift", Range(0, 360)) = 0 // Hue Shift
        _WaveAmount ("Wave Amount", Range(0, 25)) = 7 // Wave
        _WaveSpeed ("Wave Speed", Range(0, 25)) = 10 // Wave
        _WaveStrength ("Wave Strength", Range(0, 25)) = 7.5 // Wave
        _WaveX ("Wave X Axis", Range(0, 1)) = 0 // Wave
        _WaveY ("Wave Y Axis", Range(0, 1)) = 0.5 // Wave
        _TextureScrollXSpeed ("Speed X Axis", Range(-5, 5)) = 1 // Texture Scroll
        _TextureScrollYSpeed ("Speed Y Axis", Range(-5, 5)) = 0 // Texture Scroll
        _DistortTex ("Distortion Texture", 2D) = "white" {} // Distortion
        _DistortAmount ("Distortion Amount", Range(0,2)) = 0.5 // Distortion
        _DistortTexXSpeed ("Scroll speed X", Range(-50,50)) = 5 // Distortion
        _DistortTexYSpeed ("Scroll speed Y", Range(-50,50)) = 5 // Distortion
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _DistortTex;
            float4 _MainTex_ST, _DistortTex_ST;
            float4 _Color;
            float _HsvShift;
            float _WaveAmount, _WaveSpeed, _WaveStrength, _WaveX, _WaveY;
            float _TextureScrollXSpeed, _TextureScrollYSpeed;
            float _DistortAmount, _DistortTexXSpeed, _DistortTexYSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvDistTex : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvDistTex = TRANSFORM_TEX(v.uv, _DistortTex);
                return o;
            }

            float3 ShiftHue(float3 color, float hueShift)
            {
                float3 RGB = color;
                float C = max(RGB.r, max(RGB.g, RGB.b));
                float S = C != 0 ? (C - min(RGB.r, min(RGB.g, RGB.b))) / C : 0;
                float H = 0;
                if (C == RGB.r)
                    H = (RGB.g - RGB.b) / (C - min(RGB.r, min(RGB.g, RGB.b)));
                else if (C == RGB.g)
                    H = 2 + (RGB.b - RGB.r) / (C - min(RGB.r, min(RGB.g, RGB.b)));
                else
                    H = 4 + (RGB.r - RGB.g) / (C - min(RGB.r, min(RGB.g, RGB.b)));
                H /= 6;
                if (H < 0) H += 1;

                H += (hueShift / 360.0);
                if (H > 1) H -= 1;

                uint i = floor(H * 6);
                float f = H * 6 - i;
                float p = C * (1 - S);
                float q = C * (1 - f * S);
                float t = C * (1 - (1 - f) * S);

                switch(i % 6)
                {
                    case 0: return float3(C, t, p);
                    case 1: return float3(q, C, p);
                    case 2: return float3(p, C, t);
                    case 3: return float3(p, q, C);
                    case 4: return float3(t, p, C);
                    case 5: return float3(C, p, q);
                }
                return float3(0,0,0); // This should never happen due to modulo operation
            }

            float4 frag (v2f i) : SV_Target
            {
                // Texture Scroll
                i.uv.x += (_Time.y * _TextureScrollXSpeed) % 1;
                i.uv.y += (_Time.y * _TextureScrollYSpeed) % 1;

                // Wave
                float2 uvWave = float2(_WaveX, _WaveY) - i.uv;
                float angWave = (sqrt(dot(uvWave, uvWave)) * _WaveAmount) - (_Time.y * _WaveSpeed);
                i.uv += normalize(uvWave) * sin(angWave) * (_WaveStrength / 1000.0);

                // Distortion
                i.uvDistTex.x += (_Time.y * _DistortTexXSpeed) % 1;
                i.uvDistTex.y += (_Time.y * _DistortTexYSpeed) % 1;
                float distortAmount = (tex2D(_DistortTex, i.uvDistTex).r - 0.5) * 0.2 * _DistortAmount;
                i.uv.x += distortAmount;
                i.uv.y += distortAmount;

                // Sample the texture
                float4 col = tex2D(_MainTex, i.uv);

                // Apply Hue Shift
                col.rgb = ShiftHue(col.rgb, _HsvShift);

                // Apply Color
                col *= _Color;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}