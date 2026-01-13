using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Declaración del Singleton
    public static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null) Debug.Log("No hay GameManager");
            return instance;

        }
    
        //Fin del Singleton
    }
        //DECLARAMOS CUALQUIER VALOR GENERAL EN PUBLIC
        public int playerHealth = 3;
        public int playerPoints;
        public int timeLeft;
        public int bossMaxHealth = 6;
        public int bossHealth = 6;
    
        private void Awake()
        {
            if (instance == null)
            {
                //Si no hay GameManager, lo referenciamos y hacemos que perdure entre escenas
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                //Si ya hay GameManager, el duplicado se destruye
                Destroy(gameObject);
            }

        }

        private void Update()
        {
            if (playerHealth < 0) playerHealth = 0;
        }
        

}
