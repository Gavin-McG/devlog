using System;
using UnityEngine;
using WolverineSoft.DialogueSystem.Values;

public class ResetHolder : MonoBehaviour
{
    [SerializeField] DSValueHolder valueHolder;

    private void OnEnable()
    {
        valueHolder.Reset();
    }
}
