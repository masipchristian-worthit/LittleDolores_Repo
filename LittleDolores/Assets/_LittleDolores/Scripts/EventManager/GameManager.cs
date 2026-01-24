using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ===== SINGLETON =====
    public static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
            }
            return instance;
        }
    }

    // ===== ESTADÍSTICAS DEL JUGADOR =====
    [Header("Player Stats")]
    public int maxHealth = 10;
    public int playerHealth = 10;
    public int playerDamage = 1;
    public float currentMoveSpeed = 10f;
    public float currentJumpForce = 15f;
    public bool hasDashAbility = false;
    public Transform playerTransform;
    public int playerPoints;
    
    // ===== REGENERACIÓN DE VIDA =====
    [Header("Health Regeneration")]
    [SerializeField] float regenDelay = 15f;
    [Tooltip("Tiempo en segundos sin recibir daño antes de empezar a regenerar")]
    [SerializeField] int regenAmount = 1;
    [Tooltip("Cantidad de vida que se regenera")]
    [SerializeField] float regenInterval = 1f;
    [Tooltip("Intervalo en segundos entre cada regeneración")]
    
    private float timeSinceLastDamage = 0f;
    private float regenTimer = 0f;
    private bool isRegenerating = false;

    // ===== NUEVO: AUDIO SETTINGS =====
    [Header("Audio Settings")]
    [Tooltip("El índice del sonido de dolor en la librería SFX del AudioManager")]
    public int playerHurtSfxIndex = 0; 

    // ===== GLOBAL AI & ENEMIGOS =====
    [Header("Enemies & AI")]
    public bool enemiesAreUnhappy = false;
    public int maxDefenders = 3;
    public int currentDefenders = 0;
    private HashSet<Transform> occupiedWeakSpots = new HashSet<Transform>();
    private List<GShroomEnemy> activeEnemies = new List<GShroomEnemy>();

    // ===== TIMER DE ESCENAS =====
    [Header("Scene Timer")]
    public float sceneTimeLimit = 60f;
    private bool isTimerActive = false;
    public float timeRemaining;
    private bool firstKillTriggered = false;

    // Paneles UI
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject victoryPanel;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Suscribirse al evento de carga de escena solo una vez
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Evita errores al destruir el objeto
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Forzamos que el tiempo siempre sea 1 al empezar cualquier escena
        Time.timeScale = 1f;

        // Reset de estados lógicos
        enemiesAreUnhappy = false;
        firstKillTriggered = false;
        isTimerActive = false;
        currentDefenders = 0;
        occupiedWeakSpots.Clear();
        activeEnemies.Clear();
        timeRemaining = 0;
        
        // Reset regeneración
        timeSinceLastDamage = 0f;
        regenTimer = 0f;
        isRegenerating = false;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
    }

    private void Update()
    {
        playerHealth = Mathf.Clamp(playerHealth, 0, maxHealth);

        // Sistema de regeneración
        HandleHealthRegeneration();

        if (isTimerActive)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                TriggerGameOver();
            }
        }
    }

    void HandleHealthRegeneration()
    {
        // Solo regenerar si no está a vida máxima
        if (playerHealth >= maxHealth)
        {
            isRegenerating = false;
            timeSinceLastDamage = 0f;
            regenTimer = 0f;
            return;
        }

        // Incrementar el tiempo desde el último daño
        timeSinceLastDamage += Time.deltaTime;

        // Verificar si ha pasado el tiempo suficiente para empezar a regenerar
        if (timeSinceLastDamage >= regenDelay)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                regenTimer = 0f;
            }

            // Timer de regeneración
            regenTimer += Time.deltaTime;

            if (regenTimer >= regenInterval)
            {
                HealPlayer(regenAmount);
                regenTimer = 0f;
            }
        }
    }

    // --- MEJORAS ---
    public void UpgradeJump(float amount) { currentJumpForce += amount; }
    public void UpgradeSpeed(float amount) { currentMoveSpeed += amount; }
    public void UpgradeDamage(int amount) { playerDamage += amount; }
    public void UnlockDash() { hasDashAbility = true; }

    // --- VIDA (MODIFICADO PARA SONIDO Y REGENERACIÓN) ---
    public void TakeDamage(int damage)
    {
        playerHealth -= damage;

        // Resetear el timer de regeneración
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        regenTimer = 0f;

        // === SONIDO DE DOLOR ===
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(playerHurtSfxIndex);
        }
        // =======================

        if (playerHealth <= 0)
        {
            playerHealth = 0; // Evita valores negativos
            TriggerGameOver();
        }
    }

    public void HealPlayer(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxHealth) playerHealth = maxHealth;
    }

    // --- GESTIÓN ENEMIGOS ---
    public void RegisterEnemy(GShroomEnemy enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GShroomEnemy enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0 && isTimerActive)
        {
            isTimerActive = false;
            if (victoryPanel) victoryPanel.SetActive(true);
        }
    }

    public void NotifyEnemyDeath()
    {
        if (!enemiesAreUnhappy)
            enemiesAreUnhappy = true;

        if (!firstKillTriggered)
        {
            timeRemaining = sceneTimeLimit;
            isTimerActive = true;
            firstKillTriggered = true;
        }
    }

    // --- GAME OVER ---
    public void TriggerGameOver()
    {
        if (Time.timeScale == 0f) return; // Evita disparar el Game Over varias veces

        isTimerActive = false;
        Time.timeScale = 0f;

        if (gameOverPanel) gameOverPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // --- IA / SPOTS ---
    public bool TryClaimWeakSpot(Transform spot)
    {
        if (occupiedWeakSpots.Contains(spot)) return false;
        occupiedWeakSpots.Add(spot);
        return true;
    }

    public void ReleaseWeakSpot(Transform spot)
    {
        if (spot != null && occupiedWeakSpots.Contains(spot))
            occupiedWeakSpots.Remove(spot);
    }

    public bool TryJoinDefense()
    {
        if (currentDefenders < maxDefenders)
        {
            currentDefenders++;
            return true;
        }
        return false;
    }

    public void LeaveDefense()
    {
        if (currentDefenders > 0) currentDefenders--;
    }
}