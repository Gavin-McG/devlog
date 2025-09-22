using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexGrid : MonoBehaviour
{
    [SerializeField] Tilemap map;
    [SerializeField] Tile changeTile;

    // Start is called before the first frame update
    void Start()
    {
        for (int i=-5; i<=5; ++i)
        {
            map.SetTile(new Vector3Int(i, 0, 0), changeTile);
            map.SetTile(new Vector3Int(0, i, 0), changeTile);
        }
    }
}
