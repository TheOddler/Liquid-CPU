//#EditorFriendly
//#node7:posx=-343:posy=119.5:title=TexWithXform:title2=AlphaMapTexture:input0=(0,0):input0type=Vector2:
//#node6:posx=-465:posy=88.5:title=ParamFloat:title2=Distance:input0=1:input0type=float:
//#node5:posx=-342:posy=-38.5:title=TexWithXform:title2=MainTex:input0=(0,0):input0type=Vector2:
//#node4:posx=0:posy=0:title=Lighting:title2=On:
//#node3:posx=0:posy=0:title=DoubleSided:title2=Back:
//#node2:posx=0:posy=0:title=FallbackInfo:title2=Transparent/Cutout/VertexLit:input0=1:input0type=float:
//#node1:posx=0:posy=0:title=LODInfo:title2=LODInfo1:input0=600:input0type=float:
//#masterNode:posx=0:posy=0:title=Master Node:input0linkindexnode=5:input0linkindexoutput=0:input4linkindexnode=7:input4linkindexoutput=0:
//#sm=2.0
//#blending=Alpha
//#ShaderName
Shader "ShaderFusion/DeepWater" {
	Properties {
		_Color ("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_SpecColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		//#ShaderProperties
		_MainTex ("MainTex", 2D) = "white" {}
		//_AlphaMapTexture ("AlphaMapTexture", 2D) = "white" {}
	}
	Category {
		SubShader {
			//#Blend
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			//#CatTags
			Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			Lighting On
			Cull Back
			//#LOD
			LOD 600
			//#GrabPass
			CGPROGRAM
			//#LightingModelTag
			#pragma surface surf ShaderFusion vertex:vert exclude_path:prepass
			 //use custom lighting functions

			 //custom surface output structure
			 struct SurfaceShaderFusion {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half Specular;
				half3 GlossColor; //Gloss is now three-channel
				half Alpha;
			 };
			 //forward lighting function
			 inline half4 LightingShaderFusion (SurfaceShaderFusion s, half3 lightDir, half3 viewDir, half atten) {
				#ifndef USING_DIRECTIONAL_LIGHT
				lightDir = normalize(lightDir);
				#endif
				viewDir = normalize(viewDir);
				half3 h = normalize (lightDir + viewDir);

				half diff = max (0, dot (s.Normal, lightDir));

				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0);

				half4 c;
				//Use gloss colour instead of gloss
				c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * s.GlossColor * spec) * (atten * 2);
				//We use gloss luminance to determine its overbright contribution
				c.a = s.Alpha + _LightColor0.a * Luminance(s.GlossColor) * spec * atten;
				return c;
			 }
			 //deferred lighting function
			 inline half4 LightingShaderFusion_PrePass (SurfaceShaderFusion s, half4 light) {
				//Use gloss colour instead of gloss
				half3 spec = light.a * s.GlossColor;

				half4 c;
				c.rgb = (s.Albedo * light.rgb + light.rgb * spec.rgb);
				//We use gloss luminance to determine its overbright contribution
				c.a = s.Alpha + Luminance(spec);
				return c;
			 }
			//#TargetSM
			#pragma target 2.0
			//#UnlitCGDefs
			sampler2D _MainTex;
			//sampler2D _AlphaMapTexture;
			float4 _Color;
			struct Input {
				//#UVDefs
				float2 uv_MainTex;
				//float2 uv_AlphaMapTexture;
				float4 color: Color;
				INTERNAL_DATA
			};

			void vert (inout appdata_full v, out Input o) {
				//#DeferredVertexBody
				//#DeferredVertexEnd
			}
			void surf (Input IN, inout SurfaceShaderFusion o) {
				float4 normal = float4(0.0,0.0,1.0,0.0);
				float3 emissive = 0.0;
				float3 specular = 1.0;
				float gloss = 1.0;
				float3 diffuse = 1.0;
				//float alpha = 1.0;
				//#PreFragBody
				float4 node5 = tex2D(_MainTex,IN.uv_MainTex.xy);
				//float4 node7 = tex2D(_AlphaMapTexture,IN.uv_AlphaMapTexture.xy);
				//#FragBody
				//alpha = (node7);
				diffuse = (node5);

				o.Albedo = diffuse.rgb*_Color;
				#ifdef SHADER_API_OPENGL
				o.Albedo = max(float3(0,0,0),o.Albedo);
				#endif

				o.Emission = emissive*_Color;
				#ifdef SHADER_API_OPENGL
				o.Emission = max(float3(0,0,0),o.Emission);
				#endif

				o.GlossColor = specular*_SpecColor;
				#ifdef SHADER_API_OPENGL
				o.GlossColor = max(float3(0,0,0),o.GlossColor);
				#endif

				o.Specular = gloss;
				#ifdef SHADER_API_OPENGL
				o.Specular = max(float3(0,0,0),o.Specular);
				#endif

				o.Alpha = IN.color.a; // alpha*_Color.a;
				#ifdef SHADER_API_OPENGL
				o.Alpha = max(0,o.Alpha);
				#endif

				o.Normal = normal;
				//#FragEnd
			}
		ENDCG
		}
	}
	//#Fallback
	Fallback "Transparent/Cutout/VertexLit"
}
