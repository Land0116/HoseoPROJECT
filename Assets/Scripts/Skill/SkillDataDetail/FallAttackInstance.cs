using UnityEngine;
using System.Collections;

public class FallAttackInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private float delay = 2f;

    private GameObject attackPrefab; // 추가

    [SerializeField] private SpriteRenderer warningSprite;

    public void Init(float dmg, float rng, float cd)
    {
        damage = dmg;
        range = rng;

        transform.localScale = Vector3.one * (range * 0.3f);

        StartCoroutine(FallRoutine());
    }

    //  attackPrefab 따로 세팅하는 함수
    public void SetAttackPrefab(GameObject prefab)
    {
        attackPrefab = prefab;
    }

    private IEnumerator FallRoutine()
    {
        float t = 0f;

        Color c = warningSprite.color;
        c.a = 0f;
        warningSprite.color = c;

        Vector3 startScale = Vector3.one * (range * 0.3f);
        Vector3 endScale = Vector3.one * range;

        while (t < delay)
        {
            t += Time.deltaTime;

            float progress = Mathf.Clamp01(t / delay);

            c.a = progress * 0.85f;
            warningSprite.color = c;

            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            yield return null;
        }

        transform.localScale = endScale;

        if (attackPrefab != null)
        {
            GameObject atk = Instantiate(attackPrefab, transform.position, Quaternion.identity);
            atk.transform.localScale = Vector3.one * range;

            FallAttackDamage dmgComp = atk.GetComponent<FallAttackDamage>();
            if (dmgComp != null)
            {
                dmgComp.Init(damage);
            }
        }

        Destroy(gameObject);
    }
}