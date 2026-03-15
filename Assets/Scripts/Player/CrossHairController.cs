using System;
using UnityEngine;

public class CrossHairController : MonoBehaviour
{
    [SerializeField] private GameObject[] crossHair;
    
    private void Awake()
    {
        crossHair = new GameObject[transform.childCount];
        int index = 0;

        // 2. foreach 문으로 자식 오브젝트들을 배열에 담기
        // (부모 자신은 제외하고 직계 자식들만 순회합니다)
        foreach (Transform child in transform)
        {
            crossHair[index] = child.gameObject;
            index++;
        }
        Debug.Log("크로스헤어 동기화");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("TestEnemy"))
        {
            foreach (GameObject go in crossHair)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color =  Color.red;
                    //Debug.Log("조준점에 적이 들어옴");
                }
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("TestEnemy"))
        {
            foreach (GameObject go in crossHair)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color =  Color.white;
                    //Debug.Log("조준점에 적이 나감");
                }
            }
        }
    }
}
