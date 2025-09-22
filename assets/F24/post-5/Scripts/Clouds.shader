Shader "Custom/Clouds"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Scale1("Scale1", Float) = 1.0
        _Factor1("Factor1", Float) = 1.0
        _Scale2("Scale2", Float) = 1.0
        _Factor2("Factor2", Float) = 1.0
        _ScaleY("ScaleY", Float) = 1.0
        _Threshold("Noise Threshold", Float) = 0.5
        _Intensity("Intensity", Float) = 1
        _FadeThreshold("Fade Threshold", Float) = 20
        _FadeScale("Fade Scale", Float) = 0.1
        _ScrollRate("Scroll Rate", Float) = 1
    }
        SubShader
    {
        Tags {"RenderType" = "Opaque"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            //texture
            sampler2D _MainTex;

            // Properties
            float _Scale1;
            float _Factor1;
            float _Scale2;
            float _Factor2;

            //camera parameters
            float _ScreenX;
            float _ScreenY;
            float _ScreenWidth;
            float _ScreenHeight;

            //misc parameters
            float _ScaleY;
            float _Threshold;
            float _Intensity;
            float _FadeThreshold;
            float _FadeScale;
            float _ScrollRate;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 unity_gradientNoise_dir(float2 p) {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float unity_gradientNoise(float2 UV, float Scale) {
                float2 p = UV * Scale;

                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(unity_gradientNoise_dir(ip), fp);
                float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
            }

            float4 get_cloud_color(float2 UV) {
                UV = float2(UV.x, UV.y * _ScaleY);
                float n1 = unity_gradientNoise(UV, _Scale1) / 2 + 0.5;
                float n2 = unity_gradientNoise(UV, _Scale2) / 2 + 0.5;
                float n = (n1 * _Factor1 + n2 * _Factor2) / (_Factor1 + _Factor2) + 0.5;
                float c = 1 - n * 0.1 * _Intensity;
                float a = max(0, n - 1 + _Threshold);
                return float4(c,c,c, min(1,a * _Intensity));
            }


            float4 frag(v2f i) : SV_Target
            {
                //uv transform
                float relativeHeight = _ScreenHeight / 20;
                float cloudScale = relativeHeight / (relativeHeight + 1);

                float fade = min(1, max(0, (_ScreenHeight - _FadeThreshold) * _FadeScale));

                float2 cloudUV = i.uv - 0.5;
                cloudUV *= float2(_ScreenWidth, _ScreenHeight) * cloudScale;
                cloudUV += float2(_ScreenX, _ScreenY);
                cloudUV.x += _ScrollRate * _Time.y;

                //color computation
                float4 cloud = get_cloud_color(cloudUV) * float4(1,1,1,fade);
                float4 color = tex2D(_MainTex, i.uv);

                return cloud.a*cloud + (1-cloud.a)*color;
            }
            ENDCG
        }
    }
}