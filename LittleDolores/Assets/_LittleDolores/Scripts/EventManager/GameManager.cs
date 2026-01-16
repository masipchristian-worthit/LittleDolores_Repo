using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ===== SINGLETON =====
    public static GameManager Instance;

    // ===== CONFIGURACIÓN JUGADOR =====
    [Header("Player Stats")]
    public int maxHealth = 10;
    public int currentHealth;
    public Transform playerTransform;

    [Header("Regeneración de Vida")]
    public float regenDelay = 4f;    // Tiempo sin daño para empezar a curar
    public float regenRate = 1f;     // Tiempo entre curaciones (segundos)
    private float lastDamageTime;
    private float regenTimer;

    // ===== PERSISTENCIA DE POWERUPS =====
    // Variables globales que no se borran al reiniciar escena
    [Header("Powerups Persistentes")]
    public float currentJumpForce = 7f;   // Valor base editable
    public float currentMoveSpeed = 5f;   // Valor base editable
    public int bonusDamage = 0;
    public bool hasDashAbility = false;

    [Header("Configuración Powerups")]
    public GameObject damageParticlePrefab; // Asigna el prefab de partículas aquí (aunque esté comentado el uso)

    // ===== CONTROL DE ESCENAS & TIEMPO =====
    [Header("Contrarreloj Escenas")]
    [SerializeField] float sceneTimeLimit = 60f; // Tiempo para Aldea1/2
    [SerializeField] GameObject gameOverPanel;   // Panel con fondo oscuro y botón Retry
    [SerializeField] GameObject victoryPanel;    // Panel de "Keep Going"
    
    private bool isTimerActive = false;
    private float timeRemaining;
    private bool levelCompleted = false;

    // ===== INTELIGENCIA ARTIFICIAL =====
    [Header("Global AI")]
    public bool enemiesAreUnhappy = false;
    public int maxDefenders = 3;
    public int currentDefenders = 0;
    
    private HashSet<Transform> occupiedWeakSpots = new HashSet<Transform>();
    private List<GShroomEnemy> activeEnemies = new List<GShroomEnemy>();
    private bool firstKillTriggered = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void InitializeGame()
    {
        currentHealth = maxHealth;
        FindPlayer();
        SceneManager.sceneLoaded += OnSceneLoaded; // Suscribirse al evento de cambio de escena
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar escena, reiniciamos lógica de nivel PERO MANTENEMOS POWERUPS
        enemiesAreUnhappy = false;
        firstKillTriggered = false;
        isTimerActive = false;
        levelCompleted = false;
        currentHealth = maxHealth; // Opcional: ¿Quieres resetear vida al reiniciar? Asumo que sí.
        
        activeEnemies.Clear();
        FindPlayer();

        // Asegurarse de que el tiempo esté normal (por si venimos de un pause)
        Time.timeScale = 1f; 
        if(gameOverPanel) gameOverPanel.SetActive(false);
        if(victoryPanel) victoryPanel.SetActive(false);
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    // Registro de enemigos para saber cuándo se acaban
    public void RegisterEnemy(GShroomEnemy enemy)
    {
        if(!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GShroomEnemy enemy)
    {
        if(activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
        CheckWinCondition();
    }

    void Update()
    {
        if (playerTransform == null) FindPlayer();

        HandleHealthRegen();
        HandleSceneTimer();
    }

    // --- SISTEMA DE SALUD ---
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        lastDamageTime = Time.time; // Reseteamos el contador de regen
        
        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    void HandleHealthRegen()
    {
        if (currentHealth < maxHealth && currentHealth > 0)
        {
            if (Time.time - lastDamageTime > regenDelay)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= regenRate)
                {
                    currentHealth++;
                    regenTimer = 0;
                    Debug.Log("Regenerando vida... " + currentHealth);
                }
            }
        }
    }

    // --- SISTEMA DE TIEMPO ---
    public void NotifyEnemyDeath()
    {
        if (!enemiesAreUnhappy) enemiesAreUnhappy = true;

        // Lógica de contador para Aldea1 y Aldea2
        string sceneName = SceneManager.GetActiveScene().name;
        if ((sceneName == "SCN_Aldea1" || sceneName == "SCN_Aldea2") && !firstKillTriggered)
        {
            StartTimer();
            firstKillTriggered = true;
        }
    }

    void StartTimer()
    {
        timeRemaining = sceneTimeLimit;
        isTimerActive = true;
        Debug.Log("¡TIEMPO INICIADO! CORRE.");
    }

    void HandleSceneTimer()
    {
        if (isTimerActive && !levelCompleted)
        {
            timeRemaining -= Time.deltaTime;
            // Aquí podrías actualizar una UI de texto con timeRemaining

            if (timeRemaining <= 0)
            {
                TriggerGameOver();
            }
        }
    }

    void CheckWinCondition()
    {
        // Si no quedan enemigos y el temporizador estaba activo
        if (activeEnemies.Count == 0 && isTimerActive)
        {
            LevelCompleted();
        }
    }

    void LevelCompleted()
    {
        isTimerActive = false;
        levelCompleted = true;
        if (victoryPanel) victoryPanel.SetActive(true); // Mostrar "Keep Going..."
        Debug.Log("NIVEL COMPLETADO");
    }

    void TriggerGameOver()
    {
        isTimerActive = false;
        Time.timeScale = 0f; // Pausa total
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // Botón del UI "Try Again"
    public void RetryScene()
    {
        Time.timeScale = 1f;
        // Recargar la escena actual. Los powerups se mantienen porque GM es DontDestroyOnLoad
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- MÉTODOS POWERUPS ---
    public void UpgradeJump(float amount) { currentJumpForce += amount; }
    public void UpgradeSpeed(float amount) { currentMoveSpeed += amount; }
    public void UpgradeDamage(int amount) 
    { 
        bonusDamage += amount;
        // if(damageParticlePrefab) Instantiate(damageParticlePrefab, playerTransform); // Comentado por ahora
    }
    public void UnlockDash() { hasDashAbility = true; }

    // --- GESTIÓN IA (Flanqueo y Defensa) ---
    public bool TryClaimWeakSpot(Transform spot) { if (occupiedWeakSpots.Contains(spot)) return false; occupiedWeakSpots.Add(spot); return true; }
    public void ReleaseWeakSpot(Transform spot) { if (spot != null && occupiedWeakSpots.Contains(spot)) occupiedWeakSpots.Remove(spot); }
    public bool TryJoinDefense() { if (currentDefenders < maxDefenders) { currentDefenders++; return true; } return false; }
    public void LeaveDefense() { if (currentDefenders > 0) currentDefenders--; }
}