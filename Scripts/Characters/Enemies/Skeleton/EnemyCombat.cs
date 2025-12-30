using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public EnemyMotor motor;
    public EnemyAnim enemyAnim;

    [Header("Chase")]
    public float acquireRadius = 30f;
    public float guardEnterRange = 10f;
    public float attackRange = 2.2f;
    public float faceTargetRange = 6f;

    [Header("Defense")]
    public bool toggleDefending = true;
    public float defendHoldSeconds = 2f;
    public float minDefendToggleInterval = 0.7f;
    public float maxDefendToggleInterval = 1.4f;

    [Header("Attacks")]
    public float minAttackCooldown = 1.1f;
    public float maxAttackCooldown = 1.8f;
    public float guardChangeInterval = 1.0f;
    public bool randomizeGuards = true;

    private float _nextAttackTime;
    private float _nextGuardChangeTime;
    private float _nextDefendToggleTime;

    void Awake()
    {
        if (motor == null) motor = GetComponent<EnemyMotor>();
        if (enemyAnim == null) enemyAnim = GetComponentInChildren<EnemyAnim>();
    }

    void Update()
    {
        if (motor == null || !motor.CanMove()) return;

        EnsureTarget();
        if (target == null)
        {
            motor.Stop();
            return;
        }

        float dist = motor.DistanceTo(target.position);

        bool inGuardRange = guardEnterRange > 0f && dist <= guardEnterRange;
        if (enemyAnim != null)
        {
            if (!inGuardRange)
            {
                enemyAnim.defending = false;
                _nextDefendToggleTime = 0f;
            }
            else if (!toggleDefending)
            {
                enemyAnim.defending = true;
            }
            else
            {
                if (_nextDefendToggleTime <= 0f)
                {
                    enemyAnim.defending = true;
                    _nextDefendToggleTime = Time.time + Mathf.Max(0.05f, defendHoldSeconds);
                }
                else if (Time.time >= _nextDefendToggleTime)
                {
                    if (enemyAnim.defending)
                    {
                        enemyAnim.defending = false;
                        _nextDefendToggleTime = Time.time + Mathf.Max(0.05f, Random.Range(minDefendToggleInterval, maxDefendToggleInterval));
                    }
                    else
                    {
                        enemyAnim.defending = true;
                        _nextDefendToggleTime = Time.time + Mathf.Max(0.05f, defendHoldSeconds);
                    }
                }
            }
        }

        if (dist <= faceTargetRange)
            motor.FaceTowards(target.position);

        if (dist > attackRange)
        {
            motor.MoveTowards(target.position);
            if (enemyAnim != null && enemyAnim.defending)
                TryChangeGuard();
            return;
        }

        motor.Stop();
        if (enemyAnim != null && enemyAnim.defending)
            TryChangeGuard();
        TryAttack();
    }

    private void EnsureTarget()
    {
        if (target != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float d = Vector3.Distance(transform.position, player.transform.position);
        if (acquireRadius > 0f && d > acquireRadius) return;
        target = player.transform;
    }

    private void TryChangeGuard()
    {
        if (enemyAnim == null) return;
        if (Time.time < _nextGuardChangeTime) return;

        _nextGuardChangeTime = Time.time + Mathf.Max(0.05f, guardChangeInterval);

        if (randomizeGuards)
        {
            int i = Random.Range(0, 3);
            enemyAnim.SetGuard((EnemyAnim.Guard)i);
        }
        else
        {
            EnemyAnim.Guard next = enemyAnim.currentGuard;
            switch (next)
            {
                case EnemyAnim.Guard.Left:
                    next = EnemyAnim.Guard.High;
                    break;
                case EnemyAnim.Guard.High:
                    next = EnemyAnim.Guard.Right;
                    break;
                case EnemyAnim.Guard.Right:
                default:
                    next = EnemyAnim.Guard.Left;
                    break;
            }

            enemyAnim.SetGuard(next);
        }
    }

    private void TryAttack()
    {
        if (enemyAnim == null) return;
        if (Time.time < _nextAttackTime) return;

        enemyAnim.TriggerPrimaryAttack();
        float cd = Random.Range(minAttackCooldown, maxAttackCooldown);
        _nextAttackTime = Time.time + Mathf.Max(0.05f, cd);
    }
}
