// Halloween Quest sky: drawn on an inside-out sphere that follows the camera (SpookyFx).
// Three-colour gradient, twinkling stars, a huge glowing moon, drifting cloud bands in front of it
// and (witch forest) an aurora. No fog, no depth write: everything else draws over it, and the
// horizon colour matches the fog so far scenery melts into the sky.
Shader "Spooky/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.05, 0.02, 0.15, 1)
        _Mid ("Mid", Color) = (0.3, 0.08, 0.4, 1)
        _Horizon ("Horizon", Color) = (1, 0.45, 0.2, 1)
        _MoonDir ("Moon direction", Vector) = (0, 0.25, 1, 0)
        _MoonColor ("Moon", Color) = (1, 0.9, 0.7, 1)
        _MoonSize ("Moon radius (rad)", Float) = 0.09
        _Stars ("Stars", Float) = 1
        _Clouds ("Clouds", Float) = 0.5
        _CloudColor ("Cloud colour", Color) = (0.2, 0.1, 0.3, 1)
        _Aurora ("Aurora", Float) = 0
        _AuroraA ("Aurora A", Color) = (0.2, 1, 0.6, 1)
        _AuroraB ("Aurora B", Color) = (0.6, 0.3, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "IgnoreProjector" = "True" "PreviewType" = "Skybox" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Top, _Mid, _Horizon, _MoonDir, _MoonColor, _CloudColor, _AuroraA, _AuroraB;
            float _MoonSize, _Stars, _Clouds, _Aurora;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;   // the sphere never rotates: object direction = world direction
                return o;
            }

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i + float3(0, 0, 0)), hash(i + float3(1, 0, 0)), f.x),
                                 lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), f.x),
                                 lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float t = saturate(h);
                float time = _Time.y;

                // gradient: horizon glow -> mid -> top
                float3 col = lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0.0, 0.28, t));
                col = lerp(col, _Top.rgb, smoothstep(0.22, 0.85, t));

                // stars: one per grid cell, small round dots that twinkle
                float3 sp = d * 160.0;
                float3 cell = floor(sp);
                float r = hash(cell);
                float3 f = frac(sp) - 0.5;
                float dot1 = saturate(1.0 - length(f) * 3.0);
                float star = step(0.975, r) * dot1 * dot1 * smoothstep(0.08, 0.4, h);
                float tw = 0.55 + 0.45 * sin(time * (2.0 + r * 4.0) + r * 60.0);
                col += star * tw * _Stars * 2.2;

                // aurora curtains (witch forest)
                if (_Aurora > 0.001)
                {
                    float wave = sin(d.x * 3.2 + noise(d * 2.0 + time * 0.12) * 3.0 + time * 0.35);
                    float band = pow(saturate(1.0 - abs(wave)), 2.5) * smoothstep(0.08, 0.3, h) * (1.0 - smoothstep(0.55, 0.9, h));
                    float flick = 0.6 + 0.4 * noise(float3(d.x * 20.0, time * 0.8, 0));
                    col += lerp(_AuroraA.rgb, _AuroraB.rgb, saturate(d.x * 0.5 + 0.5)) * band * flick * _Aurora;
                }

                // moon: disc with soft craters and a wide halo
                float3 md = normalize(_MoonDir.xyz);
                float ang = acos(clamp(dot(d, md), -1.0, 1.0));
                float halo = exp(-ang / _MoonSize * 1.1);
                col += _MoonColor.rgb * halo * 0.55;
                float disc = 1.0 - smoothstep(_MoonSize * 0.97, _MoonSize, ang);
                float crater = noise(d * 55.0) * 0.6 + noise(d * 130.0) * 0.4;
                float3 moon = _MoonColor.rgb * (1.05 + 0.35 * (crater - 0.5)) * 1.6;
                col = lerp(col, moon, disc);

                // drifting cloud bands, dark against the moon
                if (_Clouds > 0.001)
                {
                    float2 uv = d.xz / (h + 0.18);
                    float n = noise(float3(uv * 1.3 + float2(time * 0.03, 0), 0.0)) * 0.65 + noise(float3(uv * 3.1 + float2(time * 0.05, 1.7), 2.0)) * 0.35;
                    float c = smoothstep(0.52, 0.78, n) * smoothstep(0.02, 0.14, h) * (1.0 - smoothstep(0.45, 0.8, h)) * _Clouds;
                    float3 cc = _CloudColor.rgb + _MoonColor.rgb * halo * 0.6;   // moon-lit rims
                    col = lerp(col, cc, c);
                }
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
