using UnityEngine;
using UnityEngine.UI;

public class DustImage : MonoBehaviour
{
    private RectTransform rect;
    private Image img;

    private float lifeTime = 2.5f; 
    private float timer;

    private Vector2 moveDir;
    private float speed;
    private float swayAmount;

    public void Init()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();

        img.color = Color.white;

        speed = Random.Range(40f, 90f); 
        swayAmount = Random.Range(20f, 50f);

        moveDir = new Vector2(Random.Range(-0.3f, 0.3f), 1f);
    }

    void Update()
    {
        timer += Time.deltaTime;

        Vector2 pos = rect.anchoredPosition;
        pos += moveDir * speed * Time.deltaTime;

        pos.x += Mathf.Sin(Time.time * 2.5f) * swayAmount * Time.deltaTime;

        rect.anchoredPosition = pos;

        float alpha = 1f - (timer / lifeTime);
        img.color = new Color(1, 1, 1, alpha);

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}