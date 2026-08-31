using UnityEngine;

public sealed class PortablePlayerVisual : MonoBehaviour
{
    [SerializeField] private GameObject preferredVisual;
    [SerializeField] private GameObject fallbackVisual;

    public void Configure(GameObject preferred, GameObject fallback)
    {
        preferredVisual = preferred;
        fallbackVisual = fallback;
        Refresh();
    }

    private void Awake()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool hasPreferredVisual = preferredVisual != null
            && preferredVisual.GetComponentsInChildren<Renderer>(true).Length > 0;

        if (preferredVisual != null)
            preferredVisual.SetActive(hasPreferredVisual);
        if (fallbackVisual != null)
            fallbackVisual.SetActive(!hasPreferredVisual);
    }
}
