using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    //Declaración del Singleton
    private static AudioManager instance;
    public static AudioManager Instance
{
    get
    {
        if (instance == null) Debug.Log("No hay AudioManager");
        return instance;
    }
}

//Fin del Singleton

    [Header("Galería de Sonidos")]
    public AudioClip[] musicSource;
    public AudioClip[] sfxSource;
    public AudioClip[] voicelinesSource;
    public AudioClip[] sfxLibrary;
    public AudioClip[] musicLibrary;
    public AudioClip[] voicelinesLibrary;


    void Awake()
        {
            //Si no hay audio manager, lo referenciamos
            if (instance == null)
            {
                instance = this;
                // DontDestroyOnLoad(gameObject); // Que perdure entre escenaas
            }
            else
            {
                //Si ya hay uno, destruimos el nuevo
                Destroy(gameObject);
            }
        }

        public void PlayMusic (int musicToPlay)
        {
            musicSource.clip = musicLibrary[musicToPlay];
            musicSource.Play(); //Reproducir la música desde el principio
        }

        public void PlaySFX(int sfxToPlay)
         {
            sfxSource.PlayOneShot(sfxLibrary[sfxToPlay]);
            sfxSource.Play(); //Reproducir el SFX desde el principio
         }

        public void PlayVoicelines(int voicelinesToPlay)
        {
            voicelinesSource.PlayOneShot(voicelinesLibrary[voicelinesToPlay]);
            voicelinesSource.Play(); //Reproducir la línea de voz desde el principio
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void StopSFX()
        {
            sfxSource.Stop();
        }

        public void StopVoicelines()
        {
            voicelinesSource.Stop();
        }

        public void PauseMusic()
        {
            musicSource.Pause();
        }
        public void PauseSFX()
        {
            sfxSource.Pause();
        }
        public void PauseVoicelines()
        {
            voicelinesSource.Pause();
        }

        public void UnPauseMusic()
        {
            musicSource.UnPause();
        }
        public void UnPauseSFX()
        {
            sfxSource.UnPause();
        }
        public void UnPauseVoicelines()
        {
            voicelinesSource.UnPause();
        }


}

    
    
