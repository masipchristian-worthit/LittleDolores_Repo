using UnityEngine;

public class AnimationSFXTriggers : MonoBehaviour
{
    
    public void PlayAnimationSFX(int sfxToPlay)
    {
        AudioManager.instance.PlaySFX(sfxToPlay);
    }

}
