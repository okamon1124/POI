using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var cardType = serializedObject.FindProperty("cardType");

        // 基本欄位 - 所有卡片都有
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardSprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.PropertyField(cardType);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("manaCost"));

        // Creature 專屬欄位
        if ((CardType)cardType.enumValueIndex == CardType.Creature)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Creature Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("health"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("combatRules"));
        }

        // Play Rules - 所有卡片都有
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerMoveRules"));

        serializedObject.ApplyModifiedProperties();
    }
}