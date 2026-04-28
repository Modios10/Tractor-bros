using UnityEngine;

public class TractorCollector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si el objeto tocado tiene la etiqueta "Grain"
        if (other.CompareTag("Grain"))
        {
            GameManager.Instance.AddGrain(1);
            Destroy(other.gameObject); // Destruye el trigo
        }
        // Si el objeto tocado tiene la etiqueta "obstaculo"
        else if (other.CompareTag("Obstaculo"))
        {
            bool applyDamage = true;
            if (other.TryGetComponent<BombHitFeedback>(out BombHitFeedback feedback))
            {
                applyDamage = feedback.HandleHit();
            }

            if (!applyDamage)
            {
                return;
            }

            GameManager.Instance?.LoseLife();

            if (feedback == null)
            {
                Destroy(other.gameObject);
            }
        }
        else if (other.CompareTag("ObstaculoInmobil"))
        {
            // L�gica para el obst�culo inm�vil
            GameManager.Instance?.LoseLife();

        }
    }
}
