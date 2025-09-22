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

    public static UnityEvent FailedPlacement = new UnityEvent();
    public static UnityEvent FailedDestroy = new UnityEvent();


    void Awake()
    {
        //initialize dictionaries
        tileDictionary = new Dictionary<Vector3Int, Building>();
        buildingDictionary = new Dictionary<Building, List<Vector3Int>>();
        typeDictionary = new Dictionary<BuildingType, List<Building>>();

        coloredOffsets = new List<Vector3Int>();

        SetEditKeyword(_editMode);
    }

    private void OnEnable()
    {
        //listen for editMode updates
        EnableBuilding.AddListener(SetBuildMode);
        EnableDeleting.AddListener(SetDeleteMode);
        DisableEditing.AddListener(SetNoneMode);

        //listen for interactions with the tilemap
        CameraManager.mouseClick.AddListener(Interract);
    }

    private void OnDisable()
    {
        //remove event listeners
        EnableBuilding.RemoveListener(SetBuildMode);
        EnableDeleting.RemoveListener(SetDeleteMode);
        DisableEditing.RemoveListener(SetNoneMode);

        //listen for interactions with the tilemap
        CameraManager.mouseClick.RemoveListener(Interract);

        ClearPreviews();
    }

    private void OnValidate()
    {
        SetEditKeyword(_editMode);
    }

    void Update()
    {
        ClearPreviews();
        DisplayPreviews();
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

        Vector3Int offsetCoord = GetSelectedOffset();

        if (!IsGroundTile(offsetCoord)) return;

        switch (_editMode)
        {
            case EditMode.None:
                NonePreview(offsetCoord);
                break;
            case EditMode.Delete:
                DeletePreview(offsetCoord);
                break;
            case EditMode.Build:
                BuildPreview(offsetCoord, activeStructure);
                break;
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
            ColorTile(previewMap, newOffsetCoord, isSructureValid ? validColor : invalidColor);
            ColorTile(groundMap, newOffsetCoord, isTileValid ? validColor : invalidColor);
        }
    }

    //Display preview of building to be deleated
    void DeletePreview(Vector3Int offsetCoord)
    {
        //get selected building
        Building building = GetBuilding(offsetCoord);

        if (building != null)
        {
            List<Vector3Int> offsetCoords = GetBuildingOffsets(building);

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
        Building building = GetBuilding(offsetCoord);

        if (building != null)
        {
            List<Vector3Int> offsetCoords = GetBuildingOffsets(building);

            //highlight each tile of building
            foreach (Vector3Int tileOffset in offsetCoords)
            {
                //color tiles
                ColorTile(objectMap, tileOffset, highlightColor);
            }
        }
        else
        {
            //color tile
            ColorTile(groundMap, offsetCoord, highlightColor);
        }
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




    //set the sprite shader alpha mode
    void SetEditKeyword(EditMode mode)
    {
        Shader.DisableKeyword("BUILD");
        Shader.DisableKeyword("DELETE");
        Shader.DisableKeyword("NONE");

        Shader.EnableKeyword(mode.ToString().ToUpper());
    }




    //Turn on Build mode, required provided structure
    public void SetBuildMode(Structure structure)
    {
        _editMode = EditMode.Build;
        activeStructure = structure;
        SetEditKeyword(_editMode);
    }

    //Turn on Delete mode
    public void SetDeleteMode()
    {
        _editMode = EditMode.Delete;
        SetEditKeyword(_editMode);
    }

    //disable editing modes
    public void SetNoneMode()
    {
        _editMode = EditMode.None;
        SetEditKeyword(_editMode);
    }




    //check if a ground tile exists at a given location
    bool IsGroundTile(Vector3Int offsetCoord)
    {
        //get ground tile
        CustomTile groundTile = groundMap.GetTile<CustomTile>(offsetCoord);

        //check that ground tile is full
        return groundTile != null && groundTile.type == TileType.Full;
    }




    //check whether a given offset position is a valid place for a tile to be set
    bool IsValidPlacement(Vector3Int offsetCoord)
    {
        //get tiles in the offset position
        CustomTile groundTile = groundMap.GetTile<CustomTile>(offsetCoord);
        CustomTile objectTile = objectMap.GetTile<CustomTile>(offsetCoord);

        //get tile type, default empty types
        TileType groundType = groundTile != null ? groundTile.type : TileType.Empty;
        TileType objectType = objectTile != null ? objectTile.type : TileType.Empty;

        //placeable is ground is full and object is empty
        return groundType==TileType.Full && objectType==TileType.Empty;
    } 




    //check whether a given offset position is a valid place for a structure to be set
    bool IsValidStructure(Vector3Int offsetCoord, Structure structure)
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



    void Interract()
    {
        Vector3Int offsetCoord = GetSelectedOffset();

        switch (_editMode)
        {
            case EditMode.None:

                break;
            case EditMode.Build:
                PlaceBuilding(offsetCoord, activeStructure);
                break;
            case EditMode.Delete:
                DeleteBuilding(offsetCoord);
                break;
        }
    }



    //place a structure at given offsetCoords
    //return true is placement is successful
    public bool PlaceBuilding(Vector3Int offsetCoord, Structure structure, bool placeEvent = true)
    {
        //check if structure placement isn't valid
        if (!IsValidStructure(offsetCoord, structure))
        {
            //could not place building
            FailedPlacement.Invoke();
            return false;
        }

        //create new building
        Building newBuilding = Building.GetBuilding(structure.buildingType);

        //add new building to type dictionary
        if (!typeDictionary.ContainsKey(structure.buildingType))
        {
            //add new key if necessary of buildingType
            typeDictionary.Add(structure.buildingType, new List<Building>());
        }
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
            tileDictionary.Add(newOffsetCoord, newBuilding);

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
    public bool DeleteBuilding(Vector3Int offsetCoord, bool deleteEvent = true)
    {
        //get building from tile
        Building building = GetBuilding(offsetCoord);

        if (building == null)
        {
            //no building to destroy
            FailedDestroy.Invoke();
            return false;
        }

        //remove building from typeDictionary
        typeDictionary[building.type].Remove(building);

        //get offset coordinates of tiles in building
        List<Vector3Int> offsetCoords = GetBuildingOffsets(building);

        //remove from buildingDictionary
        buildingDictionary.Remove(building);

        //loop through tiles in building
        foreach (Vector3Int tileOffset in offsetCoords)
        {
            //delete tile from objectMap
            objectMap.SetTile(tileOffset, null);

            //remove tile from tileDictionary
            tileDictionary.Remove(tileOffset);
        }

        //run building delete event
        if (deleteEvent)
        {
            BuildingDeleted.Invoke(building);
        }

        return true;
    }




    //return the coordinates of all tiles owned by a building
    public List<Vector3Int> GetBuildingOffsets(Building building)
    {
        if (building == null) return null;

        //fetch building tiles from building dictionary
        if (buildingDictionary.ContainsKey(building))
        {
            return buildingDictionary[building];
        }

        //building doesn't exist in map
        return null;
    }

    //return all the tiles owned by a building
    public List<CustomTile> GetBuildingTiles(Building building)
    {
        //get coords of all tiles in building
        List<Vector3Int> offsetCoords = GetBuildingOffsets(building);

        if (offsetCoords == null) return null;

        List<CustomTile> tiles = new List<CustomTile>();
        foreach (Vector3Int offsetCoord in offsetCoords)
        {
            //fetch custom tile from groundMap
            tiles.Add(objectMap.GetTile<CustomTile>(offsetCoord));
        }

        return tiles;
    }

    //get building from tile offset coordinate
    public Building GetBuilding(Vector3Int offsetCoord)
    {
        if (tileDictionary.ContainsKey(offsetCoord))
        {
            //fetch building from tile dictionary
            return tileDictionary[offsetCoord];
        }

        //tile doesn't exist in any building
        return null;
    }

    //get all buildings of a certain building type
    public List<Building> GetBuildingsOfType(BuildingType type)
    {
        if (!typeDictionary.ContainsKey(type))
        {
            //add new type to dictionary
            typeDictionary.Add(type, new List<Building>());
        }

        //no buildings exist of that type
        return typeDictionary[type];
    }
}
