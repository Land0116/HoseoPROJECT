using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopSkillItem : MonoBehaviour, IInteractable, IShopInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip purchaseSuccessClip;
    [SerializeField] private AudioClip purchaseFailClip;

    [Header("������")]
    [SerializeField] private SkillData[] skillPool;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    private SkillData currentSkill;
    private int price;

    private bool isPlayerInRange;
    private PlayerController player;

    private void Start()
    {
        GenerateItem();
    }

    private void Update()
    {
        if (currentSkill == null) return;
        RefreshPriceUI();

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = true;
        player = collision.GetComponent<PlayerController>();

        ItemUIManager.Instance?.RegisterShop(this);

        ShowUI();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = false;
        player = null;
        ItemUIManager.Instance?.UnregisterShop(this);
        ItemUIManager.Instance?.HideShopInteract();
    }
    private void GenerateItem()
    {
        if (skillPool == null || skillPool.Length == 0) return;

        currentSkill = skillPool[Random.Range(0, skillPool.Length)];
        if (currentSkill == null) return;

        price = currentSkill.price;

        if (iconImage != null)
            iconImage.sprite = currentSkill.icon;

        if (priceText != null) 
            priceText.text = ""+ GetFinalPrice().ToString()+"G";
    }

    private void RefreshPriceUI()
    {
        if (priceText == null) return;
        if (currentSkill == null) return;

        priceText.text = "" + GetFinalPrice().ToString() + "G";
    }
    public void Interact(PlayerController player)
    {
        this.player = player;
        TryPurchase();
    }

    private void TryPurchase()
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return;

        int finalPrice = GetFinalPrice();

        // 1. �� ����
        if (p.Gold < finalPrice)
        {
            ItemUIManager.Instance?.ShowAlertMessage("골드가 부족합니다.", false);
            PlayIndependent(purchaseFailClip);
            return;
        }

        // 2. ���� ����
        p.Gold -= finalPrice;

        HandleSkillAcquire(currentSkill);

        // 3. ���� UI
        ItemUIManager.Instance?.ShowAlertMessage("구매성공!", false);
        PlayIndependent(purchaseSuccessClip);

        gameObject.SetActive(false);
    }
    private void HandleSkillAcquire(SkillData skill)
    {
        SkillManager sm = SkillManager.Instance;

        // 1. ���� ��ų ���� �� ������ ���� (�ֿ켱)
        if (sm.qSkill == skill)
        {
            sm.EquipSkill(skill, SkillSlotType.Q);
            return;
        }

        if (sm.eSkill == skill)
        {
            sm.EquipSkill(skill, SkillSlotType.E);
            return;
        }

        // 2. Q ������� �� Q ����
        if (sm.qSkill == null)
        {
            sm.EquipSkill(skill, SkillSlotType.Q);
            return;
        }

        // 3. E ������� �� E ����
        if (sm.eSkill == null)
        {
            sm.EquipSkill(skill, SkillSlotType.E);
            return;
        }

        // 4. �� �� �ְ� �ٸ� ��ų �� ���� UI
        OpenReplaceUI(skill);
    }
    private void OpenReplaceUI(SkillData skill)
    {
        SkillSelectUIManager ui = SkillSelectUIManager.Instance;

        if (ui == null) return;

        ui.ForceSelectSkillFromShop(skill);
    }

    private void ShowUI()
    {
        if (ItemUIManager.Instance == null) return;
        if (currentSkill == null) return;

        ItemUIManager.Instance.ShowShopInteract(this, null); // ��ġ UI ������

        ItemUIManager.Instance.ShowShopSkillTooltip(currentSkill);
    }
    public Transform GetTransform()
    {
        return transform;
    }
    private int GetFinalPrice()
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return price;

        float discount = p.GetShopDiscountFromItems();
        return Mathf.RoundToInt(price * (1f - discount));
    }
    private void PlayIndependent(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        GameObject obj = new GameObject("ShopSkill_SFX_TEMP");
        AudioSource newSource = obj.AddComponent<AudioSource>();

        newSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        newSource.volume = audioSource.volume;
        newSource.pitch = audioSource.pitch;
        newSource.spatialBlend = 0f;

        newSource.clip = clip;
        newSource.Play();

        Destroy(obj, clip.length);
    }
}