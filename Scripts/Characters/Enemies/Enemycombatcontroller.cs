using UnityEngine;

public class EnemyCombatController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chasing,
        Guarding,
        Attacking,
        Recovering
    }
    
    [Header("Configuration")]
    public EnemyStats stats;
    
    [Header("References")]
    public Transform target;
    public EnemyMotor motor;
    public EnemyAnim enemyAnim;
    public Health health;
    
    [Header("Debug")]
    public State currentState = State.Idle;
    public float stateTimer;
    public bool showDebugLogs;
    
    // Internal timers
    private float _nextGuardSwitchTime;
    private float _nextAttackTime;
    private float _nextDefendToggleTime;
    private float _attackEnabledTime; // Can't attack until this time (after guard switch)
    private bool _isDefending;
    
    void Awake()
    {
        if (motor == null) motor = GetComponent<EnemyMotor>();
        if (enemyAnim == null) enemyAnim = GetComponentInChildren<EnemyAnim>();
        if (health == null) health = GetComponentInParent<Health>();
        
        ApplyStatsToComponents();
    }
    
    void Start()
    {
        if (stats == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyCombatController has no EnemyStats assigned!", this);
            enabled = false;
            return;
        }
        
        // Initialize with a random guard
        if (enemyAnim != null)
        {
            enemyAnim.SetGuard(stats.GetWeightedRandomGuard());
            enemyAnim.defending = false;
        }
        
        TransitionTo(State.Idle);
    }
    
    void Update()
    {
        if (stats == null) return;
        if (health != null && health.isDead) return;
        if (motor != null && !motor.CanMove()) return;
        
        EnsureTarget();
        
        stateTimer -= Time.deltaTime;
        
        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Chasing:
                UpdateChasing();
                break;
            case State.Guarding:
                UpdateGuarding();
                break;
            case State.Attacking:
                UpdateAttacking();
                break;
            case State.Recovering:
                UpdateRecovering();
                break;
        }
    }
    
    private void ApplyStatsToComponents()
    {
        if (stats == null) return;
        
        if (motor != null)
        {
            motor.moveSpeed = stats.moveSpeed;
            motor.rotationSpeed = stats.rotationSpeed;
            motor.stoppingDistance = stats.stoppingDistance;
        }
        
        if (health != null)
        {
            health.maxHealth = stats.maxHealth;
            health.currentHealth = stats.maxHealth;
        }
    }
    
    private void TransitionTo(State newState)
    {
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] {currentState} -> {newState}", this);
        
        // Exit current state
        switch (currentState)
        {
            case State.Attacking:
                // Nothing special on exit
                break;
        }
        
        currentState = newState;
        
        // Enter new state
        switch (newState)
        {
            case State.Idle:
                stateTimer = 0.5f;
                SetDefending(false);
                if (motor != null) motor.Stop();
                break;
                
            case State.Chasing:
                stateTimer = 0f;
                SetDefending(false);
                break;
                
            case State.Guarding:
                stateTimer = 0f;
                SetDefending(true);
                ScheduleNextGuardSwitch();
                ScheduleNextDefendToggle();
                break;
                
            case State.Attacking:
                stateTimer = 0.5f; // Animation will control actual duration
                TriggerAttack();
                break;
                
            case State.Recovering:
                stateTimer = Random.Range(stats.minAttackCooldown, stats.maxAttackCooldown);
                _nextAttackTime = Time.time + stateTimer;
                break;
        }
    }
    
    private void UpdateIdle()
    {
        if (target == null) return;
        
        float dist = GetDistanceToTarget();
        
        if (dist <= stats.guardEnterRange)
            TransitionTo(State.Guarding);
        else if (dist <= stats.acquireRadius)
            TransitionTo(State.Chasing);
    }
    
    private void UpdateChasing()
    {
        if (target == null)
        {
            TransitionTo(State.Idle);
            return;
        }
        
        float dist = GetDistanceToTarget();
        
        if (dist <= stats.guardEnterRange)
        {
            TransitionTo(State.Guarding);
            return;
        }
        
        if (dist > stats.acquireRadius)
        {
            TransitionTo(State.Idle);
            return;
        }
        
        if (motor != null)
            motor.MoveTowards(target.position);
    }
    
    private void UpdateGuarding()
    {
        if (target == null)
        {
            TransitionTo(State.Idle);
            return;
        }
        
        float dist = GetDistanceToTarget();
        
        // Lost target, go back to chasing
        if (dist > stats.guardEnterRange * 1.2f)
        {
            TransitionTo(State.Chasing);
            return;
        }
        
        // Face and approach target
        if (motor != null)
        {
            if (dist > stats.faceTargetRange)
                motor.MoveTowards(target.position);
            else if (dist > stats.attackRange)
            {
                motor.FaceTowards(target.position);
                motor.MoveTowards(target.position);
            }
            else
            {
                motor.FaceTowards(target.position);
                motor.Stop();
            }
        }
        
        // Handle defend toggling (creates openings for player)
        if (stats.toggleDefending && Time.time >= _nextDefendToggleTime)
        {
            _isDefending = !_isDefending;
            SetDefending(_isDefending);
            
            if (_isDefending)
                _nextDefendToggleTime = Time.time + stats.defendHoldTime;
            else
                _nextDefendToggleTime = Time.time + stats.defendDropTime;
        }
        
        // Handle guard switching
        if (Time.time >= _nextGuardSwitchTime)
        {
            SwitchGuard();
            ScheduleNextGuardSwitch();
        }
        
        // Try to attack if in range and allowed
        if (dist <= stats.attackRange && CanAttack())
        {
            TransitionTo(State.Attacking);
        }
    }
    
    private void UpdateAttacking()
    {
        // Wait for animation to complete (stateTimer counts down)
        // The actual attack trigger happens on state entry
        
        if (stateTimer <= 0f)
            TransitionTo(State.Recovering);
    }
    
    private void UpdateRecovering()
    {
        if (target == null)
        {
            TransitionTo(State.Idle);
            return;
        }
        
        float dist = GetDistanceToTarget();
        
        // Keep facing target and stay in guard
        if (motor != null)
        {
            motor.FaceTowards(target.position);
            motor.Stop();
        }
        
        SetDefending(true);
        
        // Handle guard switching during recovery too
        if (Time.time >= _nextGuardSwitchTime)
        {
            SwitchGuard();
            ScheduleNextGuardSwitch();
        }
        
        if (stateTimer <= 0f)
            TransitionTo(State.Guarding);
    }
    
    private bool CanAttack()
    {
        if (Time.time < _nextAttackTime) return false;
        if (Time.time < _attackEnabledTime) return false;
        return true;
    }
    
    private void TriggerAttack()
    {
        if (enemyAnim == null) return;
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Attacking with guard: {enemyAnim.currentGuard}", this);
        
        enemyAnim.TriggerPrimaryAttack();
    }
    
    private void SwitchGuard()
    {
        if (enemyAnim == null) return;
        if (stats == null) return;
        
        EnemyAnim.Guard newGuard;
        
        if (stats.randomizeGuards)
        {
            // Get a different guard than current
            EnemyAnim.Guard current = enemyAnim.currentGuard;
            int attempts = 0;
            do
            {
                newGuard = stats.GetWeightedRandomGuard();
                attempts++;
            }
            while (newGuard == current && attempts < 10);
        }
        else
        {
            newGuard = stats.GetNextGuardInSequence(enemyAnim.currentGuard);
        }
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Guard: {enemyAnim.currentGuard} -> {newGuard}", this);
        
        enemyAnim.SetGuard(newGuard);
        
        // Delay attacks after switching guard so the stance is visible
        _attackEnabledTime = Time.time + stats.attackDelayAfterGuardSwitch;
    }
    
    private void ScheduleNextGuardSwitch()
    {
        float holdTime = Random.Range(stats.minGuardHoldTime, stats.maxGuardHoldTime);
        _nextGuardSwitchTime = Time.time + holdTime;
    }
    
    private void ScheduleNextDefendToggle()
    {
        _isDefending = true;
        _nextDefendToggleTime = Time.time + stats.defendHoldTime;
    }
    
    private void SetDefending(bool defending)
    {
        _isDefending = defending;
        if (enemyAnim != null)
            enemyAnim.defending = defending;
    }
    
    private float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        if (motor != null) return motor.DistanceTo(target.position);
        
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        return to.magnitude;
    }
    
    private void EnsureTarget()
    {
        if (target != null) return;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        float d = Vector3.Distance(transform.position, player.transform.position);
        if (stats != null && stats.acquireRadius > 0f && d > stats.acquireRadius) return;
        
        target = player.transform;
    }
    
    // Public methods for external control
    
    public void ForceGuard(EnemyAnim.Guard guard)
    {
        if (enemyAnim != null)
            enemyAnim.SetGuard(guard);
        ScheduleNextGuardSwitch();
    }
    
    public void ForceAttack()
    {
        if (currentState == State.Guarding || currentState == State.Recovering)
            TransitionTo(State.Attacking);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}