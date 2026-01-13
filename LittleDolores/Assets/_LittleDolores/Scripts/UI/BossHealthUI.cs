using UnityEngine;
using UnityEngine.UI;
public class HealthUI : MonoBehaviour
{

    [SerializeField] Image bossHealthFill;
    void Update()
    {
        bossHealthFill.fillAmount = GameManager.Instance.bossHealth / GameManager.Instance.bossMaxHealth;
    }

}
