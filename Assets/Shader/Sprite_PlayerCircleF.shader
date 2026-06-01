Shader "Custom/Sprite_PlayerCircleFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _UseFade ("Use Fade", Float) = 0
        _PlayerWorldPos ("Player World Pos", Vector) = (0,0,0,0)
        _FadeRadius ("Fade Radius", Float) = 1.2
        _FadeSoftness ("Fade Softness", Float) = 0.35
        _InnerAlpha ("Inner Alpha", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float _UseFade;
            float4 _PlayerWorldPos;
            float _FadeRadius;
            float _FadeSoftness;
            float _InnerAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;

                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                if (_UseFade > 0.5)
                {
                    float dist = distance(i.worldPos, _PlayerWorldPos.xy);

                    float inner = max(0.001, _FadeRadius - _FadeSoftness);
                    float fadeT = smoothstep(inner, _FadeRadius, dist);

                    float alphaMul = lerp(_InnerAlpha, 1.0, fadeT);

                    col.a *= alphaMul;
                }

                return col;
            }
            ENDCG
        }
    }
}