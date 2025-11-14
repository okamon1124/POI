using UnityEngine;

public class GameLogicTester : MonoBehaviour
{
    public DeckAsset deckAsset;

    private PlayerState player;

    void Start()
    {
        player = new PlayerState(deckAsset);

        Debug.Log($"Deck size after build+shuffle: {player.Deck.Count}");

        var c1 = player.DrawCard();
        var c2 = player.DrawCard();

        Debug.Log($"Drew: {c1?.Data.cardName}, {c2?.Data.cardName}");
        Debug.Log($"Hand size: {player.Hand.Count}");
        Debug.Log($"Deck size now: {player.Deck.Count}");

        // Example: play the first card from hand into Deployment
        if (c1 != null)
        {
            player.PlayFromHandTo(c1, LogicZone.Deployment);
            Debug.Log($"Moved {c1.Data.cardName} to Deployment");
        }

        Debug.Log($"Hand: {player.Hand.Count}, Deployment: {player.Deployment.Count}");
    }
}