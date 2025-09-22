using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [Space(10)]

    [SerializeField] AudioClip UISound;
    [SerializeField] AudioClip buildSound;
    [SerializeField] AudioClip failedBuildSound;
    [SerializeField] AudioClip breakSound;
    [SerializeField] AudioClip failedBreakSound;
    [SerializeField] AudioClip treeBreakSound;
    [SerializeField] AudioClip interactSound;
    [SerializeField] AudioClip dispatchSound;
    [SerializeField] AudioClip adventurerArrivedSound;
    [SerializeField] AudioClip battleStartSound;
    [SerializeField] AudioClip battleWinSound;
    [SerializeField] AudioClip battleLostSound;


    private void Start()
    {
        UIManager.UIAction.AddListener(() => PlaySound(UISound));

        BuildingManager.BuildingPlaced.AddListener((Building _, Vector3Int _) => PlaySound(buildSound));
        BuildingManager.BuildingDeleted.AddListener((Building _, Vector3Int _) => PlaySound(breakSound));
        BuildingManager.BuildingClicked.AddListener((Building _, Vector3Int _) => PlaySound(interactSound));

        BuildingManager.FailedPlacement.AddListener(() => PlaySound(failedBuildSound));
        BuildingManager.FailedDestroy.AddListener(() => PlaySound(failedBreakSound));

        BuildingManager.EnvironmentClicked.AddListener((EnvironmentTile _, Vector3Int _) => PlaySound(interactSound));
        BuildingManager.EnvironmentDeleted.AddListener((EnvironmentTile _, Vector3Int _) => PlaySound(treeBreakSound));

        PartyManager.partyDispatched.AddListener(() => PlaySound(dispatchSound));
        PartyManager.adventurerArrived.AddListener((Adventurer _) => PlaySound(adventurerArrivedSound));
        PartyManager.adventurerReturned.AddListener((Adventurer _) => PlaySound(adventurerArrivedSound));
        PartyManager.battleBegun.AddListener(() => PlaySound(battleStartSound));
        PartyManager.battleWon.AddListener((int _, int _) => PlaySound(battleWinSound));
        PartyManager.battleLost.AddListener(() => PlaySound(battleLostSound));
    }

    void PlaySound(AudioClip sound)
    {
        audioSource.PlayOneShot(sound);
    }
}
