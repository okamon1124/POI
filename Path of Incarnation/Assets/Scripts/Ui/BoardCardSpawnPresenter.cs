using UnityEngine;

public class BoardCardSpawnPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry uiRegistry;

    [Header("Prefab & Parent")]
    [SerializeField] private UiCard cardPrefab;

    /// <summary>
    /// 場上卡片的父物件（一般放在 Board UI root 下面）
    /// </summary>
    [SerializeField] private RectTransform boardCardsRoot;

    public void Initialize(Board boardController, UiRegistry uiReg)
    {
        if (board != null)
            board.OnCardSpawned -= OnCardSpawned;

        board = boardController;
        uiRegistry = uiReg;

        if (board != null)
            board.OnCardSpawned += OnCardSpawned;
    }

    private void OnDestroy()
    {
        if (board != null)
            board.OnCardSpawned -= OnCardSpawned;
    }

    private void OnCardSpawned(CardInstance instance, Slot slot)
    {
        // ⭐ 這個 Presenter 只處理「非手牌」的生成
        if (slot.Zone.Type == ZoneType.Hand)
            return;

        // 1) 找到對應的 UiSlot（一定要用 registry 查）
        UiSlot uiSlot = uiRegistry.GetUiSlot(slot);
        if (uiSlot == null)
        {
            Debug.LogWarning($"[BoardCardSpawnPresenter] No UiSlot found for Slot in Zone {slot.Zone.Type} index {slot.Index}");
            return;
        }

        // 2) 該位置已經有卡？通常不允許
        if (uiSlot.Occupants.Count > 0)
        {
            Debug.LogWarning($"[BoardCardSpawnPresenter] Slot {uiSlot.name} already contains a card.");
            return;
        }

        // 3) Instantiate 卡片
        UiCard uiCard = Instantiate(cardPrefab, boardCardsRoot);
        uiCard.cardInstance = instance;
        uiCard.BindRegistry(uiRegistry);

        // 註冊到 registry
        uiRegistry.Register(uiCard);

        // 4) 把卡片移動到 slot (不用動畫，直接對齊)
        var cardRT = uiCard.GetComponent<RectTransform>();
        var slotRT = uiSlot.RectTransform;

        cardRT.position = slotRT.position;
        cardRT.rotation = slotRT.rotation;
        cardRT.localScale = Vector3.one * 1.1f;

        // 5) 讓 slot 記得這張 UI 卡片
        uiSlot.AttachCard(uiCard);

        // 6) 更新卡面
        uiCard.RefreshVisual();
    }
}