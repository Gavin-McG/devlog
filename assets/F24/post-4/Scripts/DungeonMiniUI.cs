using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DungeonMiniUI : MonoBehaviour
{
    [SerializeField] PartyManager pm;

    [Space(10)]

    [SerializeField] GameObject[] adventurerPanelMinis;
    [SerializeField] RectTransform progressBackground;
    [SerializeField] RectTransform progressBar;

    [Space(10)]

    [SerializeField] float updateRate = 0.5f;

    AdventurerPanelMini[] panelInfo;
    float lastUpdate = 0;

    private void OnEnable()
    {
        //check UI sizes
        Debug.Assert(adventurerPanelMinis.Length == pm.adventurers.Length);

        panelInfo = new AdventurerPanelMini[pm.adventurers.Length];
        for (int i=0; i<pm.adventurers.Length; i++)
        {
            //get adventurer panels
            panelInfo[i] = adventurerPanelMinis[i].GetComponent<AdventurerPanelMini>();
        }

        //initial UI state
        UpdateUI();
    }

    private void Update()
    {
        if (Time.time >= lastUpdate + updateRate)
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        //update time
        lastUpdate = Time.time;

        for (int i=0; i<pm.adventurers.Length; ++i)
        {
            Adventurer adventurer = pm.adventurers[i];
            if (adventurer != null && pm.dungeon != null)
            {
                adventurerPanelMinis[i].SetActive(true);

                panelInfo[i].SetHead(adventurer.info.headSprite);
                panelInfo[i].SetHealth(Mathf.Clamp01(adventurer.health/100f));

                //update state image
                switch (adventurer.state)
                {
                    case AdventurerState.Waiting:
                        panelInfo[i].SetState(pm.waitingColor);
                        break;
                    case AdventurerState.Travelling:
                        panelInfo[i].SetState(pm.travellingColor);
                        break;
                    case AdventurerState.Ready:
                        panelInfo[i].SetState(pm.readyColor);
                        break;
                    case AdventurerState.Returning:
                        panelInfo[i].SetState(pm.returningColor);
                        break;
                    case AdventurerState.Fighting:
                        panelInfo[i].SetState(pm.fightingColor);
                        break;
                    case AdventurerState.Dead:
                        panelInfo[i].SetState(pm.deadColor);
                        break;
                }
            }
            else
            {
                adventurerPanelMinis[i].SetActive(false);
            }
        }

        if (pm.dungeon != null)
        {
            progressBackground.gameObject.SetActive(true);
            progressBar.sizeDelta = new Vector2(progressBackground.sizeDelta.x * pm.progress, progressBackground.sizeDelta.y);
        }
        else
        {
            progressBackground.gameObject.SetActive(false);
        }
    }
}
