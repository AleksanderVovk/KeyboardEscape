Shader "Custom/URP/DynamicTiling_URP_Mobile_Ambient_Normal"
{
    Properties
    {
        _MainTex ("Albedo (RGB) + Alpha", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1.0

        _OffsetX ("Offset X", Float) = 0.0
        _OffsetY ("Offset Y", Float) = 0.0
        _Scale ("Tiling Scale", Range(0.01, 50)) = 1.0

        _Metallic ("Metallic (approx)", Range(0,1)) = 0.0
        _Smoothness ("Smoothness (0..1)", Range(0,1)) = 0.5

        _Color ("Color Tint", Color) = (1,1,1,1)
        _AmbientIntensity ("Ambient Light Intensity", Range(0,2)) = 1.0
        _NormalStrength ("Normal Influence", Range(0,1)) = 1.0

        _IsDynamic("Is Dynamic", Float) = 0
        _ObjectScale("Object Scale", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _BumpScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _OffsetX)
                UNITY_DEFINE_INSTANCED_PROP(float, _OffsetY)
                UNITY_DEFINE_INSTANCED_PROP(float, _Scale)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _AmbientIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _IsDynamic)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ObjectScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBitangent : TEXCOORD3;
                float3 viewDir     : TEXCOORD4;
                float3 localPos    : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 UnpackNormalFast(float4 nmap)
            {
                float3 n;
                n.xy = nmap.xy * 2 - 1;
                n.z = sqrt(saturate(1 - dot(n.xy, n.xy)));
                return n;
            }

            void TriplanarUVs(float3 pos, float3 normal,
                              out float2 uvX, out float2 uvY, out float2 uvZ,
                              out float3 blend)
            {
                float offsetX = UNITY_ACCESS_INSTANCED_PROP(Props, _OffsetX);
                float offsetY = UNITY_ACCESS_INSTANCED_PROP(Props, _OffsetY);
                float scale = UNITY_ACCESS_INSTANCED_PROP(Props, _Scale);

                float2 offset = float2(offsetX, offsetY);

                uvX = pos.yz * scale + offset;
                uvY = pos.xz * scale + offset;
                uvZ = pos.xy * scale + offset;

                float3 w = abs(normal);
                blend = w / max(w.x + w.y + w.z, 1e-6);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.worldPos = worldPos;
                o.positionHCS = TransformWorldToHClip(worldPos);

                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                o.worldTangent = TransformObjectToWorldDir(v.tangentOS.xyz);

                float3 bitangent = cross(o.worldNormal, o.worldTangent) * v.tangentOS.w;
                o.worldBitangent = bitangent;

                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                float3 objScale = UNITY_ACCESS_INSTANCED_PROP(Props, _ObjectScale).xyz;
                o.localPos = v.positionOS.xyz * objScale;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float isDynamic = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _IsDynamic));
                float3 posForUV = lerp(i.worldPos, i.localPos, isDynamic);

                float2 uvX, uvY, uvZ;
                float3 blend;
                TriplanarUVs(posForUV, i.worldNormal, uvX, uvY, uvZ, blend);

                float4 aX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                float4 aY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                float4 aZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                float3 albedo =
                    (aX.rgb * blend.x +
                     aY.rgb * blend.y +
                     aZ.rgb * blend.z) * tint.rgb;

                float alpha = aX.a * tint.a;

                float4 nX = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX);
                float4 nY = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY);
                float4 nZ = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ);

                float3 nsX = UnpackNormalFast(nX) * blend.x;
                float3 nsY = UnpackNormalFast(nY) * blend.y;
                float3 nsZ = UnpackNormalFast(nZ) * blend.z;

                float3 normalWS =
                    normalize(
                        float3(nsX.z, nsX.x, nsX.y) +
                        float3(nsY.x, nsY.z, nsY.y) +
                        float3(nsZ.x, nsZ.y, nsZ.z)
                    );

                float bumpScale = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _BumpScale));
                normalWS = normalize(lerp(i.worldNormal, normalWS, bumpScale));

                float normalStrength = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _NormalStrength));
                float ambientIntensity = UNITY_ACCESS_INSTANCED_PROP(Props, _AmbientIntensity);

                float nFactor = 0.5 + 0.5 * normalWS.y;
                nFactor = lerp(1.0, nFactor, normalStrength);

                float3 col = albedo * ambientIntensity * nFactor;

                return half4(col, alpha);
            }

            ENDHLSL
        }
    }
}