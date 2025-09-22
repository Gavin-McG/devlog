using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

//editing modes
public enum EditMode
{
    None,
    Build,
    Delete
}

//The BuildingManager class handles the placement and deletion of buildings on a
//tilemap, using different editing modes.It interacts with tilemaps to show previews
//of valid/invalid placements, and manages multiple dictionaries to track tile and
//building data. The script also provides an event-driven system to handle UI
//interactions for switching between building and deleting modes.

public class BuildingManager : MonoBehaviour
{
    //tilemaps used for building processes
    [SerializeField] Tilemap groundMap;
    [SerializeField] Tilemap objectMap;
    [SerializeField] Tilemap previewMap;

    [Space(10)]

    //colors to reresent valid/invalid placement
    [SerializeField] Color validColor = Color.green;
    [SerializeField] Color invalidColor = Color.red;
    //color for deletion highlight
    [SerializeField] Color deleteColor = Color.red;
    //color for non-editing highlight
    [SerializeField] Color highlightColor = Color.blue;

    [Space(10)]

    //current build mode state
    [SerializeField] EditMode _editMode = EditMode.None;
    [SerializeField] Structure activeStructure;

    //readonly parameter for _editMode
    [HideInInspector] public EditMode editMode {get {return _editMode;}}

    //dictionaries to track buildings
    //all Vector3Int of dictionaries are stored in Offset coordinates

    //tileDictionary tracks what Building each tile correlates to.
    //When placing a building all tiles the building fills should have their value set
    Dictionary<Vector3Int, Building> tileDictionary;
    //buildingDictionary tracks the tiles that are possessed by each building.
    //When placing a building all tiles the building fills should be added into its value list
    Dictionary<Building, List<Vector3Int>> buildingDictionary;
    //typeDictionary manages a list of all buildings of each type
    Dictionary<BuildingType, List<Building>> typeDictionary;

    //list to track colored tiles in edit mode
    List<Vector3Int> coloredOffsets;


    //UI events to change edit mode (might move to UI scripts later)
    public static UnityEvent<Structure> EnableBuilding = new UnityEvent<Structure>();
    public static UnityEvent EnableDeleting = new UnityEvent();
    public static UnityEvent DisableEditing = new UnityEvent();

    //events to mark building changes
    public static UnityEvent<Building> BuildingPlaced = new UnityEvent<Building>();
    public static UnityEvent<Building> BuildingDeleted = new UnityEvent<Building>();


    void Awake()
    {
        //initialize dictionaries
        tileDictionary = new Dictionary<Vector3Int, Building>();
        buildingDictionary = new Dictionary<Building, List<Vector3Int>>();
        typeDictionary = new Dictionary<BuildingType, List<Building>>();

        coloredOffsets = new List<Vector3Int>();
    }

    private void OnEnable()
    {
        //listen for editMode updates
        EnableBuilding.AddListener(SetBuildMode);
        EnableDeleting.AddListener(SetDeleteMode);
        DisableEditing.AddListener(SetNoneMode);
    }

    private void OnDisable()
    {
        //remove event listeners
        EnableBuilding.RemoveListener(SetBuildMode);
        EnableDeleting.RemoveListener(SetDeleteMode);
        DisableEditing.RemoveListener(SetNoneMode);
    }

    void Update()
    {
        Vector3Int offsetCoord = GetSelectedOffset();
        if (_editMode == EditMode.Build)
        {
            BuildPreview(offsetCoord, activeStructure);

            if (Input.GetMouseButtonDown(0))
            {
                PlaceBuilding(offsetCoord, activeStructure);
            }
        }
        else if (_editMode == EditMode.Delete)
        {
            DeletePreview(offsetCoord);

            if (Input.GetMouseButtonDown(0))
            {
                DeleteBuilding(offsetCoord);
            }
        }
        else
        {
            NonePreview(offsetCoord);
        }
    }




