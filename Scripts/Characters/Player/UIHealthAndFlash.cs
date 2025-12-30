using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthAndFlash : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;
    public Image damageFlashImage;
    public TMP_Text healthText;
    public TMP_InputField healthInputField;

    [Header("Flash")]
    public float flashAlpha = 0.45f;
    public float fadeSpeed = 6f;

    [Header("Display")]
    public bool showMaxHealth;

    [Header("Debug")]
    public bool logDebug;

    float currentAlpha;
    int _lastHealth = int.MinValue;

    void Awake()
    {
        EnsurePlayerHealth();
    }

    void OnEnable()
    {
        EnsurePlayerHealth();
    }

    void OnDisable()
    {
        SetPlayerHealth(null);
    }

    void Start()
    {
        EnsurePlayerHealth();
        if (playerHealth != null)
            _lastHealth = playerHealth.currentHealth;
        UpdateHealthText();
        SetFlashAlpha(0f);

        if (logDebug)
            Debug.Log($"[UIHealthAndFlash] Started. Health='{(playerHealth != null ? playerHealth.name : "null")}' Text='{(healthText != null ? healthText.name : "null")}'", this);
    }

    void Update()
    {
        EnsurePlayerHealth();

        if (playerHealth != null)
        {
            int h = playerHealth.currentHealth;
            if (_lastHealth == int.MinValue)
                _lastHealth = h;
            else if (h < _lastHealth)
                OnPlayerHurt();

            if (logDebug && h != _lastHealth)
                Debug.Log($"[UIHealthAndFlash] Health changed: {_lastHealth} -> {h}", this);

            if (h != _lastHealth)
            {
                _lastHealth = h;
                UpdateHealthText();
            }
        }

        // fade back to transparent
        if (currentAlpha > 0f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * fadeSpeed);
            SetFlashAlpha(currentAlpha);
        }
    }

    void LateUpdate()
    {
        UpdateHealthText();
    }

    public void OnPlayerHurt()
    {
        // call this when player takes damage
        currentAlpha = flashAlpha;
        SetFlashAlpha(currentAlpha);
        UpdateHealthText();
    }

    void EnsurePlayerHealth()
    {
        Health found = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            found = player.GetComponentInChildren<Health>();

        if (found == null)
            found = FindObjectOfType<Health>();

        if (found != null && found != playerHealth)
            SetPlayerHealth(found);
        else if (playerHealth == null && found == null)
            SetPlayerHealth(null);
    }

    void SetPlayerHealth(Health newHealth)
    {
        if (playerHealth == newHealth) return;

        if (playerHealth != null && playerHealth.onHurt != null)
            playerHealth.onHurt.RemoveListener(OnPlayerHurt);

        playerHealth = newHealth;

        if (playerHealth != null && playerHealth.onHurt != null)
            playerHealth.onHurt.AddListener(OnPlayerHurt);

        _lastHealth = playerHealth != null ? playerHealth.currentHealth : int.MinValue;
        UpdateHealthText();

        if (logDebug)
            Debug.Log($"[UIHealthAndFlash] Bound Health='{(playerHealth != null ? playerHealth.name : "null")}'", this);
    }

    void UpdateHealthText()
    {
        if (playerHealth == null) return;

        if (healthInputField == null && healthText != null)
            healthInputField = healthText.GetComponentInParent<TMP_InputField>();

        if (healthText == null && healthInputField != null)
            healthText = healthInputField.textComponent;

        if (healthText == null && healthInputField == null) return;

        string value = showMaxHealth
            ? $"{playerHealth.currentHealth}/{playerHealth.maxHealth}"
            : $"{playerHealth.currentHealth}";

        if (healthInputField != null)
            healthInputField.SetTextWithoutNotify(value);

        if (healthText != null)
        {
            healthText.SetText(value);
            healthText.ForceMeshUpdate();
        }
        Canvas.ForceUpdateCanvases();
    }

    void SetFlashAlpha(float a)
    {
        if (damageFlashImage == null) return;
        var c = damageFlashImage.color;
        c.a = a;
        damageFlashImage.color = c;
    }
}
