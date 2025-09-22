using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DungeonWinUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI fossilText;


    private void OnEnable()
    {
        UIManager.closeAllUI.AddListener(CloseUI);
    }

    private void OnDisable()
    {
        UIManager.closeAllUI.RemoveListener(CloseUI);
    }

    public void OpenUI(int gold, int fossils)
    {
        gameObject.SetActive(true);

        goldText.text = gold.ToString() + " Gold";
        fossilText.text = fossils.ToString() + " Fossil" + (fossils == 1 ? "" : "s");
    }

    public void CloseUI()
    {
        gameObject.SetActive(false);
    }
}
