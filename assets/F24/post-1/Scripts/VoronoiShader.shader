Shader "Custom/VoronoiShader"
{
    Properties{
        _XScale("X Scale", Float) = 1.0
        _YScale("Y Scale", Float) = 1.0
        _TimeScale("Time Scale", Float) = 1.0
        _Threshold("Color Threshold", Float) = 1.0
        _Randomness("Randomness", Float) = 1.0
        _Smoothness("Smoothness", Float) = 1.0
        _Color1("Color 1", Color) = (0,0,0,1)
        _Color2("Color 2", Color) = (1,1,1,1)
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _XScale;
            float _YScale;
            float _TimeScale;
            float _Threshold;
            float _Randomness;
            float _Smoothness;
            float4 _Color1;
            float4 _Color2;

            float3 hash3(float3 p) {
                const float3 scale1 = float3(127.1, 311.7, 74.7);    // Prime constants
                const float3 scale2 = float3(269.5, 183.3, 246.1);   // More prime constants

                // First hashing step with dot products
                p = frac(sin(dot(p, scale1)) * 43758.5453);

                // Second hashing step for better randomness
                return frac(sin(dot(p, scale2)) * 43758.5453);
            }

            float smoothstep(float edge0, float edge1, float x) {
                // Clamp the value of x to the range [edge0, edge1]
                x = clamp((x - edge0) / (edge1 - edge0), 0, 1);

                // Apply the smoothstep interpolation
                return x * x * (3.0f - 2.0f * x);
            }

            float mix(float a, float b, float t) {
                return a * (1 - t) + b * t;
            }

            float4 mix(float4 a, float4 b, float t) {
                return a * (1 - t) + b * t;
            }

            float voronoi_distance(float3 a, float3 b)
            {
                return length(a - b);
            }

            float voronoi_smooth_f1(float3 coord, float smoothness)
            {
                float3 cellPosition = floor(coord);
                float3 localPosition = coord - cellPosition;

                float smoothDistance = 0.0;
                float h = -1.0;
                for (int k = -2; k <= 2; k++) {
                    for (int j = -2; j <= 2; j++) {
                        for (int i = -2; i <= 2; i++) {
                            float3 cellOffset = float3(i, j, k);
                            float3 pointPosition = cellOffset + hash3(cellPosition + cellOffset) * _Randomness;
                            float distanceToPoint = voronoi_distance(pointPosition, localPosition);
                            h = h == -1.0 ?
                                1.0 :
                                smoothstep(
                                    0.0, 1.0, 0.5 + 0.5 * (smoothDistance - distanceToPoint) / smoothness);
                            float correctionFactor = smoothness * h * (1.0 - h);
                            smoothDistance = mix(smoothDistance, distanceToPoint, h) - correctionFactor;
                        }
                    }
                }

                return smoothDistance;
            }

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.uv = v.uv;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                float3 pos = float3(i.uv * float2(_XScale,_YScale), _Time.y * _TimeScale);

                float f1 = voronoi_smooth_f1(pos, 0);
                float smooth_f1 = voronoi_smooth_f1(pos, _Smoothness);

                float t = clamp(f1 - smooth_f1, 0, 1);
                
                return t>_Threshold ? _Color1 : mix(_Color2,_Color1,t/_Threshold);
            }
            ENDCG
        }
    }
}