    //Get offset position of mouse position
    public Vector3Int GetSelectedOffset()
    {
        //set world position of mouse
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0;

        //convert world position to offset position
        Vector3Int offsetCoord = groundMap.WorldToCell(worldPosition);
        
        return offsetCoord;
    }


    //Dsiplay preview of structure to be placed, changes color based on placement validity
    void BuildPreview(Vector3Int offsetCoord, Structure structure)
    {
        if (!Application.isFocused) return;

        //clear previous preview
        ResetColors();
        previewMap.ClearAllTiles();

        //determine color for structure highlight
        bool isSructureValid = IsValidStructure(offsetCoord, structure);

        Vector3Int cubicCoord = HexUtils.OffsetToCubic(offsetCoord);
        foreach (StructurePiece piece in structure.pieces)
        {
            //calculate coordinate
            Vector3Int newCubicCoord = cubicCoord + piece.cubicCoord;
            Vector3Int newOffsetCoord = HexUtils.CubicToOffset(newCubicCoord);

            //set tile
            previewMap.SetTile(newOffsetCoord, piece.tile);

            //determine color for tile highlight
            bool isTileValid = IsValidPlacement(newOffsetCoord);

            //color tiles
            previewMap.SetTileFlags(newOffsetCoord, TileFlags.None);
            previewMap.SetColor(newOffsetCoord, isSructureValid ? validColor : invalidColor);

            groundMap.SetTileFlags(newOffsetCoord, TileFlags.None);
            groundMap.SetColor(newOffsetCoord, isTileValid ? validColor : invalidColor);
        
            //add to list of colored tiles
            coloredOffsets.Add(newOffsetCoord);
        }
    }

    //Display preview of building to be deleated
    private void DeletePreview(Vector3Int offsetCoord)
    {
        if (!Application.isFocused) return;

        //clear previous preview
        ResetColors();

        if (tileDictionary.ContainsKey(offsetCoord))
        {
            //get building
            Building building = tileDictionary[offsetCoord];

            //highlight each tile of building
            foreach (Vector3Int tileOffset in buildingDictionary[building])
            {
                //color tiles
                objectMap.SetTileFlags(tileOffset, TileFlags.None);
                objectMap.SetColor(tileOffset, deleteColor);

                groundMap.SetTileFlags(tileOffset, TileFlags.None);
                groundMap.SetColor(tileOffset, deleteColor);

                //add to list of colored tiles
                coloredOffsets.Add(tileOffset);
            }
        }
        else
        {
            //color tile
            groundMap.SetTileFlags(offsetCoord, TileFlags.None);
            groundMap.SetColor(offsetCoord, deleteColor);

            //add to list of colored tiles
            coloredOffsets.Add(offsetCoord);
        }
    }

    //Display highlight when not in editing mode
    public void NonePreview(Vector3Int offsetCoord)
    {
        if (!Application.isFocused) return;

        //clear previous preview
        ResetColors();

        //color tile
        groundMap.SetTileFlags(offsetCoord, TileFlags.None);
        groundMap.SetColor(offsetCoord, highlightColor);

        //add to list of colored tiles
        coloredOffsets.Add(offsetCoord);
    }


    //reset all colored tiles
    public void ResetColors()
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




    //Turn on Build mode, required provided structure
    public void SetBuildMode(Structure structure)
    {
        _editMode = EditMode.Build;
        activeStructure = structure;
        ResetColors();
    }

    //Turn on Delete mode
    public void SetDeleteMode()
    {
        _editMode = EditMode.Delete;
        previewMap.ClearAllTiles();
        ResetColors();
    }

    //disable editing modes
    public void SetNoneMode()
    {
        _editMode = EditMode.Build;
        previewMap.ClearAllTiles();
        ResetColors();
    }




