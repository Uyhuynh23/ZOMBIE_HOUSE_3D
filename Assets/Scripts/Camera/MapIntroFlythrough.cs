using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controls a cinematic 4-shot camera intro flythrough at the start of a map.
/// Uses Catmull-Rom spline interpolation for smooth drone-like motion,
/// displays cinematic letterbox bars with shot subtitles, locks player input during the tour,
/// and smoothly hands control over to CameraFollow once complete or skipped.
/// </summary>
public class MapIntroFlythrough : MonoBehaviour
{
    public static MapIntroFlythrough ActiveInstance { get; private set; }

    [System.Serializable]
    public class WaypointData
    {
        public string shotName = "Shot";
        public Transform point;
        [TextArea(1, 2)] public string title = "";
        [TextArea(1, 2)] public string subtitle = "";
        [Tooltip("Seconds to travel from previous point to this point")]
        [Min(0.5f)] public float duration = 2.5f;
        [Tooltip("If true, moves in a straight line with smooth easing to this point rather than curving through a spline.")]
        public bool straightTransition = false;
    }

    [Header("Waypoints Sequence")]
    [Tooltip("List of camera shot waypoints (minimum 2, ideally 4 for full intro)")]
    public List<WaypointData> waypoints = new List<WaypointData>();

    [Header("Cinematic UI")]
    public CanvasGroup letterboxCanvasGroup;
    public Text shotTitleText;
    public Text shotSubtitleText;
    public Text skipPromptText;

    [Header("Settings")]
    [Tooltip("Automatically play on scene start")]
    public bool playOnStart = true;
    [Tooltip("Allow player to press Space / Esc / Click to skip")]
    public bool allowSkip = true;
    [Tooltip("Seconds to ease into the player camera when transitioning out")]
    public float exitBlendDuration = 1.0f;

    [Header("Pacing & Speed")]
    [Tooltip("Multiplier for transition times. 1.0 = Default calm cinematic (~22s). 1.4 = Slower & gentler (reduces nausea). 0.7 = Fast. Adjust this slider to control camera speed!")]
    [Range(0.4f, 3.0f)]
    public float durationMultiplier = 1.0f;

    [Header("Status (Read Only)")]
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private bool isCompleted = false;
    [SerializeField] private int currentSegmentIndex = 0;

    public bool IsPlaying => isPlaying;
    public bool IsCompleted => isCompleted;

    /// <summary>
    /// Event fired when the intro finishes or is skipped.
    /// Used by ZombieSpawner or TutorialManager to kick off gameplay.
    /// </summary>
    public event Action OnIntroCompleted;
    public static event Action<MapIntroFlythrough> OnAnyIntroFinished;

    private Camera mainCam;
    private CameraFollow cameraFollow;
    private PlayerController playerController;
    private Transform virtualRig;
    private Coroutine flyCoroutine;

