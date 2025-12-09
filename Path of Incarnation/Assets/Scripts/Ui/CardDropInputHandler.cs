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
    private PhaseManager phaseManager;

    // 用來標記「這個 Main Phase 的編號」
    private int _mainPhaseId = 0;

    // 目前拖曳是在哪一個 Main Phase 開始的（如果不是在 Main 開始，為 null）
    private int? _activeDragMainPhaseId = null;

    /// <summary>
    /// Initialize with board + phase manager.
    /// </summary>
    public void Initialize(Board boardController, PhaseManager phaseManager)
    {
        board = boardController;
        this.phaseManager = phaseManager;

        // 先退訂再訂（避免多次 Initialize 重複訂閱）
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);

        EventBus.Unsubscribe<CardDragBeginEvent>(OnCardDragBegin);
        EventBus.Subscribe<CardDragBeginEvent>(OnCardDragBegin);

        EventBus.Unsubscribe<CardDragEndEvent>(OnCardDragEnd);
        EventBus.Subscribe<CardDragEndEvent>(OnCardDragEnd);

        if (this.phaseManager != null)
        {
            this.phaseManager.OnPhaseEntered -= OnPhaseEntered;
            this.phaseManager.OnPhaseEntered += OnPhaseEntered;
        }
    }

    private void Awake()
    {
        // 如果你是用 Inspector 填 board / phaseManager，又沒呼叫 Initialize，
        // 這裡可以至少訂 SlotDropEvent。CardDrag / Phase 相關如果沒設 phaseManager，就只會走保險邏輯。
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);

        EventBus.Unsubscribe<CardDragBeginEvent>(OnCardDragBegin);
        EventBus.Subscribe<CardDragBeginEvent>(OnCardDragBegin);

        EventBus.Unsubscribe<CardDragEndEvent>(OnCardDragEnd);
        EventBus.Subscribe<CardDragEndEvent>(OnCardDragEnd);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Unsubscribe<CardDragBeginEvent>(OnCardDragBegin);
        EventBus.Unsubscribe<CardDragEndEvent>(OnCardDragEnd);

        if (phaseManager != null)
            phaseManager.OnPhaseEntered -= OnPhaseEntered;
    }

    // ===== Phase 事件：用來辨識「這是第幾個 Main Phase」 =====

    private void OnPhaseEntered(PhaseType phase)
    {
        if (phase == PhaseType.Main)
        {
            // 每次進入 Main，都 ++，代表新的 Main（新回合的主要階段）
            _mainPhaseId++;
        }
    }

    // ===== 拖曳事件：記錄拖曳是在哪個 Main Phase 開始 =====

    private void OnCardDragBegin(CardDragBeginEvent e)
    {
        if (phaseManager == null)
        {
            // 沒有 phaseManager 的情況下，為安全起見，不允許把這個 drag 視為合法
            _activeDragMainPhaseId = null;
            return;
        }

        if (phaseManager.CurrentPhase == PhaseType.Main)
        {
            // 只要是在 Main Phase 開始拖曳，就記住當下的 Main Phase ID
            _activeDragMainPhaseId = _mainPhaseId;
        }
        else
        {
            // 在非 Main Phase 開始拖曳：直接標記為無效
            _activeDragMainPhaseId = null;
        }
    }

    private void OnCardDragEnd(CardDragEndEvent e)
    {
        // 拖曳結束時，清掉記錄
        _activeDragMainPhaseId = null;
    }

    // ===== Drop 處理：只有「當前 Main Phase 開始的拖曳」才允許落下 =====

    private void OnSlotDrop(SlotDropEvent e)
    {
        if (board == null)
        {
            Debug.LogWarning("CardDropHandler: BoardController is null.");
            return;
        }

        if (phaseManager == null)
        {
            Debug.LogWarning("CardDropHandler: PhaseManager is null.");
            // 安全起見也不要接受 drop，直接退回
            e.Card?.AnimateToCurrentZone();
            return;
        }

        // 必須在 Main Phase
        if (phaseManager.CurrentPhase != PhaseType.Main)
        {
            Debug.Log("Drop ignored: not in Main phase.");
            e.Card?.AnimateToCurrentZone();
            return;
        }

        // 即使現在是 Main，也必須是「同一個 Main Phase」開始的拖曳
        if (_activeDragMainPhaseId == null || _activeDragMainPhaseId != _mainPhaseId)
        {
            Debug.Log("Drop ignored: drag did not start in current Main phase.");
            e.Card?.AnimateToCurrentZone();
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

        // ✅ Move 成功：仍然由 Board / Presenter 決定 UI 怎麼更新
    }
}
