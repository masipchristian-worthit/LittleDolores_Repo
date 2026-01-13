using UnityEngine;
using UnityEngine.UI;
public class BossHealthUI : MonoBehaviour
{

    [SerializeField] Image bossHealthFill;
    void Update()
    {
        bossHealthFill.fillAmount = GameManager.Instance.bossHealth / GameManager.Instance.bossMaxHealth;
    }

}
