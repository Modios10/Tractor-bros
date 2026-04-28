using UnityEngine;

public class BombHitFeedback : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Bomb_Idle";
    [SerializeField] private string explodeTrigger = "Explode";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private float explosionLifetime = 0.9f;

    [Header("Collision")]
    [SerializeField] private Collider2D damageCollider;

    [Header("Roots")]
    [SerializeField] private GameObject bombVisualRoot;
    [SerializeField] private Transform explosionRoot;
    [SerializeField] private bool detachExplosionOnHit = true;
    [SerializeField] private int explosionSortingOrderOffset = 100;

    private bool hasExploded = false;
    private int baseSortingOrder = 0;

    private void Awake()
    {
        hasExploded = false;

        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(explodeTrigger))
            {
                animator.ResetTrigger(explodeTrigger);
            }

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);
            }
        }

        if (explosionRoot != null)
        {
            explosionRoot.gameObject.SetActive(false);
        }

        CacheBaseSortingOrder();
    }

    private void Reset()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        damageCollider = GetComponent<Collider2D>();
        bombVisualRoot = gameObject;
        explosionRoot = transform;
    }

    public bool HandleHit()
    {
        if (hasExploded)
        {
            return false;
        }

        hasExploded = true;

        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        if (bombVisualRoot != null && bombVisualRoot != gameObject)
        {
            bombVisualRoot.SetActive(false);
        }

        if (explosionRoot != null)
        {
            if (detachExplosionOnHit)
            {
                explosionRoot.SetParent(null, true);
            }

            SetSortingOrderForRoot(explosionRoot, baseSortingOrder + explosionSortingOrderOffset);
            explosionRoot.gameObject.SetActive(true);
        }

        if (animator != null && !string.IsNullOrEmpty(explodeTrigger))
        {
            animator.ResetTrigger(explodeTrigger);
            animator.SetTrigger(explodeTrigger);
        }

        if (audioSource != null && hitClip != null)
        {
            audioSource.PlayOneShot(hitClip);
        }

        float life = Mathf.Max(0.1f, explosionLifetime);
        if (hitClip != null)
        {
            life = Mathf.Max(life, hitClip.length);
        }

        if (explosionRoot != null)
        {
            Destroy(explosionRoot.gameObject, life);
        }

        Destroy(gameObject, life + 0.05f);
        return true;
    }

    private void CacheBaseSortingOrder()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            baseSortingOrder = 0;
            return;
        }

        baseSortingOrder = renderers[0].sortingOrder;
    }

    private void SetSortingOrderForRoot(Transform root, int order)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = order + i;
        }
    }
}
