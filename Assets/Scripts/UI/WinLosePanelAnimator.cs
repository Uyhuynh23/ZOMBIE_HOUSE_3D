using System.Collections;
using UnityEngine;

/// <summary>
/// Add this component directly to WinPanel, WinPanel_Final, or LosePanel.
/// It automatically plays the pop-in animation whenever that sub-panel is enabled (SetActive(true)).
/// No coupling to WinLosePanelUI required.
/// </summary>
public class WinLosePanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Total duration of the pop-in animation in seconds.")]
    public float duration = 0.35f;

    [Tooltip("Starting scale multiplier.")]
    public float startScale = 0.45f;

    [Tooltip("Overshoot scale before settling at 1.0.")]
    public float overshootScale = 1.06f;

    private Coroutine _activeCoroutine;

    // ── Auto-trigger when this GameObject is activated ────────────────────────

    private void OnEnable()
    {
        // Reset and play every time the panel is shown
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        transform.localScale = Vector3.one * startScale;
        _activeCoroutine = StartCoroutine(PopInRoutine());
    }

    private void OnDisable()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        // Reset scale so next OnEnable starts clean
        transform.localScale = Vector3.one;
    }

    // ── Also callable manually if needed ─────────────────────────────────────

    public void PlayPopIn()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        transform.localScale = Vector3.one * startScale;
        _activeCoroutine = StartCoroutine(PopInRoutine());
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private IEnumerator PopInRoutine()
    {
        // Try to get or add a CanvasGroup for alpha fade
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // ── Scale: 3-phase ────────────────────────────────────────────────
            float scale;
            if (t < 0.75f)
            {
                // Phase 1: rapid scale up (ease-out feel)
                float t1 = t / 0.75f;
                float curved = 1f - Mathf.Pow(1f - t1, 3f);   // cubic ease-out
                scale = Mathf.LerpUnclamped(startScale, overshootScale, curved);
            }
            else
            {
                // Phase 2: settle back to 1.0
                float t2 = (t - 0.75f) / 0.25f;
                scale = Mathf.Lerp(overshootScale, 1f, t2);
            }

            transform.localScale = Vector3.one * scale;

            // ── Alpha: fade in during first 60% ───────────────────────────────
            cg.alpha = Mathf.Clamp01(t / 0.60f);

            yield return null;
        }

        // Guarantee final state
        transform.localScale = Vector3.one;
        cg.alpha = 1f;
        _activeCoroutine = null;
    }
}
