using System;
using Unity.VisualScripting;
using UnityEngine;

public class RequirementEventReceiver : MonoBehaviour
{
    [SerializeField] private DSEventRequirement requirementEvent;

    private void OnEnable()
    {
        requirementEvent.AddListener(CompleteRequirement);
    }

    private void OnDisable()
    {
        requirementEvent.RemoveListener(CompleteRequirement);
    }

    private void CompleteRequirement(RequirementHolder requirement)
    {
        requirement.requirement.CompleteRequirement();
    }
}
