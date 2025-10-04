using System;
using UnityEngine;

public class SpritePlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Header("Placement")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private float width = 1;


    private void OnValidate()
    {
        Camera cam = Camera.main;
        if (!cam || !spriteTransform) return;
        
        //set transform rotation
        Vector3 faceDirection = cam.transform.forward;
        Vector3 flatDirection = Vector3.ProjectOnPlane(faceDirection, Vector3.up);
        Quaternion faceRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        spriteTransform.rotation = faceRotation;
        
        //determine height
        Sprite sprite = spriteRenderer.sprite;
        Vector3 spriteSize = sprite.bounds.size;
        float aspectRatio = spriteSize.y / spriteSize.x;
        float height = aspectRatio * width / Vector3.Cross(Vector2.up, faceDirection).magnitude;
        
        //set position
        Vector3 basePosition = transform.position + offset;
        Vector3 centerPosition = basePosition + Vector3.up * height * 0.5f;
        spriteRenderer.transform.position = centerPosition;
        
        //set scale
        Vector3 spriteScale = new Vector3(width/spriteSize.x, height/spriteSize.y, 1);
        spriteRenderer.transform.localScale = spriteScale;
    }
}
