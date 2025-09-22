using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Custom Rule Tile", menuName = "Tiles/Rule Tile")]
public class CustomRuleTile : HexagonalRuleTile, ICustomTile
{
    [SerializeField] private TileType type;

    public TileType Type { get => type; set => type = value; }
}
