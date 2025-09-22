using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Random = UnityEngine.Random;

public class PartyManager : MonoBehaviour
{
    [SerializeField] BuildingManager bm;
    [SerializeField] ResourceManager rm;
    [SerializeField] GameObject adventurerPrefab;
    [SerializeField] AdventurerCollection collection;

    [Space(10)]

    //delay between dispatch of each adventurer
    [SerializeField] float dispatchDelay = 0.2f;
    [SerializeField] float statRounding = 0.1f;
    [SerializeField] float fightDelay = 2.0f;

    [Space(10)]

    //adventurer state colors
    [SerializeField] public Color waitingColor = Color.yellow;
    [SerializeField] public Color travellingColor = Color.blue;
    [SerializeField] public Color readyColor = Color.green;
    [SerializeField] public Color returningColor = Color.blue;
    [SerializeField] public Color fightingColor = Color.red;
    [SerializeField] public Color deadColor = Color.black;

    [Space(10)]

    //adventurer class colors
    [SerializeField] public Color warriorColor = Color.red;
    [SerializeField] public Color archerColor = Color.green;
    [SerializeField] public Color mageColor = Color.magenta;

    //party
    [NonSerialized] public Adventurer[] adventurers = {null,null,null,null};
    
    //fighting variables
    [HideInInspector] public Dungeon dungeon = null;
    [HideInInspector] public bool fighting = false;
    [HideInInspector] public float progress = 0;

    //events
    public static UnityEvent adventurerHired = new UnityEvent();
    public static UnityEvent adventurerFired = new UnityEvent();

    public static UnityEvent<Adventurer> adventurerArrived = new UnityEvent<Adventurer>();
    public static UnityEvent<Adventurer> adventurerReturned = new UnityEvent<Adventurer>();

    public static UnityEvent<string> fightEvent = new UnityEvent<string>();
    public static UnityEvent<Adventurer> adventurerKilled = new UnityEvent<Adventurer>();

    public static UnityEvent battleWon = new UnityEvent();
    public static UnityEvent battleLost = new UnityEvent();
    public static UnityEvent battleFinished = new UnityEvent();

    private void OnEnable()
    {
        adventurerArrived.AddListener(FinishedPath);
    }

    private void OnDisable()
    {
        adventurerArrived.RemoveListener(FinishedPath);
    }


    //get the tavern
    private Tavern GetTavern()
    {
        //get tavern(s)
        List<Building> taverns = bm.GetBuildingsOfType(BuildingType.Tavern);

        //check tavern count
        if (taverns.Count == 0)
        {
            //found no taverns
            return null;
        }
        if (taverns.Count > 1)
        {
            Debug.Log("Found multiple taverns");
            return null;
        }

        //check tavern type
        if (taverns[0] is Tavern tavern)
        {
            return tavern;
        }

        //wrong type
        Debug.LogError("Tavern building is not of 'Tavern' class");
        return null;
    }




    //dispatch party towards dungeon
    public void DispatchParty(Dungeon dungeon)
    {
        Tavern tavern = GetTavern();

        //check buildings
        if (tavern == null || dungeon == null)
        {
            Debug.LogError("Could not retrieve valid buildings");
            return;
        };

        //check that adventurers canbe dispatched
        if (!CanDispatch()) return;

        //set all adventurers to travelling
        for (int i = 0; i < adventurers.Length; ++i)
        {
            if (adventurers[i] != null) 
            {
                adventurers[i].state = AdventurerState.Travelling;
            }
        }

        //set dungeonName
        this.dungeon = dungeon;

        //get path
        List<Vector3> path = HexAStar.FindPath(tavern.exit, dungeon.entrance, bm);
        if (path.Count == 0)
        {
            Debug.LogWarning("Could not find valid path between Tavern and selected Dungeon");
            return;
        }

        //start dispatch
        StartCoroutine(DispatchRoutine(path));
    }



