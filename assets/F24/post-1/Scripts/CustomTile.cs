using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//Custom Tile used to store needed information for each tile


//types of tiles needed for terrain info
[System.Serializable]
public enum TileType
{
    Empty,
    Full,
}


[CreateAssetMenu(fileName = "New Custom Tile", menuName = "Tiles/Custom Tile")]
public class CustomTile : Tile
{
    [Space(20)] 
    public TileType type;
}