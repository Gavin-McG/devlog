using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingTooltip : ToolTip
{
    [System.Serializable]
    private struct Tab
    {
        public GameObject topTab;
        public GameObject bottomTab;
        public GameObject window;

        public void SetActive(bool active)
        {
            topTab.SetActive(active);
            bottomTab.SetActive(!active);
            window.SetActive(active);
        }
    }

    [SerializeField] List<Tab> tabs = new List<Tab>();
    [SerializeField] float closeThreshold = 20;
    [SerializeField] UpgradeUI upgradeUI;
    [SerializeField] private DeleteConfirmation deleteConf;
    [SerializeField, Range(0, 1)] float fadeOpacity;

    [Header("Elements")]
    [SerializeField] Image destroyButton;
    [SerializeField] Image upgradeButton;
    [SerializeField] TextMeshProUGUI woodText;
    [SerializeField] TextMeshProUGUI stoneText;
    [SerializeField] TextMeshProUGUI magicText;


    public void SetTab(int tabNum)
    {
        for (int i=0; i<tabs.Count; i++)
        {
            tabs[i].SetActive(tabNum == i);
        }
        Debug.Log("doihfo");
    }

    private void OnEnable()
    {
        EnableUI();

        //Set tooltip positioning
        SetPosition(Input.mousePosition);

        SetTab(0);
    }

    private void OnDisable()
    {
        DisableUI();
    }

    private void Update()
    {
        if (!CheckMousePos(closeThreshold))
        {
            CloseUI();
        }

        Building building = UIManager.Instance.currentBuilding;
        if (building != null)
        {
            destroyButton.color = building.canDestroy ? Color.white : new Color(1, 1, 1, fadeOpacity);

            if (building is IUpgradeable upgradeable)
            {
                Upgrade nextUpgrade = upgradeable.GetNextUpgrade();

                if (nextUpgrade != null)
                {
                    Resources resources = nextUpgrade.upgradeCost;

                    woodText.text = resources.Wood.ToString();
                    stoneText.text = resources.Stone.ToString();
                    magicText.text = resources.Magic.ToString();

                    upgradeButton.color = Color.white;
                }
                else
                {
                    woodText.text = "";
                    stoneText.text = "";
                    magicText.text = "";

                    upgradeButton.color = new Color(1, 1, 1, fadeOpacity);
                }
            }
            else
            {
                woodText.text = "";
                stoneText.text = "";
                magicText.text = "";

                upgradeButton.color = new Color(1, 1, 1, fadeOpacity);
            }
        }
    }


    public void UpgradeBuilding()
    {
        UIManager.Instance.ChangeUI(upgradeUI.gameObject);
        upgradeUI.nearbyBuildingList.Clear();
        upgradeUI.SetUpgradeUI(UIManager.Instance.currentBuilding, UIManager.Instance.currentBuilding);
        BuildingManager.Instance.SetUpgradeMode();

        CloseUI();
    }

    public void DeleteBuilding()
    {
        Building building = UIManager.Instance.currentBuilding;
        if (!building.canDestroy) return;
        Vector3Int offsetCoord = building.offsetCoord;
        deleteConf.SetDeleteUI(building, offsetCoord);

        CloseUI();
    }
}
