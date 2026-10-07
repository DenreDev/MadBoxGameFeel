// Unlit sprite shader that ignores the depth buffer, so health bars always draw on top of meshes.
Shader "Game/HealthBarOverlay"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent+100"
			"RenderType" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}

		Cull Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.color = input.color;
				output.uv = input.uv;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
			}
			ENDHLSL
		}
	}
}
