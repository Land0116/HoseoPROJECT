Shader "Custom/Sprite_MultiCircleFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _UseFade ("Use Fade", Float) = 0
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

            #define MAX_FADE_TARGETS 16

            sampler2D _MainTex;
            fixed4 _Color;

            float _UseFade;
            float _FadeRadius;
            float _FadeSoftness;
            float _InnerAlpha;

            int _FadeTargetCount;
            float4 _FadeTargetPositions[MAX_FADE_TARGETS];

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

                if (_UseFade > 0.5 && _FadeTargetCount > 0)
                {
                    float finalAlphaMul = 1.0;

                    float inner = max(0.001, _FadeRadius - _FadeSoftness);

                    for (int index = 0; index < MAX_FADE_TARGETS; index++)
                    {
                        if (index >= _FadeTargetCount)
                            break;

                        float2 targetPos = _FadeTargetPositions[index].xy;
                        float dist = distance(i.worldPos, targetPos);

                        float fadeT = smoothstep(inner, _FadeRadius, dist);

                        // 중심부는 _InnerAlpha, 바깥은 1
                        float alphaMul = lerp(_InnerAlpha, 1.0, fadeT);

                        // 여러 원 중 가장 많이 투명해지는 값 사용
                        finalAlphaMul = min(finalAlphaMul, alphaMul);
                    }

                    col.a *= finalAlphaMul;
                }

                return col;
            }
            ENDCG
        }
    }
}