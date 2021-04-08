//
// Weather Maker for Unity
// (c) 2016 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset from a pirate site, please consider buying it from the Unity asset store at https://www.assetstore.unity3d.com/en/#!/content/60955?aid=1011lGnL. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

Shader "WeatherMaker/WeatherMakerSkyBoxScatteringShader"
{
	SubShader
	{
		Tags{ "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
		Cull Off ZWrite Off

		Pass
		{
			CGPROGRAM

			#pragma target 3.5
			#pragma exclude_renderers gles
			#pragma exclude_renderers d3d9
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma glsl_no_auto_normalization
			#pragma multi_compile_instancing
			
			#pragma vertex GetVolumetricData
			#pragma fragment frag

			#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES
			#define WEATHER_MAKER_SKY_BOX_SHADER
			
			#include "WeatherMakerSkyBoxShaderInclude.cginc"
			
			fixed4 frag (wm_volumetric_data i) : SV_Target
			{
				WM_INSTANCE_FRAG(i);
				//i.rayDir = normalize(mul((float3x3)unity_ObjectToWorld, i.vertex));
				return ComputeSkySphereColor(i);
			}

			ENDCG
		}
	}
}
