using System.Collections;
using UnityEngine;

public class CombatPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry uiRegistry;

    public void Initialize(Board board, UiRegistry registry)
    {
        if (this.board != null)
            this.board.OnCombatBegin -= HandleCombatBegin;

        this.board = board;
        this.uiRegistry = registry;

        if (this.board != null)
            this.board.OnCombatBegin += HandleCombatBegin;
    }

    private void OnDestroy()
    {
        if (board != null)
            board.OnCombatBegin -= HandleCombatBegin;
    }

    private void HandleCombatBegin(CombatResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[CombatPresenter] HandleCombatBegin called with null result.");
            return;
        }

        Debug.Log($"[CombatPresenter] OnCombatBegin received. " +
                  $"DamageToPlayer={result.DamageToPlayer}, DamageToEnemy={result.DamageToEnemy}, " +
                  $"CardDamagesCount={result.CardDamages.Count}");

        StartCoroutine(PlayMainCombatSequence(result));
    }

    private IEnumerator PlayMainCombatSequence(CombatResult result)
    {
        if (board == null)
        {
            Debug.LogError("[CombatPresenter] board is null.");
            yield break;
        }

        if (board.MainCombatLane == null)
        {
            Debug.LogWarning("[CombatPresenter] MainCombatLane is null.");
            yield break;
        }

        var lane = board.MainCombatLane;

        var playerCard = lane.PlayerCombatSlot.InSlotCardInstance;
        var enemyCard = lane.EnemyCombatSlot.InSlotCardInstance;

        if (playerCard == null && enemyCard == null)
        {
            Debug.Log("[CombatPresenter] No cards in combat slots. Abort animation.");
            yield break;
        }

        UiCard playerUiCard = playerCard != null ? uiRegistry.GetUiCard(playerCard) : null;
        UiCard enemyUiCard = enemyCard != null ? uiRegistry.GetUiCard(enemyCard) : null;

        // 雙方同步頭槌
        if (playerUiCard != null)
        {
            Debug.Log("[CombatPresenter] Playing player attack impact.");
            playerUiCard.PlayAttackImpact();
        }

        if (enemyUiCard != null)
        {
            Debug.Log("[CombatPresenter] Playing enemy attack impact.");
            enemyUiCard.PlayAttackImpact();
        }

        // 如果你想確認動畫大約 0.3s，這裡暫時等一下（純 debug 用）
        yield return new WaitForSeconds(0.4f);

        Debug.Log("[CombatPresenter] PlayMainCombatSequence end.");
    }

}