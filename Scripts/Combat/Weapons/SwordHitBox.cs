using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public enum GuardedSide
    {
        Right,
        Left,
    }

    [Header("Owner")]
    public GameObject ownerRoot;          // set to player/enemy root; auto if empty
    public Animator ownerAnimator;        // optional; auto if empty

    [Header("Damage")]
    public int damage = 10;
    public float hitCooldown = 0.15f;     // prevents multi-hits per swing on same target

    [Header("Block Feedback (Sword vs Sword)")]
    public AudioSource audioSource;       // optional
    public AudioClip blockClip;           // optional
    public string blockTrigger = "Block"; // trigger on owner animator (optional)

    [Header("Hit Feedback (Sword vs Body)")]
    public AudioClip hitClip;             // optional (slash/impact)

    [Header("Block Conditions")]
    public bool requireDefendingForBlock = true;
    public string defendingBool = "Defending";

    [Header("Block Filtering")]
    public float ignoreBlockAfterBodyHitTime = 0.5f;

    [Header("Directional Guard")]
    public bool useDirectionalGuard = false;
    public Transform guardReference;
    public GuardedSide guardedSide = GuardedSide.Right;
    [Range(0f, 1f)]
    public float sideDotThreshold = 0f;
    public bool requireInFront = true;
    [Range(-1f, 1f)]
    public float frontDotThreshold = 0f;

    [Header("Damage Filtering")]
    public float ignoreDamageAfterSuccessfulBlockTime = 0.35f;

    [Header("Debug")]
    public bool logContacts;

    // Internal
    private float _nextHitTime;
    private float _ignoreBlockUntilTime;
    private float _nextSwordClangTime;
    private float _ignoreDamageUntilTime;
    private Collider _myCollider;

    private static readonly Dictionary<int, float> s_ignoreDamageUntilByOwner = new Dictionary<int, float>();
    private static readonly Dictionary<int, float> s_ignoreBlockUntilByOwner = new Dictionary<int, float>();

    private int OwnerId => ownerRoot != null ? ownerRoot.GetInstanceID() : GetInstanceID();

    private float GetSharedIgnoreDamageUntil()
    {
        float t;
        return s_ignoreDamageUntilByOwner.TryGetValue(OwnerId, out t) ? t : 0f;
    }

    private void SetSharedIgnoreDamageUntil(float untilTime)
    {
        int id = OwnerId;
        float existing;
        if (!s_ignoreDamageUntilByOwner.TryGetValue(id, out existing) || untilTime > existing)
            s_ignoreDamageUntilByOwner[id] = untilTime;
    }

    private float GetSharedIgnoreBlockUntil()
    {
        float t;
        return s_ignoreBlockUntilByOwner.TryGetValue(OwnerId, out t) ? t : 0f;
    }

    private void SetSharedIgnoreBlockUntil(float untilTime)
    {
        int id = OwnerId;
        float existing;
        if (!s_ignoreBlockUntilByOwner.TryGetValue(id, out existing) || untilTime > existing)
            s_ignoreBlockUntilByOwner[id] = untilTime;
    }

    private bool IsIgnoringDamage()
    {
        float until = _ignoreDamageUntilTime;
        float shared = GetSharedIgnoreDamageUntil();
        if (shared > until) until = shared;
        return Time.time < until;
    }

    private bool IsIgnoringBlock()
    {
        float until = _ignoreBlockUntilTime;
        float shared = GetSharedIgnoreBlockUntil();
        if (shared > until) until = shared;
        return Time.time < until;
    }

    private AudioSource EnsureAudioSource()
    {
        if (audioSource != null) return audioSource;

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) return audioSource;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        return audioSource;
    }

    void Awake()
    {
        if (ownerRoot == null) ownerRoot = transform.root.gameObject;
        if (ownerAnimator == null) ownerAnimator = ownerRoot.GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        _myCollider = GetComponent<Collider>();
        if (_myCollider == null)
            UnityEngine.Debug.Log("No collider found on this game object.");
    }

    void OnTriggerEnter(Collider other)
    {
        HandleContact(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleContact(collision.collider);
    }

    private void LogContact(string message)
    {
        if (!logContacts) return;
        UnityEngine.Debug.Log($"[SwordHitbox] {message}", this);
    }

    private bool IsBlockedByTargetGuard(Collider other)
    {
        if (other == null) return false;

        GameObject targetRoot = null;
        Health customHealth = other.GetComponentInParent<Health>();
        if (customHealth != null)
            targetRoot = customHealth.transform.root.gameObject;

        if (targetRoot == null)
            targetRoot = other.transform.root.gameObject;

        if (targetRoot == null || targetRoot == ownerRoot) return false;

        SwordHitbox[] swords = targetRoot.GetComponentsInChildren<SwordHitbox>();
        if (swords == null || swords.Length == 0) return false;

        Vector3 attackerPos = ownerRoot != null ? ownerRoot.transform.position : transform.position;
        for (int i = 0; i < swords.Length; i++)
        {
            SwordHitbox s = swords[i];
            if (s == null) continue;
            if (s == this) continue;
            if (s.ownerRoot == ownerRoot) continue;

            if (s.IsBlockActive() && s.IsGuardingAgainst(attackerPos))
                return true;
        }

        return false;
    }

    private void HandleContact(Collider other)
    {
        if (other == null) return;
        if (Time.time < _nextHitTime) return;

        if (ownerRoot != null)
        {
            Health ownerHealth = ownerRoot.GetComponentInChildren<Health>();
            if (ownerHealth != null && ownerHealth.isDead) return;
        }

        // Don’t hit yourself / your own rig
        if (other.transform.root.gameObject == ownerRoot) return;

        if (logContacts)
        {
            bool isOtherSword = other.GetComponentInParent<SwordHitbox>() != null;
            bool isOtherDamageable = other.GetComponentInParent<IDamageable>() != null;
            LogContact($"CONTACT. Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Collider='{other.name}' Root='{other.transform.root.name}' isSword={isOtherSword} isDamageable={isOtherDamageable}");
        }

        bool isSwordTag = other.CompareTag("Sword") || other.transform.root.CompareTag("Sword");

        // 1) Sword vs Sword (block)
        // If the other object has a SwordHitbox, treat as a blade clash
        SwordHitbox otherSword = other.GetComponentInParent<SwordHitbox>();
        if ((otherSword != null && otherSword.ownerRoot != ownerRoot) || (otherSword == null && isSwordTag))
        {
            GameObject defenderRoot = otherSword != null
                ? otherSword.ownerRoot
                : other.transform.root.gameObject;

            if (defenderRoot == null || defenderRoot == ownerRoot) return;

            Health defenderHealth = defenderRoot.GetComponentInChildren<Health>();
            if (defenderHealth != null && defenderHealth.isDead) return;

            string defenderLabel = defenderRoot != null
                ? defenderRoot.name
                : (otherSword != null ? otherSword.name : other.transform.root.name);

            if (IsIgnoringBlock())
            {
                LogContact($"Sword->Sword ignored (ignore block window). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Defender='{defenderLabel}'");
                return;
            }

            CombatResolver attackerResolver = ownerRoot != null ? ownerRoot.GetComponentInChildren<CombatResolver>() : null;
            CombatResolver defenderResolver = defenderRoot != null ? defenderRoot.GetComponentInChildren<CombatResolver>() : null;
            if (attackerResolver == null || attackerResolver.swingNumber <= 0)
            {
                LogContact($"Sword->Sword ignored (not swinging). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={(attackerResolver != null ? attackerResolver.swingNumber : -1)} Defender='{defenderLabel}'");
                return;
            }

            if (attackerResolver != null && defenderResolver != null)
            {
                if (defenderResolver.ResolveAttackFrom(attackerResolver) == CombatResolver.Outcome.Blocked)
                {
                    LogContact($"Sword->Sword BLOCK. Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={attackerResolver.swingNumber} Defender='{defenderLabel}' defending={defenderResolver.defending} guard={defenderResolver.guardNumber}");
                    float ignoreDamageUntilBlock = Time.time + ignoreDamageAfterSuccessfulBlockTime;
                    _ignoreDamageUntilTime = ignoreDamageUntilBlock;
                    SetSharedIgnoreDamageUntil(ignoreDamageUntilBlock);

                    float ignoreBlockUntilBlock = Time.time + ignoreBlockAfterBodyHitTime;
                    _ignoreBlockUntilTime = ignoreBlockUntilBlock;
                    SetSharedIgnoreBlockUntil(ignoreBlockUntilBlock);

                    _nextHitTime = Time.time + hitCooldown;
                    DoBlockFeedback(false);
                }
                else
                {
                    LogContact($"Sword->Sword contact (no block). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={attackerResolver.swingNumber} Defender='{defenderLabel}' defending={defenderResolver.defending} guard={defenderResolver.guardNumber}");
                }

                return;
            }

            if (otherSword != null)
            {
                CombatResolver fallbackAttackerResolver = ownerRoot != null ? ownerRoot.GetComponentInChildren<CombatResolver>() : null;
                if (fallbackAttackerResolver == null || fallbackAttackerResolver.swingNumber <= 0)
                {
                    LogContact($"Sword->Sword ignored (not swinging, fallback). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={(fallbackAttackerResolver != null ? fallbackAttackerResolver.swingNumber : -1)} Defender='{defenderLabel}'");
                    return;
                }

                bool validBlock = otherSword.IsBlockActive() && otherSword.IsGuardingAgainst(ownerRoot.transform.position);
                if (!validBlock)
                {
                    LogContact($"Sword->Sword contact (no block by directional/defending checks). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Defender='{defenderLabel}'");
                    return;
                }

                LogContact($"Sword->Sword BLOCK (fallback). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Defender='{defenderLabel}'");

                float ignoreDamageUntil = Time.time + ignoreDamageAfterSuccessfulBlockTime;
                _ignoreDamageUntilTime = ignoreDamageUntil;
                SetSharedIgnoreDamageUntil(ignoreDamageUntil);

                float ignoreBlockUntil = Time.time + ignoreBlockAfterBodyHitTime;
                _ignoreBlockUntilTime = ignoreBlockUntil;
                SetSharedIgnoreBlockUntil(ignoreBlockUntil);

                _nextHitTime = Time.time + hitCooldown;
                DoBlockFeedback(false);
                return;
            }

            string safeDefenderLabel = defenderRoot != null ? defenderRoot.name : "null";
            LogContact($"Sword->Sword contact (tag only; no resolver). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' DefenderRoot='{safeDefenderLabel}'");
            return;
        }

        // 2) Sword vs Damageable (hurt)
        IDamageable dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            Health targetHealth = other.GetComponentInParent<Health>();
            if (targetHealth != null && targetHealth.isDead) return;

            if (IsIgnoringDamage())
            {
                LogContact($"Sword->Body ignored (ignore damage window). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Collider='{other.name}'");
                return;
            }

            CombatResolver attackerResolver = ownerRoot != null ? ownerRoot.GetComponentInChildren<CombatResolver>() : null;
            if (attackerResolver == null || attackerResolver.swingNumber <= 0)
            {
                LogContact($"Sword->Body ignored (not swinging). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={(attackerResolver != null ? attackerResolver.swingNumber : -1)} Collider='{other.name}'");
                return;
            }

            GameObject targetRoot = null;
            Health customHealth = other.GetComponentInParent<Health>();
            if (customHealth != null)
                targetRoot = customHealth.transform.root.gameObject;

            if (targetRoot == null)
                targetRoot = other.transform.root.gameObject;

            CombatResolver defenderResolver = targetRoot != null ? targetRoot.GetComponentInChildren<CombatResolver>() : null;

            bool usedResolver = attackerResolver != null && defenderResolver != null;
            if (usedResolver)
            {
                bool resolverBlocked = defenderResolver.ResolveAttackFrom(attackerResolver) == CombatResolver.Outcome.Blocked;
                if (resolverBlocked)
                {
                    LogContact($"Sword->Body BLOCK. Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={attackerResolver.swingNumber} Defender='{(targetRoot != null ? targetRoot.name : other.transform.root.name)}' defending={defenderResolver.defending} guard={defenderResolver.guardNumber} Collider='{other.name}'");
                    float ignoreDamageUntilBlock = Time.time + ignoreDamageAfterSuccessfulBlockTime;
                    _ignoreDamageUntilTime = ignoreDamageUntilBlock;
                    SetSharedIgnoreDamageUntil(ignoreDamageUntilBlock);
                    _nextHitTime = Time.time + hitCooldown;
                    DoBlockFeedback(false);
                    return;
                }

                LogContact($"Sword->Body HIT (resolver). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' swing={attackerResolver.swingNumber} Defender='{(targetRoot != null ? targetRoot.name : other.transform.root.name)}' defending={defenderResolver.defending} guard={defenderResolver.guardNumber} Collider='{other.name}'");
            }

            if (!usedResolver && IsBlockedByTargetGuard(other))
            {
                LogContact($"Sword->Body BLOCK (fallback guard). Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' Collider='{other.name}'");
                float ignoreDamageUntil = Time.time + ignoreDamageAfterSuccessfulBlockTime;
                _ignoreDamageUntilTime = ignoreDamageUntil;
                SetSharedIgnoreDamageUntil(ignoreDamageUntil);
                _nextHitTime = Time.time + hitCooldown;
                DoBlockFeedback(false);
                return;
            }

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 dir = (other.transform.position - ownerRoot.transform.position).normalized;

            LogContact($"Sword->Body DAMAGE applied. Attacker='{(ownerRoot != null ? ownerRoot.name : name)}' damage={damage} Collider='{other.name}'");
            dmg.TakeDamage(damage, hitPoint, dir, ownerRoot);
            DoHitFeedback();

            float ignoreBlockUntil = Time.time + ignoreBlockAfterBodyHitTime;
            _ignoreBlockUntilTime = ignoreBlockUntil;
            SetSharedIgnoreBlockUntil(ignoreBlockUntil);
            _nextHitTime = Time.time + hitCooldown;
            return;
        }
    }

    private bool CanTriggerBlockAnimation()
    {
        if (!requireDefendingForBlock) return true;
        if (ownerAnimator == null) return false;
        if (string.IsNullOrEmpty(defendingBool)) return false;
        return ownerAnimator.GetBool(defendingBool);
    }

    public bool IsBlockActive()
    {
        return CanTriggerBlockAnimation();
    }

    public bool IsGuardingAgainst(Vector3 attackerPosition)
    {
        if (!useDirectionalGuard) return true;

        Transform t = guardReference != null ? guardReference : ownerRoot != null ? ownerRoot.transform : transform;
        Vector3 toAttacker = attackerPosition - t.position;
        toAttacker.y = 0f;
        if (toAttacker.sqrMagnitude < 0.0001f) return true;
        toAttacker.Normalize();

        float sideDot = Vector3.Dot(t.right, toAttacker);
        bool sideOk = guardedSide == GuardedSide.Right
            ? sideDot >= sideDotThreshold
            : sideDot <= -sideDotThreshold;

        if (!sideOk) return false;

        if (requireInFront)
        {
            float frontDot = Vector3.Dot(t.forward, toAttacker);
            if (frontDot < frontDotThreshold) return false;
        }

        return true;
    }

    private void DoBlockFeedback(bool triggerAnimation)
    {
        if (blockClip != null && Time.time >= _nextSwordClangTime)
        {
            AudioSource src = EnsureAudioSource();
            UnityEngine.Debug.Log($"[SwordHitbox] AUDIO PlayOneShot BLOCK clip='{blockClip.name}' time={Time.time:F3} owner='{(ownerRoot != null ? ownerRoot.name : name)}' self='{name}' root='{transform.root.name}'", this);
            src.PlayOneShot(blockClip);
            _nextSwordClangTime = Time.time + hitCooldown;
        }

        if (!triggerAnimation) return;

        if (ownerAnimator != null && !string.IsNullOrEmpty(blockTrigger))
            ownerAnimator.SetTrigger(blockTrigger);

        _nextHitTime = Time.time + hitCooldown;
    }

    private void DoHitFeedback()
    {
        if (hitClip != null)
        {
            AudioSource src = EnsureAudioSource();
            UnityEngine.Debug.Log($"[SwordHitbox] AUDIO PlayOneShot HIT clip='{hitClip.name}' time={Time.time:F3} owner='{(ownerRoot != null ? ownerRoot.name : name)}' self='{name}' root='{transform.root.name}'", this);
            src.PlayOneShot(hitClip);
        }
    }
}
