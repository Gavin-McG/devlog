using System.Collections;
using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;

//defines the structure of tiles for a type of building

[System.Serializable]
public struct StructurePiece
{
    public CustomTile tile;
    public Vector3Int cubicCoord;
}

[CreateAssetMenu(fileName = "Structure", menuName = "ScriptableObjects/Structure", order = 1)]
public class Structure : ScriptableObject
{
    public BuildingType buildingType;
    public StructurePiece[] pieces;
}
