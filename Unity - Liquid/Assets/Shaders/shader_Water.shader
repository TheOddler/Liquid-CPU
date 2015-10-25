Shader "Custom/Pablo's Water"
{
	Properties
	{
		_Height("Height", 2D) = "black" {} //R = bedrock, G = Dirt, B = Water

		_WaterTex("Water Texture", 2D) = "white" {}
		_WaterCol("Water Color", Color) = (1, 1, 1, 1)

		_GroundTex("Ground Texture", 2D) = "white" {}
		_GroundCol("Ground Color", Color) = (1, 1, 1, 1)

		_Gloss("Ground Smoothness", Range(0, 1)) = 0.5
		_Metallic("Ground Metallic", Range(0, 1)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _Height;

		sampler2D _WaterTex;
		fixed4 _WaterCol;

		sampler2D _GroundTex;
		fixed4 _GroundCol;

		half _Gloss;
		half _Metallic;

		struct Input {
			float2 uv_MainTex;
		};

		void vert(inout appdata_full v) {
			float4 h = tex2Dlod(_Height, float4(v.texcoord.xy, 0, 0));
			v.vertex.y += h.r + h.g + h.b;
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 waterCol = tex2D(_WaterTex, IN.uv_MainTex) * _WaterCol;
			fixed4 groundCol = tex2D(_GroundTex, IN.uv_MainTex) * _GroundCol;
			fixed4 height = tex2D(_Height, IN.uv_MainTex);
			
			o.Albedo = lerp(groundCol, waterCol, height.b);
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Gloss;
			o.Alpha = 1; // c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
