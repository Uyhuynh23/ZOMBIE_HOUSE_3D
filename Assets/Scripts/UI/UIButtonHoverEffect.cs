using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.04f;
    public float pressScale = 0.96f;
    public float transitionSpeed = 15f;

    [Header("Audio")]
    public bool playHoverSound = true;

    [Header("Target")]
    public Transform targetTransform;

    private Transform Target => targetTransform != null ? targetTransform : transform;
    private Vector3 baseScale = Vector3.one;
    private Vector3 currentTargetScale = Vector3.one;
    private bool isHovered = false;
    private bool isPressed = false;
    private Selectable selectable;

    private void Awake()
    {
        baseScale = Target.localScale;
        currentTargetScale = baseScale;
        selectable = GetComponent<Selectable>();
    }

    private void OnEnable()
    {
        Target.localScale = baseScale;
        currentTargetScale = baseScale;
        isHovered = false;
        isPressed = false;
    }

    private void Update()
    {
        Target.localScale = Vector3.Lerp(Target.localScale, currentTargetScale, Time.unscaledDeltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isHovered = true;
        if (!isPressed) currentTargetScale = baseScale * hoverScale;
        if (playHoverSound) AudioManager.PlaySfx(AudioCue.UiClick);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        currentTargetScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isPressed = true;
        currentTargetScale = baseScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isPressed = false;
        currentTargetScale = isHovered ? (baseScale * hoverScale) : baseScale;
    }
}