    //dispatch adventurers 1 by 1
    IEnumerator DispatchRoutine(List<Vector3> path)
    {
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                //wait for next adventurer
                yield return new WaitForSeconds(dispatchDelay);

                //create walking adventurer character
                GameObject newAdventurer = Instantiate(adventurerPrefab, path[0], Quaternion.identity);
                WalkingAdventurer walker = newAdventurer.GetComponent<WalkingAdventurer>();
                walker.StartPath(adventurers[i], path);
            }
        }
    }



    public Adventurer GenerateAdevnturer()
    {
        Tavern tavern = GetTavern();

        if (tavern==null)
        {
            Debug.LogError("Attempting to generate adventurer without a valid tavern");
            return null;
        }

        return new Adventurer(GetRandomSkills(tavern.averageSkill, 0.1f), GetRandomInfo(), "New Adventurer");
    }

    float NormalDistribution(float mean, float std)
    {
        // Generate a standard normal distribution with mean 0 and standard deviation 1
        float u1 = 1.0f - Random.value; // uniform(0,1] random values
        float u2 = 1.0f - Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        // Scale to our desired spread and center around the target
        return mean + randStdNormal * std;
    }

    float GetRandomValue(float mean, float std)
    {
        float value = -1;
        while (value < 0 || value > 1)
        {
            value = NormalDistribution(mean, std);
        }

        value = Mathf.Ceil(value/statRounding) * statRounding;
        return value;
    }

    Skills GetRandomSkills(float mean, float std)
    {
        return new Skills(
            GetRandomValue(mean, std),
            GetRandomValue(mean, std),
            GetRandomValue(mean, std)
        );
    }

    AdventurerInfo GetRandomInfo()
    {
        int index = Random.Range(0, collection.data.Length);
        return collection.data[index];
    }

    public int AdventurerCount()
    {
        int adventurerCount = 0;
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null && adventurers[i].state != AdventurerState.Dead)
            {
                ++adventurerCount;
            }
        }
        return adventurerCount;
    }

    public bool CanHire()
    {
        bool canHire = dungeon == null;
        for (int i=0; i<adventurers.Length && canHire; ++i)
        {
            if (adventurers[i] != null)
            {
                canHire = canHire && adventurers[i].state == AdventurerState.Waiting;
            }
        }
        return canHire;
    }

    public bool CanDispatch()
    {
        bool canDispatch = true;
        for (int i=0; i<adventurers.Length && canDispatch; ++i)
        {
            if (adventurers[i] != null)
            {
                canDispatch = adventurers[i].state == AdventurerState.Waiting;
            }
        }
        return canDispatch && AdventurerCount() > 0;
    }

    public bool CanFight(Dungeon dungeon)
    {
        if (dungeon != this.dungeon) return false;

        bool canFight = true;
        for (int i = 0; i < adventurers.Length && canFight; ++i)
        {
            if (adventurers[i] != null)
            {
                canFight = adventurers[i].state == AdventurerState.Ready;
            }
        }
        return canFight && AdventurerCount() > 0;
    }

    public bool CanReturn()
    {
        bool canReturn = true;
        for (int i = 0; i < adventurers.Length && canReturn; ++i)
        {
            if (adventurers[i] != null)
            {
                canReturn = adventurers[i].state == AdventurerState.Ready || adventurers[i].state == AdventurerState.Fighting;
            }
        }
        return canReturn && AdventurerCount() > 0;
    }


    void FinishedPath(Adventurer adventurer)
    {
        for (int i=0; i<adventurers.Length; i++)
        {
            if (adventurer == adventurers[i])
            {
                if (adventurers[i].state == AdventurerState.Travelling)
                {
                    adventurers[i].state = AdventurerState.Ready;
                }
                else if (adventurers[i].state == AdventurerState.Returning)
                {
                    adventurers[i].state = AdventurerState.Waiting;
                }
            }
        }
    }



    public bool FireAdventurer(int index)
    {
        if (adventurers[index]==null)
        {
            Debug.LogError("Attenpting to Fire empty adevnturer slot");
            return false;
        }

        adventurers[index] = null;

        adventurerFired.Invoke();
        return true;
    }

    public bool HireAdventurer(int index, Adventurer adventurer)
    {
        if (adventurers[index] != null)
        {
            Debug.LogError("Attenpting to Hire in non-empty adevnturer slot");
            return false;
        }

        adventurers[index] = adventurer;

        adventurerHired.Invoke();
        Debug.Log(GetPartyStrength());
        return true;
    }

    //caluclate numerical strength of party
    public float GetPartyStrength()
    {
        float warriorMultiplier = 1;
        float archerMultiplier = 1;
        float mageMultiplier = 1;

        float skillScore = 1;
        float strengthScore = 1;
        float teamworkScore = 1;

        int adventurerCount = 0;

        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null && adventurers[i].state != AdventurerState.Dead)
            {
                //class add
                switch (adventurers[i].info.classType)
                {
                    case ClassType.Warrior: 
                        warriorMultiplier += 1;
                        break;
                    case ClassType.Archer:
                        archerMultiplier += 1;
                        break;
                    case ClassType.Mage:
                        mageMultiplier += 1;
                        break;
                }

                //stats
                skillScore += adventurers[i].skills.skill;
                strengthScore += adventurers[i].skills.strength;
                teamworkScore += adventurers[i].skills.teamwork;

                //adventurer count
                ++adventurerCount;
            }
        }

        //zero party
        if (adventurerCount == 0)
        {
            return 0;
        }

        //calculate averages
        float divisor = Mathf.Sqrt(adventurerCount);
        skillScore /= divisor;
        strengthScore /= divisor;
        teamworkScore /= divisor;

        return warriorMultiplier * archerMultiplier * mageMultiplier * skillScore * strengthScore * teamworkScore;
    }


    //begin fight
    public void StartFight(float difficulty)
    {
        //set adventurers to fighting
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                adventurers[i].state = AdventurerState.Fighting;
            }
        }

        StartCoroutine(FightRoutine(difficulty));
    }

    //randomly determine whether a given check is succeeded by the party
    bool IsSuccess(float strength, float difficulty, float sensitivity)
    {
        float offset = strength - difficulty;
        float score = offset * sensitivity;
        float requirement = NormalDistribution(0, 1);

        return score > requirement;
    }




    //Run the dungeon fight
    IEnumerator FightRoutine(float difficulty)
    {
        Debug.Log("starting fight");
        fighting = true;

        //count living adventurers
        int living = AdventurerCount();

        //run fight loop
        progress = 0;
        while (progress < 1 && living > 0)
        {
            yield return new WaitForSeconds(fightDelay);

            string eventText = null;
            if (IsSuccess(GetPartyStrength(), difficulty, 0.03f))
            {
                //party succeeded test
                progress += 0.05f;

                //TODO random output text
                eventText = "success " + Mathf.Round(progress*100)/100;
            }
            else
            {
                //choose random adventurer to hurt
                List<int> alive = new List<int>();
                for (int i=0; i<adventurers.Length; ++i)
                {
                    if (adventurers[i] != null && adventurers[i].state == AdventurerState.Fighting)
                    {
                        alive.Add(i);
                    }
                }
                int randomIndex = alive[Random.Range(0, alive.Count)];

                //hurt random adventurer
                float randomDamage = Random.Range(8, 21);
                adventurers[randomIndex].health -= randomDamage;

                //test for killed adventurer
                if (adventurers[randomIndex].health < 0)
                {
                    adventurers[randomIndex].health = 0;
                    adventurers[randomIndex].state = AdventurerState.Dead;
                    living--;
                    adventurerKilled.Invoke(adventurers[randomIndex]);
                }

                //TODO random output text
                eventText = "Adventurer " + randomIndex + " Hurt for " + randomDamage;
            }

            //new fight event
            fightEvent.Invoke(eventText);
        }

        //clear dead adventurers and regen
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                if (adventurers[i].state == AdventurerState.Dead)
                {
                    adventurers[i] = null;
                }
                else
                {
                    adventurers[i].health = 100;
                }
            }
        }

        //sort adventurers
        int count = 0;
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                Adventurer temp = adventurers[i];
                adventurers[i] = null;
                adventurers[count++] = temp;
            }
        }

        //win/loss
        battleFinished.Invoke();
        if (living>0)
        {
            battleWon.Invoke();
            ReturnParty(dungeon);

            //TODO grant custom rewards
            rm.fossilCount++;
            rm.currentResource.Gold += Random.Range(200, 400);
        }
        else
        {
            battleLost.Invoke();
        }

        dungeon = null;
        progress = 0;
        fighting = false;
    }

    //dispatch party towards dungeon
    public void ReturnParty(Dungeon dungeon)
    {
        Tavern tavern = GetTavern();
        
        //check that adventurers can return
        if (!CanReturn()) return;

        //get destination for adventurers
        HexPoint destination = new HexPoint(Vector3Int.zero, false);
        if (tavern != null)
        {
            destination = tavern.exit;
        }
        else if (bm.GetBuildingsOfType(BuildingType.MainTower)[0] is MainTower tower)
        {
            destination = tower.entrance;
        }

        //set all adventurers to returning
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                adventurers[i].state = AdventurerState.Returning;
            }
        }

        //get path
        List<Vector3> path = HexAStar.FindPath(dungeon.entrance, destination, bm);
        if (path.Count == 0)
        {
            Debug.LogWarning("Could not find valid path between Tavern and selected Dungeon");
            return;
        }

        //start dispatch
        StartCoroutine(ReturnRoutine(path));
    }



    //dispatch adventurers 1 by 1
    IEnumerator ReturnRoutine(List<Vector3> path)
    {
        for (int i=0; i<adventurers.Length; ++i)
        {
            if (adventurers[i] != null)
            {
                //wait for next adventurer
                yield return new WaitForSeconds(dispatchDelay);

                //create walking adventurer character
                GameObject newAdventurer = Instantiate(adventurerPrefab, path[0], Quaternion.identity);
                WalkingAdventurer walker = newAdventurer.GetComponent<WalkingAdventurer>();
                walker.StartPath(adventurers[i], path);
            }
        }
    }
}
