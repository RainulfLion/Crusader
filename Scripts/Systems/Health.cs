using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;



public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    public bool isDead;

    [Header("Animation")]
    public Animator animator;                 // assign or auto-find
    public string hurtTrigger = "Hurt";
    [FormerlySerializedAs("dieTrigger")]
    public string dieParameter = "Die";

    public bool resetDieBoolNextFrame = false;

    public bool disableAnimatorOnDeath = false;
    public float disableAnimatorDelay = 0f;

    [Header("Audio")]
    public AudioSource audioSource;           // optional
    public AudioClip hurtClip;                // optional
    public AudioClip dieClip;                 // optional
    public bool logAudio;

    [Header("Events")]
    public UnityEvent onHurt;
    public UnityEvent onDeath;

    [Header("Weapon Drop")]
    public bool dropWeaponOnDeath;
    public Transform weaponRoot;
    public bool disableWeaponTriggerCollidersOnDrop = true;
    public float weaponDropUpOffset = 0.05f;

    public MonoBehaviour[] disableBehavioursOnDeath;

    public bool IsDead => isDead || currentHealth <= 0;

    private bool _deadLatched;
    private Coroutine _resetDieBoolRoutine;

    private void DropWeaponIfConfigured()
    {
        if (!dropWeaponOnDeath) return;
        if (weaponRoot == null) return;

        weaponRoot.SetParent(null, true);
        if (weaponDropUpOffset != 0f)
            weaponRoot.position += Vector3.up * weaponDropUpOffset;

        Rigidbody rb = weaponRoot.GetComponent<Rigidbody>();
        if (rb == null)
            rb = weaponRoot.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (disableWeaponTriggerCollidersOnDrop)
        {
            Collider[] cols = weaponRoot.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                Collider c = cols[i];
                if (c == null) continue;
                if (c.isTrigger)
                    c.enabled = false;
            }
        }
    }

    private bool AnimatorHasBool(string name)
    {
        if (animator == null) return false;
        if (string.IsNullOrEmpty(name)) return false;

        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == name)
                return ps[i].type == AnimatorControllerParameterType.Bool;
        }

        return false;
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(dieParameter)) return;

        if (AnimatorHasBool(dieParameter))
        {
            animator.SetBool(dieParameter, true);
            if (!resetDieBoolNextFrame)
            {
                if (_resetDieBoolRoutine != null)
                    StopCoroutine(_resetDieBoolRoutine);
                _resetDieBoolRoutine = null;
                return;
            }

            if (_resetDieBoolRoutine != null)
                StopCoroutine(_resetDieBoolRoutine);
            _resetDieBoolRoutine = StartCoroutine(ResetDieBoolNextFrame(dieParameter));
        }
        else
            animator.SetTrigger(dieParameter);
    }

    private IEnumerator ResetDieBoolNextFrame(string boolName)
    {
        yield return null;

        if (animator != null && !string.IsNullOrEmpty(boolName) && AnimatorHasBool(boolName))
            animator.SetBool(boolName, false);

        _resetDieBoolRoutine = null;
    }

    private void DisableBehavioursOnDeath()
    {
        if (disableBehavioursOnDeath == null) return;

        for (int i = 0; i < disableBehavioursOnDeath.Length; i++)
        {
            MonoBehaviour b = disableBehavioursOnDeath[i];
            if (b == null) continue;
            if (b == this) continue;
            b.enabled = false;
        }
    }

    private void DisableAnimatorAfterDelay()
    {
        if (!disableAnimatorOnDeath) return;
        if (animator == null) return;

        if (_resetDieBoolRoutine != null)
        {
            StopCoroutine(_resetDieBoolRoutine);
            _resetDieBoolRoutine = null;
        }

        if (disableAnimatorDelay <= 0f)
        {
            animator.enabled = false;
            return;
        }

        StartCoroutine(DisableAnimatorRoutine());
    }

    private IEnumerator DisableAnimatorRoutine()
    {
        yield return new WaitForSeconds(disableAnimatorDelay);
        if (animator != null)
            animator.enabled = false;
    }

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        _deadLatched = false;
        _resetDieBoolRoutine = null;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (AnimatorHasBool(dieParameter))
            animator.SetBool(dieParameter, false);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDir, GameObject instigator)
    {
        if (isDead) return;
        if (amount <= 0) return;

        int newHealth = currentHealth - amount;
        if (newHealth <= 0)
        {
            currentHealth = 0;
            if (_deadLatched) return;
            _deadLatched = true;

            DisableBehavioursOnDeath();

            PlayDeathAnimation();

            DisableAnimatorAfterDelay();

            isDead = true;

            DropWeaponIfConfigured();

            if (audioSource != null && dieClip != null)
            {
                if (logAudio && (CompareTag("Enemy") || transform.root.CompareTag("Enemy")))
                    Debug.Log($"[Health] AUDIO PlayOneShot DIE clip='{dieClip.name}' time={Time.time:F3} obj='{name}' root='{transform.root.name}' instigator='{(instigator != null ? instigator.name : "null")}'", this);
                audioSource.PlayOneShot(dieClip);
            }

            onDeath?.Invoke();
            return;
        }

        currentHealth = newHealth;

        // hurt feedback
        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (audioSource != null && hurtClip != null)
        {
            if (logAudio && (CompareTag("Enemy") || transform.root.CompareTag("Enemy")))
                Debug.Log($"[Health] AUDIO PlayOneShot HURT clip='{hurtClip.name}' time={Time.time:F3} obj='{name}' root='{transform.root.name}' amount={amount} instigator='{(instigator != null ? instigator.name : "null")}'", this);
            audioSource.PlayOneShot(hurtClip);
        }

        onHurt?.Invoke();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (IsDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
}
