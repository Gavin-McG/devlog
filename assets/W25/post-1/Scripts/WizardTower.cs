using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WizardTowerUpgrade : Upgrade
{
    public int magicProduction;
    public int buildRange;
}


public class WizardTower : Building, IUpgradeable<WizardTowerUpgrade>, IProduction, ITownRange
{
    public override BuildingType type => BuildingType.WizardTower;

    /* Upgrade values */
    public int level { get; set; }

    [SerializeField] private WizardTowerUpgrade[] upgrades;
    public WizardTowerUpgrade[] Upgrades { get => upgrades; }
    Upgrade[] IUpgradeable.Upgrades { get => upgrades; }
    void IUpgradeable.UpgradeBuilding() => ((IUpgradeable<WizardTowerUpgrade>)this).UpgradeBuilding();

    public void SetStructure(Structure newStructure) { currentStructure = newStructure; }
    public Vector3Int GetOffsetCoord() { return offsetCoord; }
    public void OnUpgrade(WizardTowerUpgrade upgrade, int newLevel)
    {
        productionAmount = new Resources(upgrade.magicProduction, 0, 0, 0);
        buildRange = upgrade.buildRange;
    }

    /* Production values */
    public Resources productionAmount { get; set; } = new Resources(1, 0, 0, 0);
    public float productionMultiplier { get; set; } = 1;

    /* Town Range */
    public int buildRange { get; set; } = 5;
}