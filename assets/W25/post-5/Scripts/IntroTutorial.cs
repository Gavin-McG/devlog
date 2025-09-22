using DS;
using DS.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class IntroTutorial : MonoBehaviour, ISaveData
{
    [System.Serializable]
    public enum TutorialStage
    {
        MainTower,
        DeleteButton,
        CutWood,
        BuildButton,
        ContractorButton,
        ConfirmButton,
        PlaceContractor,
        Contractor,
        Finished
    }

    [System.Serializable]
    public struct EnableOnStage
    {
        public TutorialStage stage;
        public List<GameObject> objects;
        public DSDialogue dialogue;
        public DSDialogueSO callbackNode;
        public UnityEvent stageEvent;
    }

    [Header("Object references")]
    [SerializeField] public TutorialOverlay tutorialOverlay;
    [Header("Tutorial Stage Info")]
    [SerializeField] int treeCount = 5;
    [SerializeField] public TutorialStage currentStage;
    [SerializeField] public List<EnableOnStage> stageEffects;

    private int destroyedTrees = 0;

    private int stageCount { get => System.Enum.GetNames(typeof(TutorialStage)).Length; }

    private bool hasSetupTutorial = false;

    private void Update()
    {
        if (!hasSetupTutorial)
        {
            SetupTutorial();
            hasSetupTutorial = true;
        }
    }

    private void SetupTutorial()
    {
        SetObjects();
        ProgressStage(TutorialStage.MainTower);

        //set tree goal
        BuildingManager.EnvironmentDeleted.AddListener((tile, vec) => {
            destroyedTrees++;
            if (destroyedTrees >= treeCount && currentStage == TutorialStage.CutWood)
            {
                ProgressStage(TutorialStage.CutWood + 1);
            }
        });

        //set building goal
        BuildingManager.BuildingPlaced.AddListener((building, vec) => {
            if (building.type == BuildingType.Contractor && currentStage==TutorialStage.PlaceContractor)
            {
                ProgressStage(TutorialStage.PlaceContractor + 1);
            }
        });
    }


    void SetObjects()
    {
        foreach (EnableOnStage stageInfo in stageEffects)
        {
            foreach(GameObject obj in stageInfo.objects)
            {
                if (obj != null)
                {
                    obj.SetActive(currentStage >= stageInfo.stage);
                }
            }
        }
    }

    public void ProgressByOneStage()
    {
        ProgressStage(currentStage + 1);
    }

    public void ProgressStage(TutorialStage stage)
    {
        if (stage < currentStage) return;
        currentStage = stage;

        foreach (EnableOnStage stageInfo in stageEffects)
        {
            if (stageInfo.stage != currentStage) continue;

            foreach (GameObject obj in stageInfo.objects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }

            //begin dialogue for this stage
            if (stageInfo.dialogue != null)
            {
                StartCoroutine(StartDialogue(stageInfo.dialogue, 0.5f));
            }

            //set dialogue callback event
            if (stageInfo.callbackNode != null)
            {
                //stage event if node callback is set
                DialogueManager.DialogueOptionEvent.AddListener((DSDialogueSO) =>
                {
                    Debug.Log("Recieved Callback");
                    if (DSDialogueSO == stageInfo.callbackNode)
                    {
                        stageInfo.stageEvent.Invoke();
                    }
                });
            }
            else
            {
                //directly display button if no callback is set
                stageInfo.stageEvent.Invoke();
            }

            break;
        }
    }

    IEnumerator StartDialogue(DSDialogue dialogue, float time)
    {
        yield return new WaitForSeconds(time);

        DialogueManager.DialogueTriggered.Invoke(dialogue);
    }


    void ISaveData.LoadData(GameData data)
    {
        currentStage = data.tutorialData.introStage;
    }

    void ISaveData.SaveData(ref GameData data)
    {
        data.tutorialData.introStage = currentStage;
    }

    void ISaveData.SetDefaultData(ref GameData data)
    {
        data.tutorialData.introStage = TutorialStage.MainTower;
    }
}
