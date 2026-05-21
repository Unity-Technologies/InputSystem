Shader "Custom/WorldCoord Diffuse" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _BaseScale ("Base Tiling", Vector) = (1,1,1,0)
}

// URP SubShader — tri-planar with Lambert lighting
SubShader {
    Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
    LOD 150

    Pass
    {
        Name "ForwardLit"
        Tags { "LightMode"="UniversalForward" }

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ _ADDITIONAL_LIGHTS
        #pragma multi_compile_fog

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half3 _BaseScale;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS  : SV_POSITION;
            float3 positionWS  : TEXCOORD0;
            float3 normalWS    : TEXCOORD1;
            float  fogFactor   : TEXCOORD2;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = posInputs.positionCS;
            output.positionWS = posInputs.positionWS;
            output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
            output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            half3 normalWS = normalize(input.normalWS);

            half4 texXY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.positionWS.xy * _BaseScale.z);
            half4 texXZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.positionWS.xz * _BaseScale.y);
            half4 texYZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.positionWS.yz * _BaseScale.x);

            half3 blendWeights = abs(half3(
                dot(normalWS, half3(0, 0, 1)),
                dot(normalWS, half3(0, 1, 0)),
                dot(normalWS, half3(1, 0, 0))));
            blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

            half4 tex = texXY * blendWeights.x + texXZ * blendWeights.y + texYZ * blendWeights.z;
            half4 albedo = tex * _Color;

            float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            Light mainLight = GetMainLight(shadowCoord);
            half NdotL = saturate(dot(normalWS, mainLight.direction));
            half3 diffuse = albedo.rgb * mainLight.color * (NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

            half3 ambient = SampleSH(normalWS) * albedo.rgb;
            half3 finalColor = ambient + diffuse;

            finalColor = MixFog(finalColor, input.fogFactor);
            return half4(finalColor, 1.0);
        }
        ENDHLSL
    }

    Pass
    {
        Name "ShadowCaster"
        Tags { "LightMode"="ShadowCaster" }

        ZWrite On
        ZTest LEqual
        ColorMask 0

        HLSLPROGRAM
        #pragma vertex ShadowVert
        #pragma fragment ShadowFrag
        #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        float3 _LightDirection;
        float3 _LightPosition;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings ShadowVert(Attributes input)
        {
            Varyings output;
            float3 posWS    = TransformObjectToWorld(input.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - posWS);
            #else
                float3 lightDir = _LightDirection;
            #endif

            output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
            return output;
        }

        half4 ShadowFrag(Varyings input) : SV_Target
        {
            return 0;
        }
        ENDHLSL
    }
}

// Built-in RP SubShader — surface shader with Lambert (fallback)
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150

CGPROGRAM
#pragma surface surf Lambert

sampler2D _MainTex;

fixed4 _Color;
fixed3 _BaseScale;

struct Input {
    float2 uv_MainTex;
    float3 worldPos;
    float3 worldNormal;

};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 texXY = tex2D(_MainTex, IN.worldPos.xy * _BaseScale.z);
    fixed4 texXZ = tex2D(_MainTex, IN.worldPos.xz * _BaseScale.y);
    fixed4 texYZ = tex2D(_MainTex, IN.worldPos.yz * _BaseScale.x);
    fixed3 mask = fixed3(
        dot (IN.worldNormal, fixed3(0,0,1)),
        dot (IN.worldNormal, fixed3(0,1,0)),
        dot (IN.worldNormal, fixed3(1,0,0)));

    fixed4 tex =
        texXY * abs(mask.x) +
        texXZ * abs(mask.y) +
        texYZ * abs(mask.z);
    fixed4 c = tex * _Color;
    o.Albedo = c.rgb;
}
ENDCG
}

FallBack "Diffuse"
}
