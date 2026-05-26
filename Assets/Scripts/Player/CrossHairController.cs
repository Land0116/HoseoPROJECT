using UnityEngine;

public class CrossHairController : MonoBehaviour
{
    [SerializeField] private GameObject[] crossHair;

    [Header("몬스터 감지")]
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField] private float detectRadius = 0.15f;

    private readonly Collider2D[] hitBuffer = new Collider2D[8];
    private bool wasOnMonster;

    private void Awake()
    {
        crossHair = new GameObject[transform.childCount];

        int index = 0;
        foreach (Transform child in transform)
        {
            crossHair[index] = child.gameObject;
            index++;
        }
    }

    private void Update()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            detectRadius,
            hitBuffer,
            monsterLayerMask
        );

        bool isOnMonster = hitCount > 0;

        if (isOnMonster == wasOnMonster)
            return;

        wasOnMonster = isOnMonster;
        SetCrosshairColor(isOnMonster ? Color.red : Color.white);
    }

    private void SetCrosshairColor(Color color)
    {
        if (crossHair == null) return;

        for (int i = 0; i < crossHair.Length; i++)
        {
            if (crossHair[i] == null) continue;

            SpriteRenderer sr = crossHair[i].GetComponent<SpriteRenderer>();

            if (sr != null)
                sr.color = color;
        }
    }
}