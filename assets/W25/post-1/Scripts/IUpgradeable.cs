using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Upgrade
{
    public Structure newStructure;
    public Resources upgradeCost;
}

public interface IUpgradeable
{
    int level { get; set; }
    Upgrade[] Upgrades { get; }

    void SetStructure(Structure newStructure);
    Vector3Int GetOffsetCoord();
    void UpgradeBuilding();

    Upgrade GetNextUpgrade()
    {
        //if upgrade unavailable
        if (level >= Upgrades.Length)
        {
            return null;
        }

        return Upgrades[level];
    }

    //check for a contractor which could upgrade the building
    //returns (canUpgrade, WizardTower/MainTower, Contractor)
    //if canUpgrade is false, the other two fields are null
    (bool canUpgrade, ITownRange tower, Contractor contractor) IsUpgradeable()
    {
        //if upgrade unaffordable
        Upgrade upgrade = GetNextUpgrade();
        if (upgrade==null || ResourceManager.Instance.CanAfford(upgrade.upgradeCost))
        {
            return (false, null, null);
        }

        //get all towers within range
        List<ITownRange> towers = BuildingManager.Instance.GetBuildings<ITownRange>();

        //get list of contractors
        List<Contractor> contractors = BuildingManager.Instance.GetBuildings<Contractor>();

        //filter towers by range
        Vector3Int cubicCoord = HexUtils.OffsetToCubic(GetOffsetCoord());

        foreach (ITownRange tower in towers)
        {
            //get distance of tower from building
            Vector3Int towerCoord = HexUtils.OffsetToCubic(tower.GetOffsetCoord());
            int towerDistance = HexUtils.CubicDIstance(towerCoord, cubicCoord);

            //skip if not close enough
            if (towerDistance > tower.buildRange)
            {
                continue;
            }

            //check for nearby contractors
            foreach (Contractor contractor in contractors)
            {
                //get distance of tower from building
                Vector3Int contractorCoord = HexUtils.OffsetToCubic(contractor.offsetCoord);
                int contractorDistance = HexUtils.CubicDIstance(towerCoord, contractorCoord);

                //return is contractor is adequite and close enought
                if (contractorDistance <= tower.buildRange && contractor.CanUpgrade(this))
                {
                    return (true, tower, contractor);
                }
            }
        }

        return (false, null, null);
    }
}

public interface IUpgradeable<T> : IUpgradeable where T : Upgrade
{
    new T[] Upgrades { get; }

    //run the building upgrade
    new void UpgradeBuilding()
    {
        //get the upgrade
        Upgrade upgrade = GetNextUpgrade();
        
        ResourceManager.Instance.Charge(upgrade.upgradeCost);
        level++;

        //upgrade structure
        if (upgrade.newStructure)
        {
            SetStructure(upgrade.newStructure);
        }

        if (upgrade is T buildingUpgrade) {
            //building-specific changes
            OnUpgrade(buildingUpgrade, level);
        }
    }

    void OnUpgrade(T upgrade, int newLevel);
}
