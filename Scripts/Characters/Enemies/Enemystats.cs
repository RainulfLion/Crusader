using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Combat/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";
    
    [Header("Health")]
    public int maxHealth = 100;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 720f;
    public float stoppingDistance = 1.8f;
    
    [Header("Detection")]
    public float acquireRadius = 30f;
    public float guardEnterRange = 10f;
    public float attackRange = 2.2f;
    public float faceTargetRange = 6f;
    
    [Header("Combat Timing")]
    [Tooltip("How long the enemy holds a guard before potentially switching")]
    public float minGuardHoldTime = 0.8f;
    public float maxGuardHoldTime = 1.5f;
    
    [Tooltip("Delay after switching guard before the enemy can attack")]
    public float attackDelayAfterGuardSwitch = 0.3f;
    
    [Tooltip("Time between attacks")]
    public float minAttackCooldown = 1.5f;
    public float maxAttackCooldown = 2.5f;
    
    [Header("Defense Behavior")]
    [Tooltip("If true, enemy will drop guard periodically to create openings")]
    public bool toggleDefending = true;
    
    [Tooltip("How long to hold defend before dropping")]
    public float defendHoldTime = 2f;
    
    [Tooltip("How long the vulnerable window lasts")]
    public float defendDropTime = 0.5f;
    
    [Header("Guard Selection")]
    public bool randomizeGuards = true;
    
    [Tooltip("Weights for each guard stance (Left, High, Right)")]
    public float leftGuardWeight = 1f;
    public float highGuardWeight = 1f;
    public float rightGuardWeight = 1f;
    
    public EnemyAnim.Guard GetWeightedRandomGuard()
    {
        float total = leftGuardWeight + highGuardWeight + rightGuardWeight;
        float roll = Random.Range(0f, total);
        
        if (roll < leftGuardWeight)
            return EnemyAnim.Guard.Left;
        if (roll < leftGuardWeight + highGuardWeight)
            return EnemyAnim.Guard.High;
        return EnemyAnim.Guard.Right;
    }
    
    public EnemyAnim.Guard GetNextGuardInSequence(EnemyAnim.Guard current)
    {
        switch (current)
        {
            case EnemyAnim.Guard.Left:
                return EnemyAnim.Guard.High;
            case EnemyAnim.Guard.High:
                return EnemyAnim.Guard.Right;
            case EnemyAnim.Guard.Right:
            default:
                return EnemyAnim.Guard.Left;
        }
    }
}