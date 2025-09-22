using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonLevelsUI : MonoBehaviour
{
    [SerializeField] PartyManager pm;
    [SerializeField] DungeonUI dungeonUI;

    [Space(10)]

    [SerializeField] GameObject[] levelPanels;

    [Space(10)]

    [SerializeField] float updateRate = 0.5f;

    [HideInInspector] public Dungeon dungeon;
    LevelPanel[] panelInfo;

    private void OnEnable()
    {
        UIManager.closeAllUI.AddListener(CloseUI);
        UIManager.UIOpened.Invoke();

        panelInfo = new LevelPanel[levelPanels.Length];
        for (int i = 0; i < levelPanels.Length; i++)
        {
            //get adventurer panels
            panelInfo[i] = levelPanels[i].GetComponent<LevelPanel>();
        }

        UpdateUI();
    }

    private void OnDisable()
    {
        UIManager.closeAllUI.RemoveListener(CloseUI);
        UIManager.UIClosed.Invoke();
    }

    public void UpdateUI()
    {
        for (int i=0; i<panelInfo.Length; ++i)
        {
            if (i >= dungeon.levels.Length)
            {
                panelInfo[i].gameObject.SetActive(false);
            }
            else
            {
                panelInfo[i].gameObject.SetActive(true);
                panelInfo[i].SetDifficulty(dungeon.levels[i].difficulty);
                panelInfo[i].SetGold(dungeon.levels[i].GoldRange.x, dungeon.levels[i].GoldRange.y);
                panelInfo[i].SetFossil(dungeon.collected[i], dungeon.levels[i].fossilTotal);

                bool enableButton = pm.dungeon==dungeon && (i==0 || dungeon.completed[i-1]);
                panelInfo[i].SetEnterButtonActive(enableButton);
            }
        }
    }

    public void EnterLevel(int level)
    {
        dungeonUI.level = level;
        dungeonUI.dungeon = dungeon;
        OpenDungeonUI();
    }

    public void CloseUI()
    {
        gameObject.SetActive(false);
    }

    void OpenDungeonUI()
    {
        CloseUI();
        dungeonUI.gameObject.SetActive(true);
    }
}
