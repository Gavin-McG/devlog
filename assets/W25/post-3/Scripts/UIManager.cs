using System;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    //hover variables
    [Serializable]enum ToolTipMode { Hover, RightClick };
    [SerializeField] ToolTipMode toolTipMode = ToolTipMode.Hover;
    [SerializeField] float hoverTime = 1f;

    PartyManager pm;

    //ui object variables
    private Dictionary<string, GameObject> uiDictionary;
    private Dictionary<string, GameObject> tooltipDictionary;
    private GameObject openedUI = null;
    [HideInInspector] public Building currentBuilding = null;

    //hover tooltip variables
    private Vector3 lastMousePosition;
    private float idleTimer;

    //tell all UI to close
    public static UnityEvent closeAllUI = new UnityEvent();

    //events
    public static UnityEvent UIOpened = new UnityEvent();
    public static UnityEvent UIClosed = new UnityEvent();
    public static UnityEvent UIAction = new UnityEvent();

    private InputAction closeAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        //closeAllUI.AddListener(OnCloseAllUI);
        
        closeAction = GetComponent<PlayerInput>().actions["CloseMenus"];
        closeAction.performed += _ => closeAllUI.Invoke();

        uiDictionary = UIDictionaryManager.uiDictionary;
        tooltipDictionary = UIDictionaryManager.tooltipDictionary;

        lastMousePosition = Input.mousePosition;

        // Subscribe to log window being shown and freeze the game
        // IMPORTANT: In the future, if we have anything else that changes the time scale we should extract these out into whatever manages the time scale.
        //            If we aren't careful later we could end up freezed.
        DebugLogManager.Instance.OnLogWindowShown += OnLogWindowShown;
        DebugLogManager.Instance.OnLogWindowHidden += OnLogWindowHidden;
    }

    void OnLogWindowShown()
    {
        Time.timeScale = 0f;
    }

    void OnLogWindowHidden()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        pm = PartyManager.Instance;
    }

    private void OnEnable()
    {
        BuildingManager.BuildingClicked.AddListener(ClickBuilding);
    }

    private void OnDisable()
    {
        BuildingManager.BuildingClicked.RemoveListener(ClickBuilding);
    }

    private void Update()
    {
        Debug.Log(openedUI?.name);
        CheckTooltips();
    }



    void ClickBuilding(Building building, Vector3Int offsetCoords)
    {
        //event calls
        closeAllUI.Invoke();
        currentBuilding = building;

        //open correct UI
        switch (building.type) 
        {
            case BuildingType.Tavern:
                TryOpenUI("Tavern", uiDictionary);
                break;
            case BuildingType.Dungeon:
                TryOpenDungeonUI(building);
                break;
            case BuildingType.Contractor:
                TryOpenUI("Contractor", uiDictionary);
                break;
            case BuildingType.Smithy:
                TryOpenUI("Smithy", uiDictionary);
                break;
            case BuildingType.MainTower:
                TryOpenUI("MainTower", uiDictionary);
                break;
            default:
                break;
        }
    }

    void OpenTooltip(Building building)
    {
        currentBuilding = building;

        //open correct UI
        switch (building.type)
        {
            case BuildingType.Contractor:
                TryOpenUI("Contractor", tooltipDictionary);
                break;
        }
    }

    void TryOpenUI(string UIName, Dictionary<string, GameObject> uiDictionary)
    {
        GameObject foundUI;
        if (uiDictionary.TryGetValue(UIName, out foundUI))
        {
            foundUI.SetActive(true);
            openedUI = foundUI;
            UIOpened.Invoke();
        }
        else
        {
            Debug.LogError(UIName + " UI could not be found!");
        }
    }

    void TryOpenDungeonUI(Building building)
    {
        if (building is Dungeon dungeon)
        {
            if (pm.fighting && dungeon == pm.dungeon)
            {
                //open dungeon UI directly
                GameObject dungeonUI;
                if (uiDictionary.TryGetValue("Dungeon", out dungeonUI))
                {
                    dungeonUI.GetComponent<DungeonUI>().dungeon = dungeon;
                    dungeonUI.SetActive(true);
                    openedUI = dungeonUI;
                }
                else
                {
                    Debug.LogError("Dungeon UI could not be found!");
                }
            }
            else
            {
                //open levels UI
                GameObject dungeonLevelsUI;
                if (uiDictionary.TryGetValue("DungeonLevels", out dungeonLevelsUI))
                {
                    dungeonLevelsUI.GetComponent<DungeonLevelsUI>().dungeon = dungeon;
                    dungeonLevelsUI.SetActive(true);
                    openedUI = dungeonLevelsUI;
                }
                else
                {
                    Debug.LogError("Dungeon Levels UI could not be found!");
                }
            }
        }
        else
        {
            Debug.LogError("Dungeon '" + building.buildingName + "' BuildingType does not derive from Dungeon Script");
        }
    }



    void CheckTooltips()
    {
        //don't open tooltip if ui is already open
        if (openedUI != null) return;

        Building openedTooltip = toolTipMode switch
        {
            ToolTipMode.Hover => CheckHoverToolTip(),
            ToolTipMode.RightClick => CheckRightClickToolTip(),
            _ => throw new Exception("Invalid Tooltip mode selected")
        };

        if (openedTooltip != null)
        {
            Debug.Log("Trying to open tooltip");
            OpenTooltip(openedTooltip);
        } 
    }

    Building CheckHoverToolTip()
    {
        bool hoverValid = false;

        //increment hover logic and set hoverValid
        if (Input.mousePosition != lastMousePosition)
        {
            lastMousePosition = Input.mousePosition;
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= hoverTime)
            {
                hoverValid = true;
            }
        }

        //check for building to open tooltip for
        if (hoverValid)
        {
            //get hovered building
            var selectedOffset = BuildingManager.Instance.GetSelectedOffset();
            var building = BuildingManager.Instance.GetBuilding(selectedOffset);

            //return building (null if no building)
            return building;
        }
        return null;
    }

    //check if the mouse button is pressed whilst hovering over a building
    Building CheckRightClickToolTip()
    {
        if (Input.GetMouseButtonUp(1))
        {
            //get hovered building
            var selectedOffset = BuildingManager.Instance.GetSelectedOffset();
            var building = BuildingManager.Instance.GetBuilding(selectedOffset);

            //return building (null if no building)
            return building;
        }
        return null;
    }

    public void ChangeUI(GameObject newUI)
    {
        if (openedUI != null) openedUI.SetActive(false);
        openedUI = newUI;
        openedUI.SetActive(true);
    }

    public void CloseUI()
    {
        if (openedUI == null) return;

        openedUI.SetActive(false);
        openedUI = null;
        currentBuilding = null;
        UIClosed.Invoke();
    }

    public void CloseUI(GameObject element)
    {
        if (openedUI == element)
        {
            CloseUI();
        }
        else if (element.activeSelf)
        {
            element.SetActive(false);
            UIClosed.Invoke();
        }
    }
}
