using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PreviewManager : MonoBehaviour
{
    [SerializeField] Tilemap groundMap;
    [SerializeField] Tilemap objectMap;
    [SerializeField] Tilemap previewMap;

    [Space(10)]

    //colors to reresent valid/invalid placement
    [SerializeField] Color validColor = Color.green;
    [SerializeField] Color invalidColor = Color.red;
    //color for too expensive buildings
    [SerializeField] Color expensiveColor = Color.red;
    //color for deletion highlight
    [SerializeField] Color deleteColor = Color.red;
    //color for non-editing highlight
    [SerializeField] Color highlightColor = Color.blue;

    BuildingManager bm;
    ResourceManager rm;

    //list to track colored tiles in edit mode
    List<Vector3Int> coloredOffsets = new List<Vector3Int>();

    private void Start()
    {
        bm = GetComponent<BuildingManager>();
        rm = GetComponent<ResourceManager>();
    }

    private void OnDisable()
    {
        ClearPreviews();
    }

    private void Update()
    {
        ClearPreviews();
        DisplayPreviews();
    }


    //reset all colored tiles
    void ResetColors()
    {
        foreach (Vector3Int offset in coloredOffsets)
        {
            //clear color of offset
            groundMap.SetColor(offset, Color.white);
            objectMap.SetColor(offset, Color.white);
            previewMap.SetColor(offset, Color.white);
        }

        //reset color list
        coloredOffsets.Clear();
    }


    //remove all build mode previews
    void ClearPreviews()
    {
        ResetColors();

        //null check for Destroy call
        if (previewMap != null)
        {
            previewMap.ClearAllTiles();
        }
    }

    //display building system highlights
    void DisplayPreviews()
    {
        if (!Application.isFocused) return;

        Vector3Int offsetCoord = bm.GetSelectedOffset();

        if (!bm.IsGroundTile(offsetCoord)) return;

        switch (bm.editMode)
        {
            case EditMode.None:
                NonePreview(offsetCoord);
                break;
            case EditMode.Delete:
                DeletePreview(offsetCoord);
                break;
            case EditMode.Build:
                BuildPreview(offsetCoord, bm.activeStructure);
                break;
        }
    }

    void ColorTile(Tilemap map, Vector3Int offsetCoord, Color color)
    {
        //color tile
        map.SetTileFlags(offsetCoord, TileFlags.None);
        map.SetColor(offsetCoord, color);

        //add to list of colored tiles
        coloredOffsets.Add(offsetCoord);
    }


    //Dsiplay preview of structure to be placed, changes color based on placement validity
    void BuildPreview(Vector3Int offsetCoord, Structure structure)
    {
        //determine color for structure highlight
        bool isSructureValid = bm.IsValidStructure(offsetCoord, structure);

        //determine if building is afforded
        bool isAfforded = rm.CanAfford(structure.buildingObject.GetComponent<Building>().buildCost);
        Color newColor = isAfforded ? validColor : expensiveColor;

        Vector3Int cubicCoord = HexUtils.OffsetToCubic(offsetCoord);
        foreach (StructurePiece piece in structure.pieces)
        {
            //calculate coordinate
            Vector3Int newCubicCoord = cubicCoord + piece.cubicCoord;
            Vector3Int newOffsetCoord = HexUtils.CubicToOffset(newCubicCoord);

            //set tile
            previewMap.SetTile(newOffsetCoord, piece.tile);

            //determine color for tile highlight
            bool isTileValid = bm.IsValidPlacement(newOffsetCoord);

            //color tiles
            ColorTile(previewMap, newOffsetCoord, isSructureValid ? newColor : invalidColor);
            ColorTile(groundMap, newOffsetCoord, isTileValid ? newColor : invalidColor);
        }
    }

    //Display preview of building to be deleated
    void DeletePreview(Vector3Int offsetCoord)
    {
        //get selected building
        Building building = bm.GetBuilding(offsetCoord);

        if (building != null)
        {
            List<Vector3Int> offsetCoords = bm.GetBuildingOffsets(building);

            //highlight each tile of building
            foreach (Vector3Int tileOffset in offsetCoords)
            {
                //color tiles
                ColorTile(objectMap, tileOffset, deleteColor);
                ColorTile(groundMap, tileOffset, deleteColor);
            }
        }
        else
        {
            //color tile
            ColorTile(groundMap, offsetCoord, deleteColor);
        }
    }

    //Display highlight when not in editing mode
    void NonePreview(Vector3Int offsetCoord)
    {
        //get highlighted building
        Building building = bm.GetBuilding(offsetCoord);

        if (building != null)
        {
            List<Vector3Int> offsetCoords = bm.GetBuildingOffsets(building);

            //highlight each tile of building
            foreach (Vector3Int tileOffset in offsetCoords)
            {
                //color tiles
                ColorTile(objectMap, tileOffset, highlightColor);
            }

            return;
        }

        //get highlighted environmental Tile
        TileBase tile = objectMap.GetTile(offsetCoord);
        if (tile is EnvironmentTile envTile)
        {
            ColorTile(objectMap, offsetCoord, highlightColor);

            return;
        }

        //color tile
        ColorTile(groundMap, offsetCoord, highlightColor);
    }
}
