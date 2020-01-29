Shader "ProjectRally/Wireframe"
{

	Properties
	{
		_EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
		_Wireframe("Edge thickness", Range(0.0, 1)) = 0.0025
[Gamma] _Metallic("Metallic", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0.00001, 1)) = 0.5
	}

	SubShader 
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Geometry" "RenderType" = "Opaque" }
		LOD 300

		Pass
		{
			CGPROGRAM

			#pragma target 3.0

			#pragma vertex vertexFunction
			#pragma fragment fragmentFunction
			#include "UnityCG.cginc"
            #include "Lighting.cginc"

			#include "UnityPBSLighting.cginc"

			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#include "AutoLight.cginc"

			// Vector 2 Fragment
			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD3;
				float3 vertexColors : COLOR0;
				SHADOW_COORDS(1)
				float3 uv : TEXCOORD0;
				float3 normal : TEXCOORD2;
			};


			v2f vertexFunction(appdata_full  v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = float3(v.texcoord.xy, 0);
				o.normal = UnityObjectToWorldNormal(v.normal);
				o.vertexColors = v.color.rgb;

				TRANSFER_SHADOW(o)

				return o;
			}

			float _Wireframe;
			fixed4 _EdgeColor;
			float _Metallic;
			float _Smoothness;

			fixed4 fragmentFunction(v2f i) : SV_TARGET
			{
				fixed shadow = SHADOW_ATTENUATION(i);

				float x = i.uv.x;
				float y = i.uv.y;
				float valueX = 1 - (4 * (x - 0.5) * (x - 0.5));
				float valueY = 1 - (4 * (y - 0.5) * (y - 0.5));
				float value = exp2(-1 / _Wireframe * (min(valueX, valueY) * min(valueX, valueY)));

				fixed4 eCol = _EdgeColor;
				float3 col = float3(i.vertexColors[0], i.vertexColors[1], i.vertexColors[2]); //+ (eCol.rgb * value);

				i.normal = normalize(i.normal);
				float3 lightDir = _WorldSpaceLightPos0.xyz;
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

				float3 lightColor = _LightColor0.rgb;

				float3 ambient = ShadeSH9(half4(i.normal, 1));

				float3 specularTint;
				float oneMinusReflectivity;

				col = DiffuseAndSpecularFromMetallic(
					col, _Metallic, specularTint, oneMinusReflectivity
				);


				UnityLight light;
				light.color = lightColor * shadow;
				light.dir = lightDir;
				light.ndotl = DotClamped(i.normal, lightDir);

				UnityIndirect indirectLight;
				indirectLight.diffuse = ambient;
				indirectLight.specular = 0;

				return UNITY_BRDF_PBS(
					col, specularTint,
					oneMinusReflectivity, _Smoothness,
					i.normal, viewDir,
					light, indirectLight
				);
			}

			ENDCG
		}

		Pass
		{
			Tags {"LightMode"="ShadowCaster"}

			CGPROGRAM

			#pragma vertex vertexFunction
			#pragma fragment fragmentFunction
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f 
			{
				V2F_SHADOW_CASTER;
			};

			v2f vertexFunction(appdata_base v)
			{
				v2f o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 fragmentFunction(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
	}

}