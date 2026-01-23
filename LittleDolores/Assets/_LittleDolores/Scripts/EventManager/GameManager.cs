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

    // ===== ESTADÍSTICAS DEL JUGADOR (FUENTE DE LA VERDAD) =====
    [Header("Player Stats")]
    public int playerHealth = 10;
    
    // Estas son las variables que usan los Powerups y el Player
    public int playerDamage = 1;            // Daño total
    public float currentMoveSpeed = 10f;    // Velocidad actual
    public float currentJumpForce = 15f;    // Fuerza de salto actual
    public bool hasDashAbility = false;     // ¿Tiene Dash?

    public Transform playerTransform;       
    public int playerPoints;
    
    // ===== CONFIGURACIÓN DEL BOSS =====
    [Header("Boss Config")]
    public string bossSceneName = "SCN_Boss";
    public int bossMaxHealth = 20;           
    public int bossCurrentHealth;              
    public bool isBossActive = false;
    public Transform bossTransform;

    // ===== GLOBAL AI & ESCENA =====
    [Header("Global AI State")]
    public bool enemiesAreUnhappy = false;  
    public int maxDefenders = 3;            
    public int currentDefenders = 0;        
    private HashSet<Transform> occupiedWeakSpots = new HashSet<Transform>();

    // ===== TIMER DE ESCENAS =====
    [Header("Scene Timer")]
    public float sceneTimeLimit = 60f;
    private bool isTimerActive = false;
    public float timeRemaining;
    private bool firstKillTriggered = false;
    
    // Paneles UI
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject victoryPanel;

    private List<GShroomEnemy> activeEnemies = new List<GShroomEnemy>();

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
        // Reseteo de estados de nivel al cargar escena
        enemiesAreUnhappy = false;
        firstKillTriggered = false;
        isTimerActive = false;
        currentDefenders = 0;
        occupiedWeakSpots.Clear();
        activeEnemies.Clear();
        
        isBossActive = false;
        bossCurrentHealth = bossMaxHealth;
        bossTransform = null;

        if (playerTransform == null)
        {
             GameObject p = GameObject.FindGameObjectWithTag("Player");
             if(p != null)
             {
                 playerTransform = p.transform;
             }
        }

        Time.timeScale = 1f;
        
        if(gameOverPanel) 
        {
            gameOverPanel.SetActive(false);
        }
        if(victoryPanel) 
        {
            victoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerHealth < 0) 
        {
            playerHealth = 0;
        }
        
        // Timer de Escena
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
    // MÉTODOS PARA LOS POWERUPS (Se llaman desde los Powerups)
    // =========================================================
    public void UpgradeJump(float amount) 
    { 
        currentJumpForce += amount; 
    }
    
    public void UpgradeSpeed(float amount) 
    { 
        currentMoveSpeed += amount; 
    }
    
    public void UpgradeDamage(int amount) 
    { 
        playerDamage += amount; 
    }
    
    public void UnlockDash() 
    { 
        hasDashAbility = true; 
    }

    // --- GESTIÓN DE VIDA DEL PLAYER ---
    public void TakeDamage(int damage)
    {
        playerHealth -= damage;
        if (playerHealth <= 0) 
        {
            TriggerGameOver();
        }
    }

    // --- GESTIÓN BOSS ---
    public void RegisterBoss(Transform boss)
    {
        bossTransform = boss;
        bossCurrentHealth = bossMaxHealth;
        isBossActive = true;
    }

    public void DamageBoss(int damage)
    {
        bossCurrentHealth -= damage;
        if (bossCurrentHealth <= 0) 
        {
            BossDefeated();
        }
    }

    void BossDefeated()
    {
        bossCurrentHealth = 0;
        isBossActive = false;
        if (victoryPanel) 
        {
            victoryPanel.SetActive(true);
        }
    }

    // --- GESTIÓN ENEMIGOS ---
    public void RegisterEnemy(GShroomEnemy enemy) 
    { 
        if(!activeEnemies.Contains(enemy)) 
        {
            activeEnemies.Add(enemy); 
        }
    }
    
    public void UnregisterEnemy(GShroomEnemy enemy) 
    { 
        if(activeEnemies.Contains(enemy)) 
        {
            activeEnemies.Remove(enemy);
        }
        
        if (activeEnemies.Count == 0 && isTimerActive)
        {
            isTimerActive = false;
            if (victoryPanel) 
            {
                victoryPanel.SetActive(true);
            }
        }
    }

    public void NotifyEnemyDeath()
    {
        if (!enemiesAreUnhappy) 
        {
            enemiesAreUnhappy = true;
        }
        
        string sceneName = SceneManager.GetActiveScene().name;
        if ((sceneName == "SCN_Aldea1" || sceneName == "SCN_Aldea2") && !firstKillTriggered)
        {
            timeRemaining = sceneTimeLimit;
            isTimerActive = true;
            firstKillTriggered = true;
        }
    }

    void TriggerGameOver()
    {
        isTimerActive = false;
        Time.timeScale = 0f;
        if (gameOverPanel) 
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RetryScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- IA UTILS ---
    public bool TryClaimWeakSpot(Transform spot) 
    { 
        if (occupiedWeakSpots.Contains(spot)) return false; 
        occupiedWeakSpots.Add(spot); 
        return true; 
    }
    
    public void ReleaseWeakSpot(Transform spot) 
    { 
        if (spot != null && occupiedWeakSpots.Contains(spot)) 
        {
            occupiedWeakSpots.Remove(spot); 
        }
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
        {
            currentDefenders--; 
        }
    }
}