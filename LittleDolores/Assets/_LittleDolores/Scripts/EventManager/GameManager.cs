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
    public int maxHealth = 10;        // Vida máxima del jugador
    public int playerHealth = 10;     // Vida actual del jugador

    public int playerDamage = 1;       // Daño total
    public float currentMoveSpeed = 10f;
    public float currentJumpForce = 15f;
    public bool hasDashAbility = false;

    public Transform playerTransform;
    public int playerPoints;

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
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset de estados al cargar la escena
        enemiesAreUnhappy = false;
        firstKillTriggered = false;
        isTimerActive = false;
        currentDefenders = 0;
        occupiedWeakSpots.Clear();
        activeEnemies.Clear();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
        }

        Time.timeScale = 1f;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
    }

    private void Update()
    {
        // Limitar la vida del jugador entre 0 y maxHealth
        playerHealth = Mathf.Clamp(playerHealth, 0, maxHealth);

        // Timer de escena
        if (isTimerActive)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                TriggerGameOver();
            }
        }
    }

    // =========================================================
    // MÉTODOS PARA POWERUPS / MEJORAS
    // =========================================================
    public void UpgradeJump(float amount) { currentJumpForce += amount; }
    public void UpgradeSpeed(float amount) { currentMoveSpeed += amount; }
    public void UpgradeDamage(int amount) { playerDamage += amount; }
    public void UnlockDash() { hasDashAbility = true; }

    // --- GESTIÓN DE VIDA DEL PLAYER ---
    public void TakeDamage(int damage)
    {
        playerHealth -= damage;
        if (playerHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    public void HealPlayer(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxHealth)
            playerHealth = maxHealth;
    }

    // --- GESTIÓN ENEMIGOS ---
    public void RegisterEnemy(GShroomEnemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GShroomEnemy enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0 && isTimerActive)
        {
            isTimerActive = false;
            if (victoryPanel)
                victoryPanel.SetActive(true);
        }
    }

    public void NotifyEnemyDeath()
    {
        if (!enemiesAreUnhappy)
            enemiesAreUnhappy = true;

        string sceneName = SceneManager.GetActiveScene().name;
        if ((sceneName == "SCN_Aldea1" || sceneName == "SCN_Aldea2") && !firstKillTriggered)
        {
            timeRemaining = sceneTimeLimit;
            isTimerActive = true;
            firstKillTriggered = true;
        }
    }

    // --- GAME OVER / REINTENTO ---
    void TriggerGameOver()
    {
        isTimerActive = false;
        Time.timeScale = 0f;
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void RetryScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- UTILIDADES IA / DEFENSA ---
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
        if (currentDefenders > 0)
            currentDefenders--;
    }
}
