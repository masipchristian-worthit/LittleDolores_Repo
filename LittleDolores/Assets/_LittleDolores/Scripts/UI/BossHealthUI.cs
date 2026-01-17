using UnityEngine;
using UnityEngine.UI;
public class BossHealthUI : MonoBehaviour
{

    [SerializeField] Image bossHealthFill;
    void Update()
    {
        bossHealthFill.fillAmount = GameManager.Instance.bossCurrentHealth / GameManager.Instance.bossMaxHealth;
    }

}
