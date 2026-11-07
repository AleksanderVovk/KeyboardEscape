Shader "Game/RainbowHue"
{   
    Properties
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
        _HueSpeed ("Hue Speed", Range(-3,3)) = 0.6
        _Saturation ("Saturation", Range(0,2)) = 1
        _Value ("Value", Range(0,2)) = 1
        _UvWarp ("UV Warp", Range(0,0.5)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Instancing variants must be compiled for GPU instancing support [web:78]
            #pragma multi_compile_instancing

            #include "UnityCG.cginc" // built-in helpers [web:75]

            sampler2D _MainTex;

            // Per-material (batched by material)
            float _HueSpeed;
            float _Saturation;
            float _Value;
            float _UvWarp;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Cheap HSV->RGB (branchless)
            float3 HSVtoRGB(float3 hsv)
            {
                float3 p = abs(frac(hsv.xxx + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0);
                float3 rgb = hsv.z * lerp(float3(1,1,1), saturate(p - 1.0), hsv.y);
                return rgb;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                fixed4 tex = tex2D(_MainTex, i.uv);

                // Немного "живости" через UV, но очень дёшево
                float hue = frac(_Time.y * _HueSpeed + (i.uv.x + i.uv.y) * _UvWarp);

                float3 rainbow = HSVtoRGB(float3(hue, _Saturation, _Value));

                fixed3 col = tex.rgb * rainbow; // белая текстура => чистая радуга
                return fixed4(col, tex.a);
            }
            ENDCG
        }
    }
}
