using UnityEngine;

/// <summary>
/// 負責處理「卡片被放到某個 UiSlot 上」這件事：
/// - 接收 SlotDropEvent（拖曳邏輯在別處）
/// - 把 UiCard / UiSlot 轉成 CardInstance / Slot
/// - 呼叫 BoardController.TryMoveCard
/// - 如果移動違規，就請 UiCard 退回模型目前的位置
/// UI 真正的移動與動畫交給 CardMovementPresenter 處理。
/// </summary>
public class CardDropInputHandler : MonoBehaviour
{
    private Board board;

    /// <summary>
    /// 如果你想在程式初始化時注入 BoardController，可以用這個。
    /// 也可以不呼叫，直接在 Inspector 配 board。
    /// </summary>
    public void Initialize(Board boardController)
    {
        board = boardController;

        // 保險起見，先退訂再訂（避免多次 Initialize 重複訂閱）
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void Awake()
    {
        // 如果你是用 Inspector 填 board，又沒呼叫 Initialize，
        // 這裡可以保底訂一次。
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void OnSlotDrop(SlotDropEvent e)
    {
        if (board == null)
        {
            Debug.LogWarning("CardDropHandler: BoardController is null.");
            return;
        }

        var uiSlot = e.Slot;
        var uiCard = e.Card;

        if (uiSlot == null || uiCard == null)
            return;

        var instance = uiCard.cardInstance;
        var fromSlot = instance?.CurrentSlot;
        var toSlot = uiSlot.ModelSlot;

        if (instance == null || fromSlot == null || toSlot == null)
            return;

        if (!board.TryMoveCard(instance, fromSlot, toSlot, MoveType.Player, out var reason))
        {
            Debug.Log($"Move rejected: {reason}");
            // Model 沒變，讓卡片回到「目前模型認定的位置」
            uiCard.AnimateToCurrentZone();
            return;
        }

        // ✅ Move 成功的情況：
        // 不在這裡直接動 UI，等待 BoardController.OnCardMoved
        // 由 CardMovementPresenter 統一處理畫面更新。
    }
}