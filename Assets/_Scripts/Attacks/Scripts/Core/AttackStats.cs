using UnityEngine;

[CreateAssetMenu(menuName = "Attack Stats", fileName = "NewAttackStats")]
public class AttackStats : ScriptableObject
{
    [Header("General")] 
    public string attackName = "Unknown Attack";
    
    [Tooltip("Damage dealt to player on hit")]
    public float damage = 1f;
    
    [Tooltip("How long the attack exists before despawning")]
    public float lifetime = 5f;

    [Tooltip("Whether this attack can be parried")]
    public bool parryable = true;

    [Tooltip("Speed/Movement multiplier for moving attacks")]
    public float attackSpeed = 2f;

    [Tooltip("Visual for debugging")]
    public Color attackColor = Color.orangeRed;
}
