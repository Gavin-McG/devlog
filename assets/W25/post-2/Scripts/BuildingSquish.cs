using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingSquish : MonoBehaviour
{
    [SerializeField] public Vector3Int offsetCoord;
    [SerializeField] Vector3 scale = Vector3.one;
    [SerializeField] Vector3 position = Vector3.zero;
    [SerializeField] bool destroy;

    Animator animator;
    BuildingManager bm;
    Tilemap objectMap;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        bm = BuildingManager.Instance;
        objectMap = bm.objectMap;
    }

    private void Update()
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, scale);
        objectMap.SetTransformMatrix(offsetCoord, matrix);

        if (destroy)
        {
            Destroy(gameObject);
            HoverManager.Instance.removeSquish(offsetCoord);
        }
    }

    public void StartClick()
    {
        animator.SetTrigger("Squish");
    }

    public void StartHover()
    {
        animator.SetTrigger("Hover");
    }

    public void StartBuild()
    {
        animator.SetTrigger("Build");
    }
}
