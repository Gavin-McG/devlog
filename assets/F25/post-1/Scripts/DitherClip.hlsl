#ifndef DITHER_CLIP_INCLUDED
#define DITHER_CLIP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

void DitherClip_float(float4 InColor, float2 ScreenPosition, float DitherStrength, bool Dither, out float4 OutColor)
{
    OutColor = InColor;

    //return if no Dithering
    if (!Dither) return;
    
    //Normalize screen position to pixels (removes subpixel jittering artifacts)
    float2 pixelPos = floor(ScreenPosition);

    //Generate noise value from screen position
    float noise = InterleavedGradientNoise(pixelPos, 0);

    //Compare noise against threshold
    float alpha = DitherStrength - noise;

    //Perform dithering alpha clip
    clip(InColor.w!=0 ? alpha : -1);
}

#endif