using UnityEngine;

public class Basket : MonoBehaviour
{
    public float moveSpeed = 8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Apple"))
        {
            Destroy(other.gameObject);
            QLearningAgent.Instance.OnAppleCaught(); 
        }
    }
}
