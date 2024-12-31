Shader "Custom/RainShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.5,0.5,0.5,1)
        _RainSpeed ("Rain Speed", Range(0.1, 2.0)) = 1.0
        _RainDensity ("Rain Density", Range(100, 1000)) = 400
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _RainSpeed;
            int _RainDensity;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Create a simple rain effect by modifying UV coordinates
                // Simulate falling rain with moving lines
                float timeFactor = _Time.y * _RainSpeed;
                float rainPattern = fmod((i.uv.x + i.uv.y + timeFactor) * _RainDensity, 1.0);

                // Create a rain drop pattern
                float rainStrength = step(0.95, rainPattern);

                // Mix color based on rain strength
                return lerp(_Color, fixed4(1, 1, 1, 1), rainStrength);
            }
            ENDCG
        }
    }
}
