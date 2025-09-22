using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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

    [Space(10)]

    //current build mode state
    [SerializeField] EditMode _editMode = EditMode.None;
    [HideInInspector] public EditMode editMode { get { return _editMode; } }

    [SerializeField] public Structure activeStructure;

    //other managers
    ResourceManager rm;
    PreviewManager pm;


    //dictionaries to track buildings
    //all Vector3Int of dictionaries are stored in Offset coordinates

    //tileDictionary tracks what Building each tile correlates to.
    //When placing a building all tiles the building fills should have their value set
    Dictionary<Vector3Int, Building> tileDictionary = new Dictionary<Vector3Int, Building>();
    //buildingDictionary tracks the tiles that are possessed by each building.
    //When placing a building all tiles the building fills should be added into its value list
    Dictionary<Building, List<Vector3Int>> buildingDictionary = new Dictionary<Building, List<Vector3Int>>();
    //typeDictionary manages a list of all buildings of each type
    Dictionary<BuildingType, List<Building>> typeDictionary = new Dictionary<BuildingType, List<Building>>();


    //store whether the mouse is on a UI element
    bool isOverUI = false;

    //UI events to change edit mode (might move to UI scripts later)
    public static UnityEvent<Structure> EnableBuilding = new UnityEvent<Structure>();
    public static UnityEvent EnableDeleting = new UnityEvent();
    public static UnityEvent DisableEditing = new UnityEvent();

    //events to mark building interactions
    public static UnityEvent<Building, Vector3Int> BuildingClicked = new UnityEvent<Building, Vector3Int>();
    public static UnityEvent<EnvironmentTile, Vector3Int> EnvironmentClicked = new UnityEvent<EnvironmentTile, Vector3Int>();
    public static UnityEvent<Building, Vector3Int> BuildingPlaced = new UnityEvent<Building, Vector3Int>();
    public static UnityEvent<Building, Vector3Int> BuildingDeleted = new UnityEvent<Building, Vector3Int>();

    public static UnityEvent FailedPlacement = new UnityEvent();
    public static UnityEvent FailedDestroy = new UnityEvent();

    private void Start()
    {
        rm = GetComponent<ResourceManager>();
        pm = GetComponent<PreviewManager>();

        RunBuildingSpawning();
    }

    private void OnEnable()
    {
        //listen for editMode updates
        EnableBuilding.AddListener(SetBuildMode);
        EnableDeleting.AddListener(SetDeleteMode);
        DisableEditing.AddListener(SetNoneMode);

        //listen for interactions with the tilemap
        CameraManager.mouseClick.AddListener(Interract);

        SetEditKeyword(_editMode);
    }

    private void OnDisable()
    {
        //remove event listeners
        EnableBuilding.RemoveListener(SetBuildMode);
        EnableDeleting.RemoveListener(SetDeleteMode);
        DisableEditing.RemoveListener(SetNoneMode);

        //listen for interactions with the tilemap
        CameraManager.mouseClick.RemoveListener(Interract);
    }

    private void OnValidate()
    {
        SetEditKeyword(_editMode);
    }

    void Update()
    {
        isOverUI = EventSystem.current.IsPointerOverGameObject();
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
    public bool IsGroundTile(Vector3Int offsetCoord)
    {
        //get ground tile
        BasicTile groundTile = groundMap.GetTile<BasicTile>(offsetCoord);

        //check that ground tile is full
        return groundTile != null && groundTile.type == TileType.Full;
    }

    //check if object tile is empty
    public bool IsTileEmpty(Vector3Int offsetCoord)
    {
        if (GetBuilding(offsetCoord) != null) return false;
        if (objectMap.GetTile(offsetCoord) is EnvironmentTile) return false;
        return true;
    }



    //check whether a given offset position is a valid place for a tile to be set
    public bool IsValidPlacement(Vector3Int offsetCoord)
    {
        //get tiles in the offset position
        BasicTile groundTile = groundMap.GetTile<BasicTile>(offsetCoord);
        BasicTile objectTile = objectMap.GetTile<BasicTile>(offsetCoord);

        //get tile type, default empty types
        TileType groundType = groundTile != null ? groundTile.type : TileType.Empty;
        TileType objectType = objectTile != null ? objectTile.type : TileType.Empty;

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



    void Interract()
    {
        if (isOverUI) return;

        Vector3Int offsetCoord = GetSelectedOffset();

        switch (_editMode)
        {
            case EditMode.None:
                ClickTile(offsetCoord);
                break;
            case EditMode.Build:
                PlaceBuilding(offsetCoord, activeStructure);
                break;
            case EditMode.Delete:
                DeleteBuilding(offsetCoord);
                break;
        }
    }

    //Try to click an object at given offsetCoords
    //return true if there's an object to click.
    public bool ClickTile(Vector3Int offsetCoord)
    {
        //try to click building
        Building building = GetBuilding(offsetCoord);
        if (building != null)
        {
            BuildingClicked.Invoke(building, offsetCoord);
            return true;
        }

        //try to click environmental tile
        TileBase tile = objectMap.GetTile(offsetCoord);
        if (tile is EnvironmentTile envTile)
        {
            EnvironmentClicked.Invoke(envTile, offsetCoord); 
            return true;
        }

        //nothing to click
        return false;
    }

    //place a structure at given offsetCoords
    //return true is placement is successful
    public bool PlaceBuilding(Vector3Int offsetCoord, Structure structure, bool placeEvent = true, bool charge = true)
    {
        //check if structure placement isn't valid
        if (!IsValidStructure(offsetCoord, structure))
        {
            //could not place building
            FailedPlacement.Invoke();
            return false;
        }

        //check resources
        if (charge && !rm.CanAfford(structure.buildingObject.GetComponent<Building>().buildCost))
        {
            //could not afford building
            FailedPlacement.Invoke();
            return false;
        }

        //create new building
        Vector3 buildingPos = groundMap.CellToWorld(offsetCoord);
        GameObject buildingObject = Instantiate(structure.buildingObject, buildingPos, Quaternion.identity);
        buildingObject.transform.parent = transform;
        Building newBuilding = buildingObject.GetComponent<Building>();

        //charge for building
        if (charge)
        {
            rm.Charge(newBuilding.buildCost);
        }

        //add new building to type dictionary
        if (!typeDictionary.ContainsKey(newBuilding.type))
        {
            //add new key if necessary of buildingType
            typeDictionary.Add(newBuilding.type, new List<Building>());
        }
        typeDictionary[newBuilding.type].Add(newBuilding);

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
            BuildingPlaced.Invoke(newBuilding, offsetCoord);
        }

        return true;
    }

    //delete a structure at given offsetCoords
    //return true is deletion is successful
    public bool DeleteBuilding(Vector3Int offsetCoord, bool deleteEvent = true, bool refund = true)
    {
        //get building from tile
        Building building = GetBuilding(offsetCoord);

        if (building == null || !building.canDestroy)
        {
            //no building to destroy
            FailedDestroy.Invoke();
            return false;
        }

        //refund building
        if (refund)
        {
            rm.Refund(building.buildCost);
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

        //destroy building object
        Destroy(building.gameObject);

        //run building delete event
        if (deleteEvent)
        {
            BuildingDeleted.Invoke(building, offsetCoord);
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
    public List<BasicTile> GetBuildingTiles(Building building)
    {
        //get coords of all tiles in building
        List<Vector3Int> offsetCoords = GetBuildingOffsets(building);

        if (offsetCoords == null) return null;

        List<BasicTile> tiles = new List<BasicTile>();
        foreach (Vector3Int offsetCoord in offsetCoords)
        {
            //fetch custom tile from groundMap
            tiles.Add(objectMap.GetTile<BasicTile>(offsetCoord));
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




    //look through entire map and place default buildings
    public void RunBuildingSpawning()
    {
        BoundsInt bounds = objectMap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tileOffset = new Vector3Int(x, y, 0);

                // Get the tile at the current position
                TileBase tile = objectMap.GetTile(tileOffset);

                // Check if the tile is a BuildingTile
                if (tile is BuildingSpawnTile spawnTile)
                {
                    objectMap.SetTile(tileOffset, null);
                    if (!PlaceBuilding(tileOffset, spawnTile.building.currentStructure, false, false))
                    {
                        Debug.LogError("Could not place " + spawnTile.building.name + " at offset coordinate " + tileOffset);
                    }
                }
            }
        }
    }
}
