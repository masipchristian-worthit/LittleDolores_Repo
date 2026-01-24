using UnityEngine;
using Unity.Cinemachine; // <--- CAMBIO IMPORTANTE: Ahora es Unity.Cinemachine

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    // CAMBIO IMPORTANTE: CinemachineVirtualCamera ahora se llama CinemachineCamera
    private CinemachineCamera cinemachineCamera; 
    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        cinemachineCamera = GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }
    }

    public void ShakeCamera(float intensity, float time)
    {
        // En Unity 6 / Cinemachine 3, el componente sigue existiendo pero se accede así:
        var perlin = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (perlin != null)
        {
            perlin.AmplitudeGain = intensity; // Nota: ya no lleva el prefijo m_
            startingIntensity = intensity;
            shakeTimerTotal = time;
            shakeTimer = time;
        }
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            var perlin = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

            if (perlin != null)
            {
                perlin.AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
            }
        }
    }
}