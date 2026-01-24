using UnityEngine;
using Unity.Cinemachine; // Si usas la versión nueva de Cinemachine
// using Cinemachine; // Descomenta esta y borra la de arriba si usas una versión antigua

public class CameraFollowFix : MonoBehaviour
{
    void Start()
    {
        // 1. Buscamos al Player por su Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 2. Obtenemos la Virtual Camera
            var vcam = GetComponent<CinemachineCamera>();

            // 3. Le decimos a quién seguir y a quién mirar
            vcam.Follow = player.transform;
            vcam.LookAt = player.transform;
        }
        else
        {
            Debug.LogWarning("La cámara no encontró al Player para seguirlo.");
        }
    }
}