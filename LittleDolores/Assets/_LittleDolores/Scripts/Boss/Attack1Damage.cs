using UnityEngine;

public class Attack1Damage : MonoBehaviour
{
    public int damage = 1;
    public float autoDeactivateTime = 2f;
    public string targetTag = "Player";
    public string playerAttackTag = "PlayerAttack";

    private void OnEnable()
    {
        Invoke(nameof(Deactivate), autoDeactivateTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            GameManager.Instance.TakeDamage(damage);
            Deactivate();
        }
        else if (other.CompareTag(playerAttackTag))
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        CancelInvoke();
        gameObject.SetActive(false);
    }
}
