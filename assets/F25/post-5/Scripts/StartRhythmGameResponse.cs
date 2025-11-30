using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class StartRhythmGameResponse : InteractionResponse
{
    [SerializeField] SongData songData;
    [SerializeField] InputActionAsset actionAsset;

    public UnityEvent<int> end_song = new();

    private InputActionMap playerActionMap => actionAsset.actionMaps
        .FirstOrDefault(map => map.name == "Player");
    
    protected override void TriggerResponse()
    {
        RhythmGameManager.Instance.Start_Game(songData);
        RhythmGameManager.end_song.AddListener(FinishedSong);
        playerActionMap.Disable();
    }

    private void FinishedSong(int misses)
    {
        RhythmGameManager.end_song.RemoveListener(FinishedSong);
        end_song.Invoke(misses);
        playerActionMap.Enable();
    }
}
