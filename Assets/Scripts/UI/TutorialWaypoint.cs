using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-world 3D Checkpoint marker that indicates where the player should navigate to.
/// Features a ground ring, gentle vertical bobbing, and an optional 3D floating title.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialWaypoint : MonoBehaviour
{
    public event Action<TutorialWaypoint> OnPlayerReached;

    [Header("Settings")]
    public string checkpointName = "Checkpoint";
    public bool isOneShot = true;

    [Header("Visuals")]
    public Renderer ringRenderer;
    public Transform floatingBeacon;
    public Text labelText;

    private bool isReached = false;
    private bool isActive = true;
    private Vector3 initialBeaconPos;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (floatingBeacon != null)
        {
            initialBeaconPos = floatingBeacon.localPosition;
        }

        if (labelText != null)
        {
            labelText.text = checkpointName;
        }
    }

    private void Update()
    {
        if (!isActive || isReached) return;

        // Bob and rotate the beacon in 3D
        if (floatingBeacon != null)
        {
            float bob = Mathf.Sin(Time.time * 3f) * 0.12f;
            floatingBeacon.localPosition = initialBeaconPos + Vector3.up * bob;
            floatingBeacon.Rotate(Vector3.up, 60f * Time.deltaTime, Space.Self);
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
    }

    public void SetLabel(string text)
    {
        checkpointName = text;
        if (labelText != null)
        {
            labelText.text = text;
        }
    }

    public void SetColor(Color color)
    {
        if (ringRenderer != null && ringRenderer.material != null)
        {
            if (ringRenderer.material.HasProperty("_BaseColor"))
                ringRenderer.material.SetColor("_BaseColor", color);
            else if (ringRenderer.material.HasProperty("_Color"))
                ringRenderer.material.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isReached) return;

        if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player"))
        {
            if (isOneShot) isReached = true;
            Debug.Log($"[TutorialWaypoint] Player reached: {checkpointName}");
            OnPlayerReached?.Invoke(this);
        }
    }
}
