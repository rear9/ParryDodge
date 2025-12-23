using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stage", menuName = "Stage Info")]
public class StageInfo : ScriptableObject
{
    public int stageNumber;
    public string stageName;
    public List<Wave> waves = new();
    public float waveStartDelay = 3f;
    public bool isUnlocked = false;
}