using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;

public class RangeManager : MonoBehaviour
{
    [SerializeField] BuildingManager bm;
    [SerializeField] Tilemap rangeMap;
    [SerializeField] TileBase highlightTile;

    private void OnEnable()
    {
        BuildingManager.BuildingPlaced.AddListener(PlaceRange);
        BuildingManager.BuildingDeleted.AddListener(RemoveRange);

        //listen for editMode updates
        BuildingManager.EnableBuilding.AddListener(EnableRange);
        BuildingManager.EnableDeleting.AddListener(EnableRange);
        BuildingManager.DisableEditing.AddListener(DisableRange);
    }

    private void OnDisable()
    {
        BuildingManager.BuildingPlaced.RemoveListener(PlaceRange);
        BuildingManager.BuildingDeleted.RemoveListener(RemoveRange);

        BuildingManager.EnableBuilding.RemoveListener(EnableRange);
        BuildingManager.EnableDeleting.RemoveListener(EnableRange);
        BuildingManager.DisableEditing.RemoveListener(DisableRange);
    }


    void PlaceRange(Building building, Vector3Int offsetCoord)
    {
        //get building range
        int range = 0;
        if (building is MainTower mainTower)
        {
            range = mainTower.buildRange;
        }
        else if (building is WizardTower tower)
        {
            range = tower.buildRange;
        }

        if (range == 0) return;

        //place tiles
        for (int i=0; i<=range; i++)
        {
            PlaceRing(i, HexUtils.OffsetToCubic(offsetCoord));
        }
    }

    void PlaceRing(int radius, Vector3Int centerCubic)
    {
        if (radius < 0) return;

        //set single tile for radius=0
        if (radius == 0)
        {
            rangeMap.SetTile(HexUtils.CubicToOffset(centerCubic), highlightTile);
            return;
        }

        // Starting position in cubic coordinates (on the positive x-axis of the ring)
        Vector3Int currentCubic = new Vector3Int(radius, -radius, 0);

        // Array representing six directions to traverse the ring for pointed-top hexagons
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(0, 1, -1),   // Top-right
            new Vector3Int(-1, 1, 0),   // Top-left
            new Vector3Int(-1, 0, 1),   // Left
            new Vector3Int(0, -1, 1),   // Bottom-left
            new Vector3Int(1, -1, 0),   // Bottom-right
            new Vector3Int(1, 0, -1)    // Right
        };

        // Place tiles along the hexagonal ring
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                // Convert current cubic position to offset coordinates
                Vector3Int offsetPosition = HexUtils.CubicToOffset(currentCubic + centerCubic);

                // Place the tile at the calculated offset position
                if (bm.IsGroundTile(offsetPosition)) {
                    rangeMap.SetTile(offsetPosition, highlightTile);
                }

                // Move to the next position in the current direction
                currentCubic += directions[i];
            }
        }
    }

    void RemoveRange(Building building, Vector3Int offsetCoord)
    {
        //get building range
        int range = 0;
        if (building is MainTower mainTower)
        {
            range = mainTower.buildRange;
        }
        else if (building is WizardTower tower)
        {
            range = tower.buildRange;
        }

        if (range == 0) return;

        //get coords and radii of other towers
        List<Vector3Int> cubicCoords = new List<Vector3Int>();
        List<int> radii = new List<int>();

        List<MainTower> mainTowers = bm.GetBuildingsOfType(BuildingType.MainTower).Cast<MainTower>().ToList();
        for (int i=0; i<mainTowers.Count; i++)
        {
            cubicCoords.Add(HexUtils.OffsetToCubic(mainTowers[i].offsetCoord));
            radii.Add(mainTowers[i].buildRange);
        }

        List<WizardTower> wizardTowers = bm.GetBuildingsOfType(BuildingType.WizardTower).Cast<WizardTower>().ToList();
        for (int i = 0; i < wizardTowers.Count; i++)
        {
            cubicCoords.Add(HexUtils.OffsetToCubic(wizardTowers[i].offsetCoord));
            radii.Add(wizardTowers[i].buildRange);
        }

        //place tiles
        for (int i = 0; i <= range; i++)
        {
            RemoveRing(i, HexUtils.OffsetToCubic(offsetCoord), cubicCoords, radii);
        }
    }

    void RemoveRing(int radius, Vector3Int centerCubic, List<Vector3Int> cubicCoords, List<int> radii)
    {
        if (radius < 0) return;

        //set single tile for radius=0
        if (radius == 0)
        {
            RemoveTile(centerCubic, cubicCoords, radii);
            return;
        }

        // Starting position in cubic coordinates (on the positive x-axis of the ring)
        Vector3Int currentCubic = new Vector3Int(radius, -radius, 0);

        // Array representing six directions to traverse the ring for pointed-top hexagons
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(0, 1, -1),   // Top-right
            new Vector3Int(-1, 1, 0),   // Top-left
            new Vector3Int(-1, 0, 1),   // Left
            new Vector3Int(0, -1, 1),   // Bottom-left
            new Vector3Int(1, -1, 0),   // Bottom-right
            new Vector3Int(1, 0, -1)    // Right
        };

        // Place tiles along the hexagonal ring
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                // Convert current cubic position to offset coordinates
                Vector3Int cubicPosition = currentCubic + centerCubic;

                // Place the tile at the calculated offset position
                RemoveTile(cubicPosition, cubicCoords, radii);

                // Move to the next position in the current direction
                currentCubic += directions[i];
            }
        }
    }

    void RemoveTile(Vector3Int tileCoord, List<Vector3Int> cubicCoords, List<int> radii)
    {
        for (int i = 0; i<cubicCoords.Count; i++)
        {
            if (HexUtils.CubicDIstance(tileCoord, cubicCoords[i]) <= radii[i]) return;
        }
        
        //remove tile from range
        Vector3Int offsetCoord = HexUtils.CubicToOffset(tileCoord);
        rangeMap.SetTile(offsetCoord, null);

        //remove building at tile
        Building building = bm.GetBuilding(offsetCoord);
        if (building != null)
        {
            bm.DeleteBuilding(offsetCoord, true);
        }
    }


    void EnableRange()
    {
        rangeMap.gameObject.SetActive(true);
    }

    void EnableRange(Structure structure)
    {
        rangeMap.gameObject.SetActive(true);
    }

    void DisableRange()
    {
        rangeMap.gameObject.SetActive(false);
    }
}
