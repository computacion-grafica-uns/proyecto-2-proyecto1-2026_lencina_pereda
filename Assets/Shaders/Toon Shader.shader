Shader "Custom/ShaderSuperToon"
{
    Properties {
        _MainTex ("Textura de Albedo (Base)", 2D) = "white" {}
        _MatColor ("Color de Tinte", Color) = (1, 1, 1, 1)
        _SpecColor ("Color del Brillo Toon", Color) = (1, 1, 1, 1)
        
        [Header(Parametros Cel Shading)]
        _LuzUmbral ("Corte de Luz Difusa", Range(0, 1)) = 0.3
        _BrilloUmbral ("Corte de Brillo Anime", Range(0, 1)) = 0.9
        _Shininess ("Concentracion de Brillo", Range(1, 128)) = 64

        [Header(Parametros de Contorno)]
        _OutlineGrosor ("Grosor del Contorno", Range(0, 0.5)) = 0.25
        _OutlineColor ("Color del Contorno", Color) = (0, 0, 0, 1)

        // Interruptor C#
        _UseTexture ("Usa Textura Base (0 o 1)", Float) = 1
		
		_Opacidad ("Opacidad", Range(0,1)) = 1.0
        [HideInInspector] _SrcBlend ("_SrcBlend", Float) = 1.0
        [HideInInspector] _DstBlend ("_DstBlend", Float) = 0.0
        [HideInInspector] _ZWrite ("_ZWrite", Float) = 1.0
    }
    SubShader {
		
		Blend [_SrcBlend] [_DstBlend] 
        ZWrite [_ZWrite] 
        Cull Back

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewPos : TEXCOORD1;     
                float3 viewNormal : NORMAL;     
            };

            uniform float4x4 _ModelMatrix, _ViewMatrix, _ProjectionMatrix;
            sampler2D _MainTex; float4 _MatColor; float4 _SpecColor;
            float _LuzUmbral; float _BrilloUmbral; float _Shininess;
            float _OutlineGrosor; float4 _OutlineColor; float _UseTexture;
			
			float _Opacidad;

            float _DirLightActive; float _PointLightActive; float _SpotLightActive;
            float _DirIntensity; float _PointIntensity; float _SpotIntensity;
            float4 _LightPosWorld; float4 _SpotPosWorld; float4 _LightDirWorld; float4 _SpotDirWorld; 
            float4 _DirLightColor; float4 _PointLightColor; float4 _SpotLightColor;
            float _PointLightRadius; float _SpotLightRadius; float _Apertura;

            v2f vert (appdata v) {
                v2f o;
                float4 worldPos = mul(_ModelMatrix, v.vertex);
                float4 viewPos = mul(_ViewMatrix, worldPos);
                o.vertex = mul(_ProjectionMatrix, viewPos);
                o.uv = v.uv;
                o.viewPos = viewPos.xyz;
                o.viewNormal = normalize(mul((float3x3)_ViewMatrix, mul((float3x3)_ModelMatrix, v.normal)));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Selector de Textura / Color Plano
                float4 albedo = _MatColor;
                if (_UseTexture > 0.5) {
                    // Ahora la textura se tiñe con el color básico del material
                    albedo = tex2D(_MainTex, i.uv) * _MatColor; 
                }

                float3 N = normalize(i.viewNormal);
                float3 V = normalize(-i.viewPos);

                // --- OUTLINE ANALÍTICO ---
                float ndotv = max(0.0, dot(N, V));
                if (ndotv < _OutlineGrosor) {
                    return float4(_OutlineColor.rgb, 1.0);
                }

                float3 colorDifusoFinal = float3(0, 0, 0);
                float3 colorEspecularFinal = float3(0, 0, 0);

                for(int lightIdx = 0; lightIdx < 3; lightIdx++) {
                    float3 L = float3(0,0,0);
                    float3 lightColor = float3(0,0,0);
                    float atten = 1.0;
                    bool active = false;

                    if (lightIdx == 0 && _DirLightActive > 0.5) {
                        L = normalize(mul((float3x3)_ViewMatrix, -_LightDirWorld.xyz));
                        lightColor = _DirLightColor.rgb * _DirIntensity;
                        active = true;
                    }
                    else if (lightIdx == 1 && _PointLightActive > 0.5) {
                        float3 lightPosView = mul(_ViewMatrix, _LightPosWorld).xyz;
                        float3 toLight = lightPosView - i.viewPos;
                        float d = length(toLight);
                        L = normalize(toLight);
                        atten = max(0.0, 1.0 - (d / _PointLightRadius)) * _PointIntensity;
                        lightColor = _PointLightColor.rgb;
                        active = true;
                    }
                    else if (lightIdx == 2 && _SpotLightActive > 0.5) {
                        float3 lightPosView = mul(_ViewMatrix, _SpotPosWorld).xyz;
                        float3 toLight = lightPosView - i.viewPos;
                        float d = length(toLight);
                        L = normalize(toLight);
                        float3 spotDirView = normalize(mul((float3x3)_ViewMatrix, _SpotDirWorld.xyz));
                        float angulo = acos(dot(L, -spotDirView));
                        
                        if (angulo < radians(_Apertura)) {
                            atten = max(0.0, 1.0 - (d / _SpotLightRadius)) * _SpotIntensity;
                            lightColor = _SpotLightColor.rgb;
                            active = true;
                        }
                    }

                    if (active) {
                        float NdotL = max(dot(N, L), 0.0);
                        float factorCel = NdotL > _LuzUmbral ? 1.0 : 0.2;
                        colorDifusoFinal += albedo.rgb * lightColor * atten * factorCel;

                        float3 H = normalize(L + V);
                        float NdotH = max(dot(N, H), 0.0);
                        float factorSpecular = pow(NdotH, _Shininess) > _BrilloUmbral ? 1.0 : 0.0;
                        colorEspecularFinal += _SpecColor.rgb * lightColor * atten * factorSpecular;
                    }
                }

                // Luz ambiental 
                float3 ambient = albedo.rgb * 0.1;
                return float4(ambient + colorDifusoFinal + colorEspecularFinal, _Opacidad);
            }
            ENDCG
        }
    }
}