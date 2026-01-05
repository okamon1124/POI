using UnityEngine;

/// <summary>
/// 負責「當模型中的卡片移動時，UI 要怎麼跟著動」。
/// - 監聽 BoardController.OnCardMoved
/// - 用 UiRegistry 找到對應的 UiCard / UiSlot
/// - 處理 Attach / Detach / RectTransform parent
/// - 交給 UiCard.AnimateToCurrentZone() 播動畫 / 排版
/// </summary>
public class CardMovementPresenter : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private RectTransform cardsOnBoardRoot;
    [SerializeField] private RectTransform cardsInHandRoot;

    [Header("Registry")]
    [SerializeField] private UiRegistry uiRegistry;

    private Board board;

    public void Initialize(Board boardController)
    {
        if (board != null)
        {
            board.OnCardMoved -= HandleCardMoved;
        }

        board = boardController;

        if (board != null)
        {
            board.OnCardMoved += HandleCardMoved;
        }
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnCardMoved -= HandleCardMoved;
        }
    }

    private void HandleCardMoved(CardInstance card, Slot from, Slot to)
    {
        if (card == null || to == null || uiRegistry == null)
            return;

        var uiCard = uiRegistry.GetUiCard(card);
        if (uiCard == null)
            return;

        // ------- Detach from old UiSlot (if any) -------
        if (from != null)
        {
            var fromUiSlot = uiRegistry.GetUiSlot(from);
            if (fromUiSlot != null)
            {
                fromUiSlot.DetachCard(uiCard);
            }
        }

        // ------- Attach to new UiSlot / set parent -------
        var toUiSlot = uiRegistry.GetUiSlot(to);

        if (to.Zone.Type != ZoneType.Hand)
        {
            // 非手牌區：讓 UiSlot 管理「這格目前有哪些卡」
            if (toUiSlot != null)
            {
                toUiSlot.AttachCard(uiCard);
            }

            if (cardsOnBoardRoot != null)
            {
                uiCard.transform.SetParent(cardsOnBoardRoot, worldPositionStays: false);
            }
        }
        else
        {
            // 手牌區：通常用 HandLayout / SplineLayout 排版
            if (cardsInHandRoot != null)
            {
                uiCard.transform.SetParent(cardsInHandRoot, worldPositionStays: false);
            }
        }

        // ------- 讓 UiCard 根據 cardInstance.CurrentSlot + UiRegistry 決定具體位置 -------
        uiCard.AnimateToCurrentZone();
    }
}