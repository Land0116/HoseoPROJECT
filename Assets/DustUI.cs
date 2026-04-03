using UnityEngine;

public class DustUI : MonoBehaviour
{
    public float speed = 20f;
    public float fadeSpeed = 0.5f;
    
    void Start()
    {
       
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
        //cg.alpha -= fadeSpeed * Time.deltaTime;

        //if (cg.alpha <= 0)
            Destroy(gameObject);
    }
}