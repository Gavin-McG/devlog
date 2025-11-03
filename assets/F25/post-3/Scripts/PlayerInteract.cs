using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] InputActionReference interactAction;
    
    private HashSet<Interactible> nearbyObjects = new HashSet<Interactible>();

    private void OnEnable()
    {
        interactAction.action.performed += TriggerNearbyObjects;
    }

    private void OnDisable()
    {
        interactAction.action.performed -= TriggerNearbyObjects;
    }
    
    public void RegisterObject(Interactible obj)
    {
        nearbyObjects.Add(obj);
    }

    public void UnregisterObject(Interactible obj)
    {
        nearbyObjects.Remove(obj);
    }

    private void TriggerNearbyObjects(InputAction.CallbackContext context)
    {
        foreach (Interactible obj in nearbyObjects)
        {
            obj.TriggerResponses();
        }
    }
}
