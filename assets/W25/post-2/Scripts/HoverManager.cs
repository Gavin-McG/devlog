using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverManager : MonoBehaviour
{
    public static HoverManager Instance { get; private set; }

    [SerializeField] BuildingSquish squishPrefab;
    [SerializeField] Transform squishContainer;

    BuildingManager bm;

    [HideInInspector] public Vector3Int hoverOffset = Vector3Int.zero;
    Dictionary<Vector3, BuildingSquish> squishes = new();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        bm = BuildingManager.Instance;
    }

    private void Update()
    {
        //check for tile change
        Vector3Int newHoverOffset = bm.GetSelectedOffset();
        if (newHoverOffset != hoverOffset)
        {
            HoverTile(newHoverOffset);
            hoverOffset = newHoverOffset;
        }
    }

    //run when the mouse changes tile
    public void HoverTile(Vector3Int offsetCoord)
    {
        //check for same building
        Building building1 = bm.GetBuilding(offsetCoord);
        Building building2 = bm.GetBuilding(hoverOffset);
        if (building1 != null && building1 == building2) return;

        if (squishes.ContainsKey(offsetCoord))
        {
            //hover existing tile
            squishes[offsetCoord].StartHover();
        }
        else if (bm.GetBuilding(offsetCoord) != null)
        {
            Building building = bm.GetBuilding(offsetCoord);
            if (squishes.ContainsKey(building.offsetCoord))
            {
                //hover existing building
                squishes[building.offsetCoord].StartHover();
            }
            else
            {
                //start new squish for building
                BuildingSquish newSquish = StartNewHover(building);
                newSquish.StartHover();
            }
        }
        else if (bm.IsEnvironmentalTile(offsetCoord))
        {
            //start new squish for environment tile
            BuildingSquish newSquish = StartNewHover(offsetCoord);
            newSquish.StartHover();
        }
    }

    //create a new squish object for a certain coord
    BuildingSquish StartNewHover(Vector3Int offsetCoord)
    {
        Building building = bm.GetBuilding(offsetCoord);
        if (building != null || bm.IsEnvironmentalTile(offsetCoord))
        {
            BuildingSquish newSquish = Instantiate(squishPrefab, squishContainer);
            newSquish.offsetCoord = offsetCoord;
            squishes.TryAdd(offsetCoord, newSquish);
            return newSquish;
        }
        else return null;
    }

    public BuildingSquish StartNewHover(Building building)
    {
        BuildingSquish newSquish = Instantiate(squishPrefab, squishContainer);
        newSquish.offsetCoord = building.offsetCoord;
        squishes.TryAdd(building.offsetCoord, newSquish);
        return newSquish;
    }

    //remove a squish object
    public void removeSquish(Vector3Int offsetCoord)
    {
        squishes.Remove(offsetCoord);
    }

    //click a building
    public void ClickTile(Building building)
    {
        if (squishes.ContainsKey(building.offsetCoord))
        {
            squishes[building.offsetCoord].StartClick();
        }
        else
        {
            BuildingSquish newSquish = StartNewHover(building.offsetCoord);
            if (newSquish != null) newSquish.StartClick();
        }
    }

    //click a tile
    public void ClickTile(Vector3Int offsetCoord)
    {
        if (squishes.ContainsKey(offsetCoord))
        {
            squishes[offsetCoord].StartClick();
        }
        else
        {
            BuildingSquish newSquish = StartNewHover(offsetCoord);
            if (newSquish != null) newSquish.StartClick();
        }
    }

    public void BuildBounce(Building building)
    {
        if (squishes.ContainsKey(building.offsetCoord))
        {
            squishes[building.offsetCoord].StartBuild();
        }
        else
        {
            BuildingSquish newSquish = StartNewHover(building);
            if (newSquish != null) newSquish.StartBuild();
        }
    }

    public void BuildBounce(Vector3Int offsetCoord)
    {
        if (squishes.ContainsKey(offsetCoord))
        {
            squishes[offsetCoord].StartBuild();
        }
        else
        {
            BuildingSquish newSquish = StartNewHover(offsetCoord);
            if (newSquish != null) newSquish.StartBuild();
        }
    }
}