    private void Awake()
    {
        ActiveInstance = this;
        isCompleted = false;
        isPlaying = false;

        // Create a dummy virtual transform driven by this script
        GameObject rigObj = new GameObject("_FlythroughVirtualRig");
        rigObj.transform.SetParent(transform);
        virtualRig = rigObj.transform;

        if (letterboxCanvasGroup != null)
        {
            letterboxCanvasGroup.alpha = 0f;
            letterboxCanvasGroup.gameObject.SetActive(false);
        }

        // Immediately snap the main camera to the first waypoint so
        // the very first rendered frame shows the Overview — no flash
        // of the default CameraFollow angle before Play() is called.
        if (playOnStart && waypoints != null && waypoints.Count > 0 && waypoints[0].point != null)
        {
            // Snap camera position
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = waypoints[0].point.position;
                cam.transform.rotation = waypoints[0].point.rotation;
            }

            // Also pre-set CameraFollow override so its LateUpdate
            // doesn't fight the camera during the 1-frame delay
            virtualRig.position = waypoints[0].point.position;
            virtualRig.rotation = waypoints[0].point.rotation;
            cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetOverrideView(virtualRig, 25f);
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(DelayedStartRoutine());
        }
    }

    private IEnumerator DelayedStartRoutine()
    {
        // Wait 1 frame so PlayerSpawner and CameraFollow initialize their targets
        yield return null;
        Play();
    }

    /// <summary>
    /// Starts the cinematic flythrough sequence.
    /// </summary>
    public void Play()
    {
        if (isPlaying || isCompleted) return;
        if (waypoints == null || waypoints.Count < 2)
        {
            Debug.LogWarning("[MapIntroFlythrough] Not enough waypoints to play flythrough (need at least 2).");
            FinishIntro(immediate: true);
            return;
        }

        // Clean null points
        waypoints.RemoveAll(w => w.point == null);
        if (waypoints.Count < 2)
        {
            Debug.LogWarning("[MapIntroFlythrough] Valid waypoints count is less than 2 after cleaning.");
            FinishIntro(immediate: true);
            return;
        }

        mainCam = Camera.main;
        cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
        playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

        // Lock player controller
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Place virtual rig at first waypoint
        virtualRig.position = waypoints[0].point.position;
        virtualRig.rotation = waypoints[0].point.rotation;

        // Set camera override
        if (cameraFollow != null)
        {
            cameraFollow.SetOverrideView(virtualRig, 25f);
        }
        else if (mainCam != null)
        {
            mainCam.transform.position = virtualRig.position;
            mainCam.transform.rotation = virtualRig.rotation;
        }

        isPlaying = true;
        flyCoroutine = StartCoroutine(FlythroughRoutine());
    }

    private void Update()
    {
        if (!isPlaying || !allowSkip) return;

        bool skipPressed = false;
        if (Keyboard.current != null)
        {
            skipPressed |= Keyboard.current.spaceKey.wasPressedThisFrame;
            skipPressed |= Keyboard.current.escapeKey.wasPressedThisFrame;
            skipPressed |= Keyboard.current.enterKey.wasPressedThisFrame;
        }
        if (Mouse.current != null)
        {
            skipPressed |= Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (skipPressed)
        {
            Debug.Log("[MapIntroFlythrough] Intro skipped by user.");
            Skip();
        }
    }

    /// <summary>
    /// Instantly skips remaining shots and blends smoothly into player view.
    /// </summary>
    public void Skip()
    {
        if (!isPlaying || isCompleted) return;
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }
        StartCoroutine(ExitBlendRoutine());
    }

    private IEnumerator FlythroughRoutine()
    {
        // Fade in letterbox
        if (letterboxCanvasGroup != null)
        {
            letterboxCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(letterboxCanvasGroup, 0f, 1f, 0.4f));
        }

        int count = waypoints.Count;

        for (int i = 0; i < count - 1; i++)
        {
            currentSegmentIndex = i;
            WaypointData targetWp = waypoints[i + 1];

            // Update shot UI banners
            UpdateShotBanner(targetWp.title, targetWp.subtitle);

            // Spline control points (clamped to sequence ends)
            Vector3 p0 = waypoints[Mathf.Max(0, i - 1)].point.position;
            Vector3 p1 = waypoints[i].point.position;
            Vector3 p2 = waypoints[i + 1].point.position;
            Vector3 p3 = waypoints[Mathf.Min(count - 1, i + 2)].point.position;

            Quaternion r1 = waypoints[i].point.rotation;
            Quaternion r2 = waypoints[i + 1].point.rotation;

            float duration = Mathf.Max(0.5f, targetWp.duration * durationMultiplier);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease in / ease out
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Position: Straight line or Catmull-Rom spline
                if (targetWp.straightTransition)
                {
                    virtualRig.position = Vector3.Lerp(p1, p2, smoothT);
                }
                else
                {
                    virtualRig.position = GetCatmullRomPosition(smoothT, p0, p1, p2, p3);
                }

                // Slerp rotation
                virtualRig.rotation = Quaternion.Slerp(r1, r2, smoothT);

                // If camera follow isn't overriding, directly drive camera
                if (cameraFollow == null && mainCam != null)
                {
                    mainCam.transform.position = virtualRig.position;
                    mainCam.transform.rotation = virtualRig.rotation;
                }

                yield return null;
            }
        }

        // End of sequence, blend to gameplay
        yield return StartCoroutine(ExitBlendRoutine());
    }

    private IEnumerator ExitBlendRoutine()
    {
        // Fade out letterbox bars
        if (letterboxCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(letterboxCanvasGroup, 1f, 0f, 0.4f));
            letterboxCanvasGroup.gameObject.SetActive(false);
        }

        // Smoothly release override view on CameraFollow
        if (cameraFollow != null)
        {
            // Ease speed out and clear override
            cameraFollow.ClearOverrideView();
        }

        yield return new WaitForSeconds(exitBlendDuration);
        FinishIntro(immediate: false);
    }

    private void FinishIntro(bool immediate)
    {
        isPlaying = false;
        isCompleted = true;

        if (letterboxCanvasGroup != null)
        {
            letterboxCanvasGroup.alpha = 0f;
            letterboxCanvasGroup.gameObject.SetActive(false);
        }

        if (cameraFollow != null)
        {
            cameraFollow.ClearOverrideView();
        }

        // Unlock player controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log("[MapIntroFlythrough] Intro sequence completed. Handing over to gameplay.");

        OnIntroCompleted?.Invoke();
        OnAnyIntroFinished?.Invoke(this);
    }

    private void UpdateShotBanner(string title, string subtitle)
    {
        if (shotTitleText != null) shotTitleText.text = title;
        if (shotSubtitleText != null) shotSubtitleText.text = subtitle;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    /// <summary>
    /// Calculates position on a Catmull-Rom spline given 4 points and t in [0, 1].
    /// </summary>
    public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    /// <summary>
    /// Samples position along the entire waypoint chain for editor gizmo drawing.
    /// </summary>
    public Vector3 SampleSpline(float globalT)
    {
        if (waypoints == null || waypoints.Count < 2) return Vector3.zero;

        int numSegments = waypoints.Count - 1;
        float scaledT = Mathf.Clamp01(globalT) * numSegments;
        int seg = Mathf.Min((int)scaledT, numSegments - 1);
        float localT = scaledT - seg;

        Vector3 p0 = waypoints[Mathf.Max(0, seg - 1)].point != null ? waypoints[Mathf.Max(0, seg - 1)].point.position : Vector3.zero;
        Vector3 p1 = waypoints[seg].point != null ? waypoints[seg].point.position : Vector3.zero;
        Vector3 p2 = waypoints[seg + 1].point != null ? waypoints[seg + 1].point.position : Vector3.zero;
        Vector3 p3 = waypoints[Mathf.Min(waypoints.Count - 1, seg + 2)].point != null ? waypoints[Mathf.Min(waypoints.Count - 1, seg + 2)].point.position : Vector3.zero;

        if (seg + 1 < waypoints.Count && waypoints[seg + 1] != null && waypoints[seg + 1].straightTransition)
        {
            return Vector3.Lerp(p1, p2, Mathf.SmoothStep(0f, 1f, localT));
        }

        return GetCatmullRomPosition(localT, p0, p1, p2, p3);
    }

    /// <summary>
    /// Samples camera rotation along the waypoint chain for editor previewing.
    /// </summary>
    public Quaternion SampleSplineRotation(float globalT)
    {
        if (waypoints == null || waypoints.Count == 0) return Quaternion.identity;
        if (waypoints.Count == 1) return waypoints[0].point != null ? waypoints[0].point.rotation : Quaternion.identity;

        int numSegments = waypoints.Count - 1;
        float scaledT = Mathf.Clamp01(globalT) * numSegments;
        int seg = Mathf.Min((int)scaledT, numSegments - 1);
        float localT = scaledT - seg;

        Quaternion r1 = (seg < waypoints.Count && waypoints[seg].point != null) ? waypoints[seg].point.rotation : Quaternion.identity;
        Quaternion r2 = (seg + 1 < waypoints.Count && waypoints[seg + 1].point != null) ? waypoints[seg + 1].point.rotation : Quaternion.identity;

        return Quaternion.Slerp(r1, r2, Mathf.SmoothStep(0f, 1f, localT));
    }

    /// <summary>
    /// Computes the total intro runtime in seconds based on current waypoint durations and durationMultiplier.
    /// </summary>
    public float GetTotalDuration()
    {
        if (waypoints == null || waypoints.Count < 2) return 0f;
        float sum = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i + 1] != null)
            {
                sum += Mathf.Max(0.5f, waypoints[i + 1].duration * durationMultiplier);
            }
        }
        return sum + exitBlendDuration;
    }
}
