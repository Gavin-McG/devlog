using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIQueueManager : MonoBehaviour
{
    [SerializeField] GameObject[] CheckedUI;
    [SerializeField] float checkDelay = 0.5f;

    [Space(10)]

    [SerializeField] DungeonFailUI DungeonFailUI;
    [SerializeField] DungeonWinUI DungeonWinUI;

    class QueueEntry { };
    class DungeonFailEntry : QueueEntry { };
    class DungeonWinEntry : QueueEntry { 
        public int gold; 
        public int fossil;
        public DungeonWinEntry (int gold, int fossil)
        {
            this.gold = gold;
            this.fossil = fossil;
        }
    };


    Queue<QueueEntry> queue = new Queue<QueueEntry>();
    float lastUpdate = 0;


    private void OnEnable()
    {
        PartyManager.battleWon.AddListener(DungeonWin);
        PartyManager.battleLost.AddListener(DungeonFail);
    }

    private void OnDisable()
    {
        PartyManager.battleWon.RemoveListener(DungeonWin);
        PartyManager.battleLost.RemoveListener(DungeonFail);
    }

    private void Update()
    {
        if (Time.time - lastUpdate > checkDelay)
        {
            if (!CheckForOpenUI())
            {
                OpenNextEntry();
            }

            lastUpdate = Time.time;
        }
    }


    bool CheckForOpenUI()
    {
        bool UIOpen = false;
        for (int i = 0; i < CheckedUI.Length; i++)
        {
            if (CheckedUI[i].activeSelf == true)
            {
                UIOpen = true;
                break;
            }
        }
        return UIOpen;
    }


    void OpenNextEntry()
    {
        if (queue.Count == 0) return;

        QueueEntry entry = queue.Dequeue();

        if (entry is DungeonFailEntry dungeonFailEntry)
        {
            DungeonFailUI.gameObject.SetActive(true);
        }
        else if (entry is DungeonWinEntry dungeonWinEntry)
        {
            DungeonWinUI.OpenUI(dungeonWinEntry.gold, dungeonWinEntry.fossil);
        }
    }

    void DungeonFail()
    {
        queue.Enqueue(new DungeonFailEntry());
    }

    void DungeonWin(int gold, int fossil)
    {
        queue.Enqueue(new DungeonWinEntry(gold, fossil));
    }
}
