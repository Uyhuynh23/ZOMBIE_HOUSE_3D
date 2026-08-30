using System;
using UnityEngine;

/// <summary>
/// Keeps the integration scene playable when optional third-party map packs are
/// not installed. If the real source object is restored, this fallback hides itself.
/// </summary>
public sealed class MapFallbackVisibility : MonoBehaviour
{
    [SerializeField] private string sourceObjectName = "Baker_house";

    private void Awake()
    {
        Transform[] objects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform item in objects)
        {
            if (item == transform || !item.name.StartsWith(sourceObjectName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (item.GetComponentInChildren<Renderer>(true) != null)
            {
                gameObject.SetActive(false);
                return;
            }
        }
    }
}
