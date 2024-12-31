Shader "Hidden/Shine" {
    Properties {
        _MainTex ("Texture", 2D) = "white" { }
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1) //���ߵ���ɫ
        _ShineLocation ("Shine Location", Range(0, 1)) = 0.5 //���ߵ�λ��
        _ShineRotate ("Rotate Angle(radians)", Range(0, 6.2831)) = 0 //2�� 360��
        _ShineWidth ("Shine Width", Range(0.05, 1)) = 0.1 //���߿���
        _ShineGlow ("Shine Glow", Range(0, 100)) = 1 //��������

    }
    SubShader {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half4 _ShineColor;
            half _ShineLocation, _ShineRotate, _ShineWidth, _ShineGlow;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                half2 uvShine = i.uv;
				half cosAngle = cos(_ShineRotate);//�ֱ���������ת�Ƕ�_ShineRotate������ֵ
				half sinAngle = sin(_ShineRotate);//�ֱ���������ת�Ƕ�_ShineRotate������ֵ
				half2x2 rot = half2x2(cosAngle, -sinAngle, sinAngle, cosAngle);//����һ����ά��ת����rot����ת��������
				uvShine -= half2(0.5, 0.5);//�������������ƽ�ƣ�ʹ��������������Ϊԭ�� Ҫ��(0.5,0.5)�ĵط����(0,0)
				uvShine = mul(rot, uvShine);//���������갴����ת����rot������ת
				uvShine += half2(0.5, 0.5);//����ת�����������ָ���ԭ����λ��
				half currentDistanceProjection = (uvShine.x + uvShine.y) / 2;//���㵱ǰ�����ڹ���ͶӰ�����ϵľ���ͶӰ
				half whitePower = 1 - (abs(currentDistanceProjection - _ShineLocation) / _ShineWidth);//���㵱ǰ���ص����ȣ�����ģ����ߵ�ǿ��
				col.rgb +=  col.a * whitePower * _ShineGlow * max(sign(currentDistanceProjection - (_ShineLocation - _ShineWidth)), 0.0)
				* max(sign((_ShineLocation + _ShineWidth) - currentDistanceProjection), 0.0) * _ShineColor;//��������յ���ɫ

                return col;
            }
            ENDCG
        }
    }
}