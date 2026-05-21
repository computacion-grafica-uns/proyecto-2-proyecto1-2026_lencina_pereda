Shader "Custom/ShaderBlinn-Phong"
{
    Properties {
        _MainTex ("Textura de Albedo", 2D) = "white" {}
        _NormalMap ("Mapa de Normales", 2D) = "bump" {}
        _MatColor ("Color de Tinte", Color) = (1, 1, 1, 1)
        _SpecColor ("Color Especular", Color) = (1, 1, 1, 1)
        _Shininess ("Exponente de Brillo", Range(1, 128)) = 32
        _UseNormalMap ("Usa Mapa de Normales (0 o 1)", Float) = 0
    }
    SubShader {
        // ==========================================================
        // VOLVEMOS A LA ESTABILIDAD DE LA ACTIVIDAD 11 (OPACO)
        // ==========================================================
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Blend Off 
        ZWrite On 
        Cull Back // Restaura el culling normal para evitar fallos de profundidad

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
            float4 _MatColor; float4 _SpecColor; float _Shininess; float _UseNormalMap;

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

                // Normal de la Actividad 11
                o.viewNormal = normalize(mul((float3x3)_ViewMatrix, mul((float3x3)_ModelMatrix, v.normal)));
                
                // Tangente con blindaje para el ObjParser del piso
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
                float4 texColor = tex2D(_MainTex, i.uv);
                float4 albedo = texColor * _MatColor;

                float3 N = normalize(i.viewNormal);
                float3 V = normalize(-i.viewPos);

                // Mapeo de Normales protegido
                if (_UseNormalMap > 0.5) {
                    float3 T = normalize(i.viewTangent.xyz);
                    float w = (i.viewTangent.w != 0.0) ? i.viewTangent.w : 1.0;
                    float3 B = normalize(cross(N, T) * w);
                    float3x3 TBN = float3x3(T, B, N);
                    float3 nm = tex2D(_NormalMap, i.uv).rgb * 2.0 - 1.0;
                    N = normalize(mul(nm, TBN));
                }

                float3 totalDiffuse = float3(0,0,0);
                float3 totalSpecular = float3(0,0,0);
                
                // Agregamos luz ambiental base para que no sea negro absoluto sin luces
                float3 ambient = albedo.rgb * 0.1; 

                // 1. DIRECCIONAL
                if (_DirLightActive > 0.5) {
                    float3 lightDirView = normalize(mul((float3x3)_ViewMatrix, _LightDirWorld.xyz));
                    float3 L = normalize(-lightDirView);
                    float NdotL = max(0.0, dot(N, L));
                    totalDiffuse += _DirLightColor.rgb * albedo.rgb * NdotL * _DirIntensity;

                    float3 H = normalize(L + V);
                    float NdotH = max(0.0, dot(N, H));
                    totalSpecular += _DirLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * _DirIntensity;
                }

                // 2. PUNTUAL
                if (_PointLightActive > 0.5) {
                    float3 lightPosView = mul(_ViewMatrix, float4(_LightPosWorld.xyz, 1.0)).xyz;
                    float3 toLight = lightPosView - i.viewPos;
                    float d = length(toLight);

                    if (d > 0.001) {
                        float3 L = toLight / d;
                        float atten = max(0.0, 1.0 - (d / _PointLightRadius));
                        float NdotL = max(0.0, dot(N, L));
                        totalDiffuse += _PointLightColor.rgb * albedo.rgb * NdotL * atten * _PointIntensity;

                        float3 H = normalize(L + V);
                        float NdotH = max(0.0, dot(N, H));
                        totalSpecular += _PointLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * atten * _PointIntensity;
                    }
                }

                // 3. SPOT
                if (_SpotLightActive > 0.5) {
                    float3 spotPosView = mul(_ViewMatrix, float4(_SpotPosWorld.xyz, 1.0)).xyz;
                    float3 toLight = spotPosView - i.viewPos;
                    float d = length(toLight);

                    if (d > 0.001) {
                        float3 L = toLight / d;
                        float3 spotDirView = normalize(mul((float3x3)_ViewMatrix, _SpotDirWorld.xyz));
                        float3 dirFocoViewInvertida = normalize(-spotDirView);

                        float dotVal = clamp(dot(L, dirFocoViewInvertida), -1.0, 1.0);
                        float angulo = acos(dotVal);

                        if (angulo < radians(_Apertura)) {
                            float atten = max(0.0, 1.0 - (d / _SpotLightRadius));
                            float NdotL = max(0.0, dot(N, L));
                            totalDiffuse += _SpotLightColor.rgb * albedo.rgb * NdotL * atten * _SpotIntensity;

                            float3 H = normalize(L + V);
                            float NdotH = max(0.0, dot(N, H));
                            totalSpecular += _SpotLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * atten * _SpotIntensity;
                        }
                    }
                }

                // Forzamos Opacidad Total (1.0) para anular comportamientos fantasmas
                return float4(ambient + totalDiffuse + totalSpecular, 1.0);
            }
            ENDCG
        }
    }
}