using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CrossHairController : MonoBehaviour
{
    [SerializeField] private Image[] crossHair;

    private Camera mainCamera;

    private void Awake()
    {
        crossHair = GetComponentsInChildren<Image>();
        mainCamera = Camera.main;

        Debug.Log("크로스헤어 동기화");
    }

    private void Update()
    {
        CheckEnemyUnderMouse();
    }

    void CheckEnemyUnderMouse()
    {
        if (mainCamera == null)
        {
            
            return;
        }

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(
            Mouse.current.position.ReadValue()
        );

        

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorld, Vector2.zero);

        if (hits.Length == 0)
        {
            
            SetColor(Color.white);
            return;
        }

        foreach (var hit in hits)
        {
            

            if (hit.collider.CompareTag("Monster"))
            {
                SetColor(Color.red);
                return;
            }
        }

        SetColor(Color.white);
    }

    private void SetColor(Color color)
    {
        foreach (var img in crossHair)
        {
            if (img != null)
            {
                img.color = color;
            }
        }
    }
}