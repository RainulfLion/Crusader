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

    [Header("Events")]
    public UnityEvent onHurt;
    public UnityEvent onDeath;

    public MonoBehaviour[] disableBehavioursOnDeath;

    public bool IsDead => isDead || currentHealth <= 0;

    private bool _deadLatched;
    private Coroutine _resetDieBoolRoutine;

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

            if (audioSource != null && dieClip != null)
                audioSource.PlayOneShot(dieClip);

            onDeath?.Invoke();
            return;
        }

        currentHealth = newHealth;

        // hurt feedback
        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (audioSource != null && hurtClip != null)
            audioSource.PlayOneShot(hurtClip);

        onHurt?.Invoke();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (IsDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
}
