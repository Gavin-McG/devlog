using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance;
    public static UnityEvent<int> end_song = new();

    [Serializable]
    struct RhythmInputs
    {
        [SerializeField] public InputActionReference up;
        [SerializeField] public InputActionReference down;
        [SerializeField] public InputActionReference left;
        [SerializeField] public InputActionReference right;

        public InputActionMap ActionMap =>
            up?.action?.actionMap;
    }

    [Serializable]
    struct Indicators
    {
        [SerializeField] public Indicator up;
        [SerializeField] public Indicator down;
        [SerializeField] public Indicator left;
        [SerializeField] public Indicator right;
    }

    public enum Direction { Left, Down, Up, Right }

    [Header("Main Settings")]
    [SerializeField] private GameObject rhythm_minigame;
    [SerializeField] private RhythmInputs inputs;
    [SerializeField] private Indicators indicators;

    [Header("Notes")]
    [SerializeField] private Transform map_movement;
    [SerializeField] private Note[] note_prefab; // [left, down, up, right]

    [Header("Game Tuning")]
    [SerializeField] private float grace_period = 0.15f;
    [SerializeField] private float early_time = 0.05f;
    [SerializeField] private float bpm = 60f;
    [SerializeField] private float note_speed = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text miss_text;

    private int misses = 0;
    private float game_start_time = 0f;
    public float Game_Time { get; private set; }

    private bool game_active = false;

    private int progression = 0;
    public Note[] Current_Map;

    public SongData current_song_data;

    // left, down, up, right times (or -1)
    private readonly double[] QueuedInputs = { -1, -1, -1, -1 };
    
    private void Awake()
    {
        Instance = this;
        DisableInputMap();
    }

    private void OnEnable()
    {
        inputs.up.action.started += OnUp;
        inputs.down.action.started += OnDown;
        inputs.left.action.started += OnLeft;
        inputs.right.action.started += OnRight;
    }

    private void OnDisable()
    {
        inputs.up.action.started -= OnUp;
        inputs.down.action.started -= OnDown;
        inputs.left.action.started -= OnLeft;
        inputs.right.action.started -= OnRight;
    }

    
    private void EnableInputMap()
    {
        var map = inputs.ActionMap;
        if (map != null) map.Enable();
    }

    private void DisableInputMap()
    {
        var map = inputs.ActionMap;
        if (map != null) map.Disable();
    }

    
    public void Start_Game(SongData data)
    {
        current_song_data = data;

        bpm = data.bpm;
        note_speed = data.note_speed;

        progression = 0;
        misses = 0;
        miss_text.text = "Misses: 0";

        rhythm_minigame.SetActive(true);

        GenerateStartingRandomSong(data.song_length);

        game_start_time = (float)AudioSettings.dspTime;
        game_active = true;

        EnableInputMap();
    }

    private void OnSongEnd()
    {
        game_active = false;

        DisableInputMap();
        rhythm_minigame.SetActive(false);

        end_song.Invoke(misses);
        Debug.Log("Song over");
    }

    
    private void GenerateStartingRandomSong(int n)
    {
        Note[] notes = new Note[n];

        for (int i = 1; i <= n; i++)
        {
            int dir = UnityEngine.Random.Range(0, 4);

            float xPos = (dir * 2) - 3;
            float yPos = -i * note_speed * 60f / bpm;

            Note new_note = Instantiate(note_prefab[dir],
                                        new Vector3(xPos, yPos, 0) + transform.position,
                                        Quaternion.identity,
                                        map_movement);

            new_note.TimeToHit = i * note_speed * 60f / bpm;

            notes[i - 1] = new_note;
        }

        Current_Map = notes;
    }



    private void Update()
    {
        if (!game_active) return;

        Game_Time = ((float)AudioSettings.dspTime - game_start_time) * note_speed;

        if (progression >= Current_Map.Length)
        {
            OnSongEnd();
            return;
        }

        ProcessHits();
        ProcessMisses();

        map_movement.position = new Vector3(map_movement.position.x, Game_Time + transform.position.y, 0);
    }

    
    private void ProcessHits()
    {
        int i = progression;

        while (i < Current_Map.Length && Current_Map[i].TimeToHit < Game_Time + grace_period)
        {
            int matchedDirection = -1;

            // Check for hit
            if (!Current_Map[i].hit)
            {
                int dir = (int)Current_Map[i].direction;
                double queued = QueuedInputs[dir];

                if (queued != -1 && Mathf.Abs((float)(queued - Current_Map[i].TimeToHit)) <= grace_period)
                {
                    matchedDirection = dir;
                    Current_Map[i].Hit();
                    progression = i + 1;

                    Debug.Log("Nice hit");
                }
            }

            // Apply unrelated queued inputs as misses
            for (int j = 0; j < 4; j++)
            {
                if (QueuedInputs[j] != -1 && j != matchedDirection)
                    OnMiss();
            }

            i++;
        }

        ResetQueuedInputs();
    }

    private void ProcessMisses()
    {
        if (progression >= Current_Map.Length) return;

        if (Game_Time - Current_Map[progression].TimeToHit > grace_period * note_speed)
        {
            if (!Current_Map[progression].hit)
                OnMiss();

            progression++;
        }
    }

    private void ResetQueuedInputs()
    {
        for (int i = 0; i < 4; i++)
            QueuedInputs[i] = -1;
    }



    private void OnMiss()
    {
        misses++;
        miss_text.text = "Misses: " + misses;
        Debug.Log("Miss!");
    }



    private void OnLeft(InputAction.CallbackContext ctx)
    {
        indicators.left.Indicate();
        QueuedInputs[0] = Game_Time;
    }

    private void OnDown(InputAction.CallbackContext ctx)
    {
        indicators.down.Indicate();
        QueuedInputs[1] = Game_Time;
    }

    private void OnUp(InputAction.CallbackContext ctx)
    {
        indicators.up.Indicate();
        QueuedInputs[2] = Game_Time;
    }

    private void OnRight(InputAction.CallbackContext ctx)
    {
        indicators.right.Indicate();
        QueuedInputs[3] = Game_Time;
    }
}
