using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Material material;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("_ScreenX", transform.position.x);
        material.SetFloat("_ScreenY", transform.position.y);
        material.SetFloat("_ScreenHeight", cam.orthographicSize*2);
        material.SetFloat("_ScreenWidth", cam.orthographicSize*2 * cam.aspect);

        Graphics.Blit(source, destination, material);
    }
}
