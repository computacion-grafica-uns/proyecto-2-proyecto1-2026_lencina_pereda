Shader "Custom/ShaderBlinn-Phong"
{
    Properties {
        _MainTex ("Textura del Objeto", 2D) = "white" {}
        _MatColor ("Color de Tinte (Albedo)", Color) = (1, 1, 1, 1)
        _SpecColor ("Color Especular (Brillo)", Color) = (1, 1, 1, 1)
        _Shininess ("Exponente de Brillo", Range(1, 128)) = 32
    }
    SubShader {
        // 1. CONFIGURACIÓN DE TRANSPARENCIA PARA EL VIDRIO
        // Cambiamos la cola de renderizado a Transparent para que Unity no rompa el Z-Buffer
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        // Activamos el mezclado de canales (Alpha Blending) en el hardware de la GPU
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite On // Mantenemos la escritura de profundidad activa para la oclusión correcta

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

            // Matrices del Pipeline Manual
            uniform float4x4 _ModelMatrix, _ViewMatrix, _ProjectionMatrix;
            
            // Datos de Superficie y Material Independiente
            sampler2D _MainTex;
            float4 _MatColor;
            float4 _SpecColor;
            float _Shininess;

            // Uniforms de Control de Luces (Inyectados desde LuzController)
            float _DirLightActive; float _PointLightActive; float _SpotLightActive;
            float _DirIntensity; float _PointIntensity; float _SpotIntensity;
            float4 _LightPosWorld; float4 _SpotPosWorld; float4 _LightDirWorld; float4 _SpotDirWorld; 
            float4 _DirLightColor; float4 _PointLightColor; float4 _SpotLightColor;
            float _PointLightRadius; float _SpotLightRadius; float _Apertura;

            v2f vert (appdata v) {
                v2f o;
                // Transformación geométrica manual
                float4 worldPos = mul(_ModelMatrix, v.vertex);
                float4 viewPos = mul(_ViewMatrix, worldPos);
                o.vertex = mul(_ProjectionMatrix, viewPos);
                
                o.uv = v.uv;
                o.viewPos = viewPos.xyz;
                o.viewNormal = mul((float3x3)_ViewMatrix, mul((float3x3)_ModelMatrix, v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Fusión de la textura (estática o procedural de C#) con el tinte del material
                float4 texColor = tex2D(_MainTex, i.uv) * _MatColor;
                
                float3 N = normalize(i.viewNormal);
                float3 V = normalize(-i.viewPos); // Vector hacia la cámara en View Space

                float3 totalDiffuse = float3(0, 0, 0);
                float3 totalSpecular = float3(0, 0, 0);

                // 1. CÁLCULO LUZ DIRECCIONAL (SOL)
                if (_DirLightActive > 0.5) {
                    float3 L = normalize(mul((float3x3)_ViewMatrix, -_LightDirWorld.xyz));
                    float NdotL = max(0.0, dot(N, L));
                    totalDiffuse += _DirLightColor.rgb * texColor.rgb * NdotL * _DirIntensity;

                    float3 H = normalize(L + V);
                    float NdotH = max(0.0, dot(N, H));
                    // APLICAMOS EL COLOR ESPECULAR DEL MATERIAL
                    totalSpecular += _DirLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * _DirIntensity;
                }

                // 2. CÁLCULO LUZ PUNTUAL (BOMBITA OMNIDIRECCIONAL)
                if (_PointLightActive > 0.5) {
                    float3 lightPosView = mul(_ViewMatrix, _LightPosWorld).xyz;
                    float3 toLight = lightPosView - i.viewPos;
                    float d = length(toLight);
                    float3 L = normalize(toLight);

                    float atten = max(0.0, 1.0 - (d / _PointLightRadius));
                    float NdotL = max(0.0, dot(N, L));
                    totalDiffuse += _PointLightColor.rgb * texColor.rgb * NdotL * atten * _PointIntensity;

                    float3 H = normalize(L + V);
                    float NdotH = max(0.0, dot(N, H));
                    // APLICAMOS EL COLOR ESPECULAR DEL MATERIAL
                    totalSpecular += _PointLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * atten * _PointIntensity;
                }

                // 3. CÁLCULO LUZ SPOT (FOCO CONICO DE APERTURA)
                if (_SpotLightActive > 0.5) {
                    float3 lightPosView = mul(_ViewMatrix, _SpotPosWorld).xyz;
                    float3 toLight = lightPosView - i.viewPos;
                    float d = length(toLight);
                    float3 L = normalize(toLight);

                    float3 spotDirView = normalize(mul((float3x3)_ViewMatrix, _SpotDirWorld.xyz));
                    float3 dirFocoViewInvertida = normalize(-spotDirView);
                    float angulo = acos(dot(L, dirFocoViewInvertida));
                    
                    if (angulo < radians(_Apertura)) {
                        float atten = max(0.0, 1.0 - (d / _SpotLightRadius));
                        float NdotL = max(0.0, dot(N, L));
                        totalDiffuse += _SpotLightColor.rgb * texColor.rgb * NdotL * atten * _SpotIntensity;
                        
                        float3 H = normalize(L + V);
                        float NdotH = max(0.0, dot(N, H));
                        // APLICAMOS EL COLOR ESPECULAR DEL MATERIAL
                        totalSpecular += _SpotLightColor.rgb * _SpecColor.rgb * pow(NdotH, _Shininess) * 0.5 * atten * _SpotIntensity;
                    }
                }

                // Retornamos la suma de radiancia conservando el canal Alpha calculado (texColor.a)
                return float4(totalDiffuse + totalSpecular, texColor.a);
            }
            ENDCG
        }
    }
}