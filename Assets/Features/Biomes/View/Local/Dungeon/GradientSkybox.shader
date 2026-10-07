// Three-color sky: top, horizon, bottom. Flat and stylized instead of Unity's procedural atmosphere.
// Set the horizon color to the scene fog color and the floor fades into the sky without a visible edge.
Shader "Game/Skybox/Gradient"
{
	Properties
	{
		_TopColor ("Top Color", Color) = (0.3, 0.4, 0.85, 1)
		_HorizonColor ("Horizon Color", Color) = (0.85, 0.72, 0.95, 1)
		_BottomColor ("Bottom Color", Color) = (0.85, 0.72, 0.95, 1)
		_Falloff ("Horizon Falloff", Range(0.1, 4)) = 0.6
	}

	SubShader
	{
		Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _TopColor;
			fixed4 _HorizonColor;
			fixed4 _BottomColor;
			half _Falloff;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 dir : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.dir = v.vertex.xyz;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float height = normalize(i.dir).y;
				fixed3 color = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(height), _Falloff));
				color = lerp(color, _BottomColor.rgb, pow(saturate(-height), _Falloff));
				return fixed4(color, 1);
			}
			ENDCG
		}
	}
}
