using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ===== SINGLETON =====
    public static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
                Debug.Log("No hay AudioManager");
            return instance;
        }
    }
    // ===== FIN SINGLETON =====


    [Header("Galería de Sonidos")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voicelinesSource;

    public AudioClip[] musicLibrary;
    public AudioClip[] sfxLibrary;
    public AudioClip[] voicelinesLibrary;

    // =========================================================
    // ¡ESTA ES LA VARIABLE QUE TE FALTA!
    // Sirve para que el VoiceTrigger sepa si hay alguien hablando
    // =========================================================
    [HideInInspector] 
    public bool isPriorityPlaying = false; 
    // =========================================================

    void Awake()
    {
        // Si no hay AudioManager, lo referenciamos
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(int musicToPlay)
    {
        if (musicToPlay < 0 || musicToPlay >= musicLibrary.Length) return;

        musicSource.clip = musicLibrary[musicToPlay];
        musicSource.Play();
    }

    public void PlaySFX(int sfxToPlay)
    {
        if (sfxToPlay < 0 || sfxToPlay >= sfxLibrary.Length) return;

        sfxSource.PlayOneShot(sfxLibrary[sfxToPlay]);
    }

    public void PlayVoicelines(int voicelinesToPlay)
    {
        if (voicelinesToPlay < 0 || voicelinesToPlay >= voicelinesLibrary.Length) return;

        // Aquí usamos PlayOneShot para que suene aunque haya otros sonidos
        voicelinesSource.PlayOneShot(voicelinesLibrary[voicelinesToPlay]);
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