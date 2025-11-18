using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text hpText;

    public string currentWaveName = "";
    private void Awake()
    {
        UpdateHP(10, 10);
    }
    public void UpdateHP(float current, float max) // update hp text
    {
        if (hpText == null) return;
        hpText.text = $"HP: {current}/{max}";
    }
    public void UpdateWaveName(string wave) // update wave text
    {
        currentWaveName = wave;
        if (stageText == null) return;
        stageText.text = wave;
    }

    public string GetCurrentWaveName() => currentWaveName;
}