using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHPUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0.8f, 0);

    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;

    private Monster monster;


    public void Init(Monster target)
    {
        monster = target;
    }

    void Update()
    {
        if (monster == null) return;

        float current = monster.CurrentHP;
        float max = monster.MaxHP;

        hpBar.value = current / max;

        hpText.text = $"{(int)current} / {(int)max}";
    }

    void LateUpdate()
    {
        if (monster == null) return;

        transform.position = monster.transform.position + offset;

        transform.rotation = Quaternion.identity;
    }
}