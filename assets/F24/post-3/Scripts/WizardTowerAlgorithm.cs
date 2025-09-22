enum CheckState
{
    NotFound,
    Queued,
    Checked
}

class TowerEntry
{
    public Vector3Int cubicCoord;
    public int range;
    public CheckState state;

    public TowerEntry (Vector3Int cubicCoord, int range, CheckState state)
    {
        this.cubicCoord = cubicCoord;
        this.range = range;
        this.state = state;
    }
}

//check that all ranges can chain to main tower. Remove tower otherwise.
void CleanTowers()
{
    checking = true;

    //init list
    List<TowerEntry> towerData = new List<TowerEntry>();

    //add main tower to list
    List<MainTower> mainTowers = bm.GetBuildingsOfType(BuildingType.MainTower).Cast<MainTower>().ToList();
    towerData.Add(new TowerEntry(HexUtils.OffsetToCubic(mainTowers[0].offsetCoord), mainTowers[0].buildRange, CheckState.Queued));

    //add wizard towers to list
    List<WizardTower> wizardTowers = bm.GetBuildingsOfType(BuildingType.WizardTower).Cast<WizardTower>().ToList();
    foreach (WizardTower tower in wizardTowers)
    {
        towerData.Add(new TowerEntry(HexUtils.OffsetToCubic(tower.offsetCoord), tower.buildRange, CheckState.NotFound));
    }

    //run checking algorithm
    bool finished = false;
    while (!finished)
    {
        finished = true;
        for (int i = 0; i<towerData.Count; ++i)
        {
            if (towerData[i].state == CheckState.Queued)
            {
                finished = false;
                for (int j=1; j<towerData.Count; ++j)
                {
                    int distance = HexUtils.CubicDIstance(towerData[i].cubicCoord, towerData[j].cubicCoord);
                    if (towerData[j].state == CheckState.NotFound && distance <= towerData[i].range)
                    {
                        towerData[j].state = CheckState.Queued;
                    }
                }
                towerData[i].state = CheckState.Checked;
                break;
            }
        }
    }

    //remove all notfound towers
    for (int i = 0; i<towerData.Count; ++i)
    {
        if (towerData[i].state == CheckState.NotFound)
        {
            bm.DeleteBuilding(HexUtils.CubicToOffset(towerData[i].cubicCoord));
        }
    }

    checking = false;
}