Shader "Custom/DiffuseOutline3D"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Saturation("Saturation", Float) = 1
        _Contrast("Contrast", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineSize ("Outline Thickness", Float) = 1.5
    }
    SubShader
    {
        Tags
        {
            "Queue"="Geometry+1"
            "RenderType"="Opaque"
        }

        LOD 200

        ZWrite On
        ZTest Less

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;

        fixed4 _Color;
        fixed _Saturation;
        fixed _Contrast;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            //饱和度
            fixed gray = 0.2125 * c.r + 0.7154 * c.g + 0.0721 * c.b;
            fixed3 grayColor = fixed3(gray, gray, gray);
            fixed3 finalColor = lerp(grayColor, c, _Saturation);

            //对比度
            fixed3 avgColor = fixed3(0.5, 0.5, 0.5);
            finalColor = lerp(avgColor, finalColor, _Contrast);

            o.Albedo = finalColor;
            o.Alpha = c.a;
        }
        ENDCG

        Pass
        {
            LOD 200

            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            ZWrite Off
            ZTest Always
            ColorMask Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDCG
        }

        // render outline
        Pass
        {
            LOD 200

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            half _OutlineSize;
            fixed4 _OutlineColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                v.vertex.xyz += v.normal * _OutlineSize;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Mobile/VertexLit"
}