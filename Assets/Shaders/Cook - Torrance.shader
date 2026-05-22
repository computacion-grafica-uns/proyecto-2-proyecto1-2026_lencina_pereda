Shader "Custom/ShaderCookTorrance"
{
    Properties {
        _MainTex ("Textura de Albedo (Base)", 2D) = "white" {}
        _NormalMap ("Mapa de Normales", 2D) = "bump" {}
        _MatColor ("Color de Tinte", Color) = (1, 1, 1, 1)
        
        [Header(Parametros PBR)]
        _Roughness ("Rugosidad (0=Pulido, 1=Mate)", Range(0.01, 1)) = 0.5
        _Metallic ("Metalicidad (0=Dielectrico, 1=Metal)", Range(0, 1)) = 0
        
        // Interruptores C#
        _UseTexture ("Usa Textura Base (0 o 1)", Float) = 1
        _UseNormalMap ("Usa Mapa de Normales (0 o 1)", Float) = 0
    }
    SubShader {
        // --- CORRECCIÓN DE OPACIDAD ---
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Blend Off 
        ZWrite On 
        Cull Back

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewPos : TEXCOORD1;     
                float3 viewNormal : NORMAL;
                float4 viewTangent : TEXCOORD3;
            };

            uniform float4x4 _ModelMatrix, _ViewMatrix, _ProjectionMatrix;
            sampler2D _MainTex; sampler2D _NormalMap;
            float4 _MatColor; float _Roughness; float _Metallic;
            float _UseNormalMap; float _UseTexture;

            float _DirLightActive; float _PointLightActive; float _SpotLightActive;
            float _DirIntensity; float _PointIntensity; float _SpotIntensity;
            float4 _LightPosWorld; float4 _SpotPosWorld; float4 _LightDirWorld; float4 _SpotDirWorld; 
            float4 _DirLightColor; float4 _PointLightColor; float4 _SpotLightColor;
            float _PointLightRadius; float _SpotLightRadius; float _Apertura;

            // --- FÍSICA PBR ---
            float DistributionGGX(float3 N, float3 H, float roughness) {
                float a = roughness * roughness;
                float a2 = a * a;
                float NdotH = max(dot(N, H), 0.0);
                float NdotH2 = NdotH * NdotH;
                float num = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = UNITY_PI * denom * denom;
                return num / max(denom, 0.000001);
            }

            float GeometrySchlickGGX(float NdotV, float roughness) {
                float r = (roughness + 1.0);
                float k = (r * r) / 8.0;
                float num = NdotV;
                float denom = NdotV * (1.0 - k) + k;
                return num / max(denom, 0.000001);
            }

            float GeometrySmith(float3 N, float3 V, float3 L, float roughness) {
                float NdotV = max(dot(N, V), 0.0);
                float NdotL = max(dot(N, L), 0.0);
                float ggx2 = GeometrySchlickGGX(NdotV, roughness);
                float ggx1 = GeometrySchlickGGX(NdotL, roughness);
                return ggx1 * ggx2;
            }

            float3 FresnelSchlick(float cosTheta, float3 F0) {
                return F0 + (1.0 - F0) * pow(max(1.0 - cosTheta, 0.0), 5.0);
            }

            v2f vert (appdata v) {
                v2f o;
                float4 worldPos = mul(_ModelMatrix, v.vertex);
                float4 viewPos = mul(_ViewMatrix, worldPos);
                o.vertex = mul(_ProjectionMatrix, viewPos);
                o.uv = v.uv;
                o.viewPos = viewPos.xyz;

                // Normal de vista
                o.viewNormal = normalize(mul((float3x3)_ViewMatrix, mul((float3x3)_ModelMatrix, v.normal)));
                
                // Tangente protegida para soportar modelos sin matriz tangencial (como el obj del piso)
                float3 tWorld = mul((float3x3)_ModelMatrix, v.tangent.xyz);
                if (length(tWorld) > 0.001) {
                    o.viewTangent.xyz = normalize(mul((float3x3)_ViewMatrix, tWorld));
                } else {
                    o.viewTangent.xyz = float3(1, 0, 0);
                }
                o.viewTangent.w = v.tangent.w;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Selector de Textura / Color Plano
                float4 albedo = _MatColor;
                if (_UseTexture > 0.5) {
                    albedo = tex2D(_MainTex, i.uv);
                }

                // Cálculo de Normales (Relieve)
                float3 N = normalize(i.viewNormal);
                if (_UseNormalMap > 0.5) {
                    float3 T = normalize(i.viewTangent.xyz);
                    float w = (i.viewTangent.w != 0.0) ? i.viewTangent.w : 1.0;
                    float3 B = normalize(cross(N, T) * w);
                    float3x3 TBN = float3x3(T, B, N);
                    
                    float3 nm = tex2D(_NormalMap, i.uv).rgb * 2.0 - 1.0;
                    N = normalize(mul(nm, TBN)); 
                }

                float3 V = normalize(-i.viewPos);
                float3 F0 = float3(0.04, 0.04, 0.04);
                F0 = lerp(F0, albedo.rgb, _Metallic);

                float3 totalRadiance = float3(0, 0, 0);

                // --- BUCLE DE LUCES PBR ---
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
                        float3 H = normalize(V + L);
                        float NdotL = max(dot(N, L), 0.0);
                        float NdotV = max(dot(N, V), 0.0);

                        float D = DistributionGGX(N, H, _Roughness);
                        float G = GeometrySmith(N, V, L, _Roughness);
                        float3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

                        float3 numerador = D * G * F;
                        float denominador = 4.0 * NdotV * NdotL;
                        float3 specular = numerador / max(denominador, 0.001);

                        float3 kS = F;
                        float3 kD = float3(1.0, 1.0, 1.0) - kS;
                        kD *= (1.0 - _Metallic);

                        totalRadiance += (kD * albedo.rgb / UNITY_PI + specular) * lightColor * atten * NdotL;
                    }
                }

                // Sumamos la ambiental base y forzamos el canal Alpha a 1.0
                float3 ambient = albedo.rgb * 0.1;
                return float4(ambient + totalRadiance, 1.0);
            }
            ENDCG
        }
    }
}