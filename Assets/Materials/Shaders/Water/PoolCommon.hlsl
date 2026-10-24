#ifndef POOLPATROL_POOL_COMMON_INCLUDED
#define POOLPATROL_POOL_COMMON_INCLUDED

// Shared state for the pool shaders. Everything here is a global pushed from C#, so the floor,
// surface, deck and per-object effects all agree on the pool's shape, the sun and the ripple sim.
//   PoolEnvironment : _PoolRect, _PoolShape, _PoolSun
//   RippleEffect    : _PoolRippleTex, _PoolRippleVP, _PoolRippleParams
// Every shader still renders sensibly in edit mode, where the ripple sim isn't running
// (_PoolRippleParams.z is 0 then, which zeroes every ripple term).

float4 _PoolRect;          // xy = pool centre (world), zw = half extents of the water area
float4 _PoolShape;         // x = corner radius
float4 _PoolSun;           // xy = direction light travels across the pool (shadow direction), z = rim shadow length, w = shadow softness

TEXTURE2D(_PoolRippleTex); SAMPLER(sampler_PoolRippleTex);
float4x4 _PoolRippleVP;    // view-projection of the ripple camera, maps world -> ripple texture
float4 _PoolRippleParams;  // x = 1 / texture size, y = unused, z = 1 when the sim is running

// Signed distance to the pool outline. Negative inside the water.
float PoolSDF(float2 p)
{
    float r = _PoolShape.x;
    float2 q = abs(p - _PoolRect.xy) - (_PoolRect.zw - r);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

// Outward unit normal of the nearest stretch of pool edge.
float2 PoolNormal(float2 p)
{
    float2 d = p - _PoolRect.xy;
    float2 q = abs(d) - (_PoolRect.zw - _PoolShape.x);
    float2 s = float2(d.x < 0.0 ? -1.0 : 1.0, d.y < 0.0 ? -1.0 : 1.0);
    float2 n;
    if (q.x > 0.0 && q.y > 0.0) n = normalize(q);
    else n = q.x > q.y ? float2(1.0, 0.0) : float2(0.0, 1.0);
    return n * s;
}

// 1 inside the rounded corners of the outline (and their extension outwards), else 0.
float PoolInCorner(float2 p)
{
    float2 q = abs(p - _PoolRect.xy) - (_PoolRect.zw - _PoolShape.x);
    return step(0.0, q.x) * step(0.0, q.y);
}

float2 RippleUV(float2 worldXY)
{
    float4 c = mul(_PoolRippleVP, float4(worldXY, 0.0, 1.0));
    return c.xy / c.w * 0.5 + 0.5;
}

// Ripple height and its screen-independent slope. xy = slope, z = height.
float3 SampleRipple(float2 worldXY)
{
    if (_PoolRippleParams.z < 0.5) return 0;
    float2 uv = RippleUV(worldXY);
    float e = _PoolRippleParams.x;
    float h  = SAMPLE_TEXTURE2D(_PoolRippleTex, sampler_PoolRippleTex, uv).r;
    float hx = SAMPLE_TEXTURE2D(_PoolRippleTex, sampler_PoolRippleTex, uv + float2(e, 0)).r;
    float hy = SAMPLE_TEXTURE2D(_PoolRippleTex, sampler_PoolRippleTex, uv + float2(0, e)).r;
    // Keep anything that leaks past the ripple camera's square from sampling clamped edge texels.
    float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
    return float3(hx - h, hy - h, h) * inside;
}

float PoolHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float PoolNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = PoolHash21(i);
    float b = PoolHash21(i + float2(1.0, 0.0));
    float c = PoolHash21(i + float2(0.0, 1.0));
    float d = PoolHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Cheap low-frequency drift so the floor shimmers even where nothing has disturbed the water.
float2 PoolWobble(float2 p, float t)
{
    return float2(sin(p.y * 1.7 + t * 1.3) + sin(p.x * 0.9 - t * 0.7),
                  cos(p.x * 1.5 + t * 1.1) + cos(p.y * 0.8 + t * 0.9)) * 0.5;
}

#endif
