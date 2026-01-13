using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Declaración del Singleton
    private static GameManager instance;
    private static GameManager Instance
    {
        get
        {
            if (instance == null) Debug.Log("No hay GameManager");
            return instance;

        }
    
        //Fin del Singleton
    }
        //DECLARAMOS CUALQUIER VALOR GENERAL EN PUBLIC
        public int playerHealth;
        public int playerPoints;
        public int timeLeft;
    
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
