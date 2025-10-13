using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class HandManagerUIWorldSpace : MonoBehaviour
{
    [Header("Hand")]
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private GameObject cardPrefab;

    [Header("Layout")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float tweenDuration = 0.25f;

    [Header("UI (World Space)")]
    [SerializeField] private Canvas canvas;                 // Must be World Space
    [SerializeField] private RectTransform spawnPoint;      // RectTransform spawn anchor under the same canvas

    private readonly List<RectTransform> handCards = new List<RectTransform>();
    private RectTransform canvasRect;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("HandManagerUIWorldSpace: Assign a Canvas set to World Space.");
            enabled = false;
            return;
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        if (splineContainer == null)
        {
            Debug.LogError("HandManagerUIWorldSpace: Assign a SplineContainer.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            DrawCard();
    }

    public void DrawCard()
    {
        if (handCards.Count >= maxHandSize || cardPrefab == null) return;

        // Instantiate without keeping world values → preserves prefab's local values (including scale)
        GameObject g = Instantiate(cardPrefab, canvas.transform, false);

        RectTransform rect = g.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogError("Card prefab must have a RectTransform.");
            Destroy(g);
            return;
        }

        // ✅ Fix: explicitly copy prefab’s local scale
        RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
        if (prefabRect != null)
            rect.localScale = prefabRect.localScale;

        // Optional: spawn transform
        if (spawnPoint != null)
        {
            rect.anchoredPosition3D = spawnPoint.anchoredPosition3D;
            rect.localRotation = spawnPoint.localRotation;
        }
        else
        {
            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
        }

        handCards.Add(rect);
        UpdateCardPositions();
    }


    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstP = 0.5f - (handCards.Count - 1) * cardSpacing / 2f;

        Spline spline = splineContainer.Spline;

        for (int i = 0; i < handCards.Count; i++)
        {
            float p = Mathf.Clamp01(firstP + i * cardSpacing);
            PlaceRectOnSpline(handCards[i], spline, p);
        }
    }

    /// <summary>
    /// Place UI card on a World Space Canvas:
    /// - Position: canvas local (anchoredPosition3D)
    /// - Rotation: only around Z, using the spline tangent projected into the canvas plane
    ///   so the card always faces the canvas/camera.
    /// </summary>
    private void PlaceRectOnSpline(RectTransform rect, Spline spline, float p)
    {
        // World pose from spline (cast float3 → Vector3)
        Vector3 worldPos = (Vector3)spline.EvaluatePosition(p);
        Vector3 worldTangent = ((Vector3)spline.EvaluateTangent(p)).normalized;

        // Convert to canvas local space
        Vector3 localPosOnCanvas = canvasRect.InverseTransformPoint(worldPos);
        Vector3 localTangent = canvasRect.InverseTransformDirection(worldTangent).normalized;

        // Project the tangent onto the canvas plane (XY in canvas local space)
        Vector2 dir2D = new Vector2(localTangent.x, localTangent.y);
        if (dir2D.sqrMagnitude < 1e-6f)
            dir2D = Vector2.right;

        float angleDeg = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;

        // Tween: position + Z-only rotation so the card stays in the canvas plane
        rect.DOAnchorPos3D(localPosOnCanvas, tweenDuration);
        rect.DOLocalRotate(new Vector3(0f, 0f, angleDeg), tweenDuration);
    }
}
