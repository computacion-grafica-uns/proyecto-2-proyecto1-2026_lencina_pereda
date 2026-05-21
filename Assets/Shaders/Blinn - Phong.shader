Shader "Custom/ShaderBlinn-Phong"
{
    Properties {
        _MainTex ("Textura de Albedo (Base)", 2D) = "white" {}
        _MatColor ("Color de Tinte", Color) = (1, 1, 1, 1)
        _SpecColor ("Color Especular (Brillo)", Color) = (1, 1, 1, 1)
        _Shininess ("Exponente de Brillo", Range(1, 128)) = 32

        [Header(Mapeo de Normales)]
        _UseNormalMap ("Usa Mapa de Normales (0 o 1)", Float) = 0
        _NormalMap ("Mapa de Normales (TBN)", 2D) = "bump" {}
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite On 

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT; // Viene (0,0,0,0) de un OBJ sin tangentes
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
            sampler2D _MainTex;
            sampler2D _NormalMap;
            float4 _MatColor;
            float4 _SpecColor;
            float _Shininess;
            float _UseNormalMap;

            float _DirLightActive; float _PointLightActive; float _SpotLightActive;
            float _DirIntensity; float _PointIntensity; float _SpotIntensity;
            float4 _LightPosWorld; float4 _SpotPosWorld; float4 _LightDirWorld; float4 _SpotDirWorld; 
            float4 _DirLightColor; float4 _PointLightColor; float4 _SpotLightColor;
            float _PointLightRadius; float _SpotLightRadius; float _Apertura;

            // --- FUNCIÓN DE BLINDAJE NaN: Normalización Segura ---
            // Evita divisiones por cero o epsilon sin sesgar ejes.
            float3 SafeNormalize(float3 V) {
                float len = length(V);
                // Si el vector es casi nulo, devolvemos un vector arbitrario estable
                // o el mismo vector para que collapse a 0 si la longitud es 0.
                // Lo mas estable es un vector nulo protegido contra normalizacion posterior.
                return (len < 1e-6) ? float3(0, 0, 0) : V / len;
            }

            v2f vert (appdata v) {
                v2f o;
                float4 worldPos = mul(_ModelMatrix, v.vertex);
                float4 viewPos = mul(_ViewMatrix, worldPos);
                o.vertex = mul(_ProjectionMatrix, viewPos);
                
                o.uv = v.uv;
                o.viewPos = viewPos.xyz;
                
                // --- BLINDAJE NaN 1: Normales y Tangentes ---
                // No sumamos epsilon arbitrario a un eje. Usamos SafeNormalize.
                float3 normalW = mul((float3x3)_ModelMatrix, v.normal);
                o.viewNormal = SafeNormalize(mul((float3x3)_ViewMatrix, normalW));
                
                float3 tangentW = mul((float3x3)_ModelMatrix, v.tangent.xyz);
                o.viewTangent.xyz = SafeNormalize(mul((float3x3)_ViewMatrix, tangentW));
                o.viewTangent.w = (v.tangent.w != 0.0) ? v.tangent.w : 1.0;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 texColor = tex2D(_MainTex, i.uv) * _MatColor;
                float3 totalDiffuse = float3(0, 0, 0);
                float3 totalSpecular = float3(0, 0, 0);
                float3 N_Geom = SafeNormalize(i.viewNormal);

                // --- BLINDAJE NaN 2: Vector de Vista V ---
                // Si i.viewPos es 0, colapsa.
                float3 V = SafeNormalize(-i.viewPos); 

                // Si V es nulo (SafeNormalize fallo), forzamos un vector estable
                if (length(V) < 0.1) V = float3(0, 0, 1);

                // Blindaje robusto de brillos nulos
                float shine = max(1e-4, _Shininess);

                float3 N;
                if (_UseNormalMap > 0.5) {
                    float3 normalGeom = N_Geom;
                    float3 tangentGeom = SafeNormalize(i.viewTangent.xyz);
                    
                    // Aseguramos que la bitangente no colapse (blindaje epsilon)
                    float3 bitangentGeom = SafeNormalize(cross(normalGeom, tangentGeom) + float3(1e-7, 1e-7, 1e-7)) * i.viewTangent.w;
                    float3x3 tbnMatrix = float3x3(tangentGeom, bitangentGeom, normalGeom);

                    float3 rgbNormal = tex2D(_NormalMap, i.uv).rgb * 2.0 - 1.0;
                    N = SafeNormalize(mul(rgbNormal, tbnMatrix));
                } else {
                    N = N_Geom;
                }

                // --- MODELO DE ILUMINACIÓN BLINN-PHONG CON BLINDAJE NaN ---

                // 1. DIRECCIONAL
                if (_DirLightActive > 0.5) {
                    float3 L = SafeNormalize(mul((float3x3)_ViewMatrix, -_LightDirWorld.xyz));
                    float NdotL = max(0.0, dot(N, L));
                    totalDiffuse += _DirLightColor.rgb * texColor.rgb * NdotL * _DirIntensity;
                    
                    // --- BLINDAJE NaN 3: Half-Vector H (Aquí estaba el fallo previo) ---
                    float3 H_V = L + V;
                    // Sumamos una micro-proteccion esferica (0.000001 en los 3 ejes) antes de normalizar
                    // Esto evita el sesgo en X y protege division por 0.
                    float3 H = SafeNormalize(H_V + float3(1e-6, 1e-6, 1e-6));

                    // --- BLINDAJE NaN 4: pow(NdotH, shine) ---
                    // Asegurar base >= 0 y base > 0 si n <= 0.
                    float NdotH_S = max(1e-6, dot(N, H)); 
                    totalSpecular += _DirLightColor.rgb * _SpecColor.rgb * pow(NdotH_S, shine) * 0.5 * _DirIntensity;
                }

                // 2. PUNTUAL
                if (_PointLightActive > 0.5) {
                    float3 lightPosView = mul(_ViewMatrix, _LightPosWorld).xyz;
                    float3 toLight = lightPosView - i.viewPos;
                    // --- BLINDAJE NaN 5: L de Puntual (Distancia d) ---
                    float d = length(toLight);
                    // Si d es casi 0 (camara dentro de luz), blindamos L.
                    float3 L = (d < 1e-6) ? float3(0, 1, 0) : toLight / d;
                    
                    // Atenuación protegida contra division por 0.
                    float atten = saturate(1.0 - (d / (max(1e-4, _PointLightRadius))));

                    float NdotL = max(0.0, dot(N, L));
                    totalDiffuse += _PointLightColor.rgb * texColor.rgb * NdotL * atten * _PointIntensity;

                    // Blindaje H robusto para Puntual
                    float3 H = SafeNormalize(L + V + float3(1e-6, 1e-6, 1e-6));
                    float NdotH_S = max(1e-6, dot(N, H));
                    totalSpecular += _PointLightColor.rgb * _SpecColor.rgb * pow(NdotH_S, shine) * 0.5 * atten * _PointIntensity;
                }

                // 3. SPOT
                if (_SpotLightActive > 0.5) {
                    float3 lightPosView = mul(_ViewMatrix, _SpotPosWorld).xyz;
                    float3 toLight = lightPosView - i.viewPos;
                    // Blindaje d
                    float d = length(toLight);
                    float3 L = (d < 1e-6) ? float3(0, 1, 0) : toLight / d;

                    // Blindaje SpotDir (fallo si es vector nulo)
                    float3 spotDirView = SafeNormalize(mul((float3x3)_ViewMatrix, _SpotDirWorld.xyz));
                    float3 dirFocoViewInvertida = SafeNormalize(-spotDirView);
                    
                    // Blindaje acos/clamp
                    float angulo = acos(clamp(dot(L, dirFocoViewInvertida), -1.0, 1.0));
                    
                    if (angulo < radians(_Apertura)) {
                        // Blindaje atten
                        float atten = saturate(1.0 - (d / (max(1e-4, _SpotLightRadius))));
                        float NdotL = max(0.0, dot(N, L));
                        totalDiffuse += _SpotLightColor.rgb * texColor.rgb * NdotL * atten * _SpotIntensity;
                        
                        // Blindaje H robusto para Spot
                        float3 H = SafeNormalize(L + V + float3(1e-6, 1e-6, 1e-6));
                        float NdotH_S = max(1e-6, dot(N, H));
                        totalSpecular += _SpotLightColor.rgb * _SpecColor.rgb * pow(NdotH_S, shine) * 0.5 * atten * _SpotIntensity;
                    }
                }

                return float4(totalDiffuse + totalSpecular, texColor.a);
            }
            ENDCG
        }
    }
}