    //check whether a given offset position is a valid place for a tile to be set
    public bool IsValidPlacement(Vector3Int offsetCoord)
    {
        //get tiles in the offset position
        TileBase groundTile = groundMap.GetTile(offsetCoord);
        TileBase objectTile = objectMap.GetTile(offsetCoord);

        //default empty types
        TileType groundType = TileType.Empty;
        TileType objectType = TileType.Empty;

        //retreive types of tiles if valid tile/not null
        if (groundTile is CustomTile groundHex) 
        {
            groundType = groundHex.type;
        }
        if (objectTile is CustomTile objectHex)
        {
            objectType = objectHex.type;
        }

        //placeable is ground is full and object is empty
        return groundType==TileType.Full && objectType==TileType.Empty;
    } 




    //check whether a given offset position is a valid place for a structure to be set
    public bool IsValidStructure(Vector3Int offsetCoord, Structure structure)
    {
        if (structure == null) return false;

        Vector3Int cubicCoord = HexUtils.OffsetToCubic(offsetCoord);
        foreach (StructurePiece piece in structure.pieces)
        {
            //calculate coordinate
            Vector3Int newCubicCoord = cubicCoord + piece.cubicCoord;
            Vector3Int newOffsetCoord = HexUtils.CubicToOffset(newCubicCoord);

            //check piece's valid placement
            if (!IsValidPlacement(newOffsetCoord)) return false;
        }

        return true;
    }




    //place a structure at given offsetCoords
    //return true is placement is successful
    public bool PlaceBuilding(Vector3Int offsetCoord, Structure structure, bool placeEvent = true)
    {
        //skip if structure placement isn't valid
        if (!IsValidStructure(offsetCoord, structure)) return false;

        //create new building
        Building newBuilding = Building.GetBuilding(structure.buildingType);

        //add new building to type dictionary
        if (!typeDictionary.ContainsKey(structure.buildingType))
            //add new key if necessary of buildingType
            typeDictionary.Add(structure.buildingType, new List<Building>());
        typeDictionary[structure.buildingType].Add(newBuilding);

        //add new building to building dictionary
        buildingDictionary.Add(newBuilding, new List<Vector3Int>());

        //place all tiles of structure
        Vector3Int cubicCoord = HexUtils.OffsetToCubic(offsetCoord);
        foreach (StructurePiece piece in structure.pieces)
        {
            //calculate coordinate
            Vector3Int newCubicCoord = cubicCoord + piece.cubicCoord;
            Vector3Int newOffsetCoord = HexUtils.CubicToOffset(newCubicCoord);

            //set tile
            objectMap.SetTile(newOffsetCoord, piece.tile);

            //put tile in tileDictionary
            if (!tileDictionary.ContainsKey(newOffsetCoord))
                //set new tile's value as newBuilding
                tileDictionary.Add(newOffsetCoord, newBuilding);
            else
                //set tile's value as newBuilding
                tileDictionary[newOffsetCoord] = newBuilding;

            //put tile in buildingDictionary
            buildingDictionary[newBuilding].Add(newOffsetCoord);
        }

        //run building place event
        if (placeEvent)
        {
            BuildingPlaced.Invoke(newBuilding);
        }

        return true;
    }




    //delete a structure at given offsetCoords
    //return true is deletion is successful
    public bool DeleteBuilding(Vector3Int offsetCoords, bool deleteEvent = true)
    {
        //get building from tile
        if (!tileDictionary.ContainsKey(offsetCoords)) return false;
        Building building = tileDictionary[offsetCoords];

        //remove building from typeDictionary
        typeDictionary[building.type].Remove(building);

        //loop through tiles in building
        foreach (Vector3Int tileOffset in buildingDictionary[building])
        {
            //delete tile from objectMap
            objectMap.SetTile(tileOffset, null);

            //remove tile from tileDictionary
            tileDictionary.Remove(tileOffset);
        }

        //remove from buildingDictionary
        buildingDictionary.Remove(building);

        //run building delete event
        if (deleteEvent)
        {
            BuildingDeleted.Invoke(building);
        }

        return true;
    }
}
