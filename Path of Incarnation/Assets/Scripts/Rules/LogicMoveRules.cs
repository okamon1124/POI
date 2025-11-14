using System;
using UnityEngine;

//public static class LogicMoveRules
//{
//    public static bool CanMove(
//        CardData cardData,
//        ZoneType fromZone,
//        ZoneType toZone,
//        MoveType moveType,
//        out string reason)
//    {
//        if (!cardData)
//        {
//            reason = "Card data missing.";
//            return false;
//        }
//
//        if (fromZone == toZone)
//        {
//            reason = "Same zone.";
//            return false;
//        }
//
//        // System rules at pure-logic level (optional for now)
//        if (moveType == MoveType.System)
//        {
//            // You can put logic-only constraints here if you want,
//            // or just say it's always allowed and let UI handle lane/nextZone.
//            reason = null;
//            return true;
//        }
//
//        // Pick which rule set to use
//        var rules = moveType switch
//        {
//            MoveType.Player => cardData.playerMoveRules,
//            MoveType.Effect => cardData.playerMoveRules, // later maybe effectMoveRules
//            _ => cardData.playerMoveRules
//        } ?? Array.Empty<PlayRule>();
//
//        // IMPORTANT: PlayRuleEval should also be logic-only
//        return PlayRuleEval.Evaluate(
//            rules,
//            cardData,
//            fromZone,
//            toZone,
//            moveType,
//            out reason
//        );
//    }
//}