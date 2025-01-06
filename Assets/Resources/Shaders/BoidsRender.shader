Shader "Custom/BoidsRender"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma instancing_options procedural:setup

        struct BoidData
        {
            float3 position;
            float3 velocity;
        };

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<BoidData> _BoidDataBuffer;
        #endif

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _BoidObjectScale;

        float4x4 eulerAnglesToRotationMatrix(float3 angles)
        {
			float ch = cos(angles.y); float sh = sin(angles.y); // heading
			float ca = cos(angles.z); float sa = sin(angles.z); // attitude
			float cb = cos(angles.x); float sb = sin(angles.x); // bank

			// Ry-Rx-Rz (Yaw Pitch Roll)
			return float4x4(
				ch * ca + sh * sb * sa, -ch * sa + sh * sb * ca, sh * cb, 0,
				cb * sa, cb * ca, -sb, 0,
				-sh * ca + ch * sb * sa, sh * sa + ch * sb * ca, ch * cb, 0,
				0, 0, 0, 1
			);
        }

        void vert(inout appdata_full v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            BoidData boidData = _BoidDataBuffer[unity_InstanceID];
            float3 pos = boidData.position.xyz;
            float3 scale = _BoidObjectScale;
            
            float4x4 object2World = (float4x4)0;
            object2World._11_22_33_44 = float4(scale.xyz, 1.0);
            float rotY = atan2(boidData.velocity.x, boidData.velocity.z);
            float rotX = -asin(boidData.velocity.y / (length(boidData.velocity.xyz) + 1e-8));
            float4x4 rotMat = eulerAnglesToRotationMatrix(float3(rotX, rotY, 0));
            object2World = mul(rotMat, object2World);
            object2World._14_24_34 += pos.xyz;
            v.vertex = mul(object2World, v.vertex);
            v.normal = normalize(mul(object2World, v.normal));
            #endif
        }

        void setup()
        {
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
