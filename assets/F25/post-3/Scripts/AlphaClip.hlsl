#ifndef ALPHA_CLIP_INCLUDED
#define ALPHA_CLIP_INCLUDED

void AlphaClip_float(float4 color_in, out float4 color_out)
{
    // If alpha is 0, clip the pixel (discard it)
    clip(color_in.a - 0.0001);
    color_out = color_in;
}

#endif