using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the step-by-step tutorial flow in Map_Tutorial:
/// - Step 1: Movement & Basic Combat (WASD, Mouse, LMB/Space attack, Waypoint 1)
/// - Step 2: Sun Gathering & Tree Planting (Sun orbs, Sunflower, Peashooter on soil squares)
/// - Step 3: Zombie Wave & House Defense (3 tutorial zombies down East Road, combat practice)
/// - Step 4: Completion & Transition to Round 1 (Map_Day) or Main Menu.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialPhase
    {
        MovementAndAttack = 1,
        SunAndPlanting = 2,
        ZombieDefense = 3,
        Completed = 4
    }

    [Header("Current Progress")]
    [SerializeField] private TutorialPhase currentPhase = TutorialPhase.MovementAndAttack;

    [Header("Core References")]
    public TutorialTable3D tutorialTable;
    public TutorialWaypoint waypoint1;
    public TutorialWaypoint waypoint2;
    public PlantableSquare[] gardenSquares;
    public ZombieRoute tutorialRoute;
    public HouseHealth houseHealth;

    [Header("Prefabs & Spawning")]
    public GameObject sunPrefab;
    public GameObject zombiePrefab;
    public Transform[] sunSpawnPoints;
    public float zombieSpawnInterval = 4.0f;
    public int tutorialZombieCount = 3;

    [Header("End-Tutorial UI")]
    public GameObject completionPanel;
    public Button nextRoundButton;
    public Button mainMenuButton;

    [Header("Fonts")]
    public Font titleFont;
    public Font bodyFont;

    [Header("Screen HUD Objective Banner")]
    public GameObject screenObjectiveBanner;
    public Text screenObjectiveTitleText;
    public Text screenObjectiveChecklistText;
    public Text screenObjectiveText;

    // Internal task tracking
    private bool hasMoved = false;
    private bool hasAttacked = false;
    private bool hasReachedWaypoint1 = false;

    private int initialSun = 0;
    private bool hasPlantedSunflower = false;
    private bool hasPlantedPeashooter = false;

    private List<ZombieHealth> activeTutorialZombies = new List<ZombieHealth>();
    private bool isSpawningZombies = false;
    private PlayerController player;

    public TutorialPhase CurrentPhase => currentPhase;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (completionPanel != null) completionPanel.SetActive(false);
    }

    private void OnEnable()
    {
        ZombieHealth.OnZombieDied += HandleZombieDied;
    }

    private void OnDisable()
    {
        ZombieHealth.OnZombieDied -= HandleZombieDied;
    }

    private void Start()
    {
        player = Object.FindFirstObjectByType<PlayerController>();

        if (waypoint1 != null)
        {
            waypoint1.OnPlayerReached += OnWaypoint1Reached;
            waypoint1.SetActive(true);
        }

        if (waypoint2 != null)
        {
            waypoint2.OnPlayerReached += OnWaypoint2Reached;
            waypoint2.SetActive(false);
        }

        if (EconomyManager.Instance != null)
        {
            initialSun = EconomyManager.Instance.currentSun;
        }

        if (nextRoundButton != null) nextRoundButton.onClick.AddListener(LoadRound1);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Hide BattleStatus during movement/planting tutorial phases
        Canvas canvas = GetUICanvas();
        if (canvas != null)
        {
            Transform bsTrans = canvas.transform.Find("BattleStatus");
            if (bsTrans != null) bsTrans.gameObject.SetActive(false);
        }

        EnsureScreenObjectiveBanner();
        StartPhase1();
    }

    private Canvas GetUICanvas()
    {
        GameObject uiCanvasObj = GameObject.Find("UI_Canvas");
        if (uiCanvasObj != null)
        {
            Canvas c = uiCanvasObj.GetComponent<Canvas>();
            if (c != null) return c;
        }

        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.gameObject.name.Contains("UI"))
            {
                return c;
            }
        }
        return null;
    }

    private Font GetShlopFont()
    {
        if (titleFont != null) return titleFont;
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (var f in fonts)
        {
            if (f != null && f.name.ToLower().Contains("shlop"))
            {
                titleFont = f;
                return f;
            }
        }
        return GetBodyFont();
    }

    private Font GetBodyFont()
    {
        if (bodyFont != null) return bodyFont;
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (var f in fonts)
        {
            if (f != null && f.name.ToLower().Contains("roboto"))
            {
                bodyFont = f;
                return f;
            }
        }
        bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return bodyFont;
    }

    private void EnsureScreenObjectiveBanner()
    {
        Canvas canvas = GetUICanvas();
        if (canvas == null) return;

        if (screenObjectiveBanner != null) return;

        Transform existing = canvas.transform.Find("TutorialTopBanner");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject bannerObj = new GameObject("TutorialTopBanner");
        bannerObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = bannerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        rt.localScale = new Vector3(1.58f, 1.58f, 1f);
        rt.sizeDelta = new Vector2(680f, 68f);

        Image bg = bannerObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.05f, 0.94f);

        // Gold top accent line
        GameObject topBar = new GameObject("GoldAccent");
        topBar.transform.SetParent(bannerObj.transform, false);
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(0.92f, 0.78f, 0.28f, 0.95f);
        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.sizeDelta = new Vector2(0f, 3f);
        tbRt.anchoredPosition = Vector2.zero;

        // Title (Row 1) - Size 26 in Shlop Font
        GameObject titleObj = new GameObject("BannerTitle");
        titleObj.transform.SetParent(bannerObj.transform, false);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = GetShlopFont();
        titleTxt.fontSize = 26;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1f, 0.88f, 0.35f);
        titleTxt.supportRichText = true;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.48f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(12f, 0f);
        tRt.offsetMax = new Vector2(-12f, -3f);

        // Checklist (Row 2) - Size 20 in Roboto-Bold Font
        GameObject checkObj = new GameObject("BannerChecklist");
        checkObj.transform.SetParent(bannerObj.transform, false);
        Text checkTxt = checkObj.AddComponent<Text>();
        checkTxt.font = GetBodyFont();
        checkTxt.fontSize = 20;
        checkTxt.fontStyle = FontStyle.Bold;
        checkTxt.alignment = TextAnchor.MiddleCenter;
        checkTxt.color = new Color(0.95f, 0.95f, 0.90f);
        checkTxt.supportRichText = true;
        checkTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        checkTxt.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform cRt = checkObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f);
        cRt.anchorMax = new Vector2(1f, 0.50f);
        cRt.offsetMin = new Vector2(12f, 2f);
        cRt.offsetMax = new Vector2(-12f, 0f);

        screenObjectiveBanner = bannerObj;
        screenObjectiveTitleText = titleTxt;
        screenObjectiveChecklistText = checkTxt;
        screenObjectiveText = checkTxt;
    }

    public void SetBannerPositionForCombat(bool inCombat)
    {
        if (screenObjectiveBanner == null) return;
        RectTransform rt = screenObjectiveBanner.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = inCombat ? new Vector2(0f, -96f) : new Vector2(0f, -16f);
        }
    }

    private void SetObjectiveBanner(string title, string checklist)
    {
        if (screenObjectiveTitleText != null) screenObjectiveTitleText.text = title;
        if (screenObjectiveChecklistText != null) screenObjectiveChecklistText.text = checklist;
        if (screenObjectiveText != null && screenObjectiveText != screenObjectiveChecklistText)
            screenObjectiveText.text = $"{title}  •  {checklist}";
    }

    private void Update()
    {
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        switch (currentPhase)
        {
            case TutorialPhase.MovementAndAttack:
                UpdatePhase1();
                break;
            case TutorialPhase.SunAndPlanting:
                UpdatePhase2();
                break;
            case TutorialPhase.ZombieDefense:
                UpdatePhase3();
                break;
        }
    }

    // ──────────────────────────────────────────────────────────
    // PHASE 1: MOVEMENT & BASIC COMBAT
    // ──────────────────────────────────────────────────────────
    private void StartPhase1()
    {
        currentPhase = TutorialPhase.MovementAndAttack;

        string header = "STEP 1: HERO MOVEMENT & COMBAT";
        string body = 
            "Welcome to the training yard!\n" +
            "Learn to maneuver your hero across the courtyard and strike with your sword.\n\n" +
            "* [W][A][S][D] - Walk in 3rd person\n" +
            "* [Mouse] - Orbit camera\n" +
            "* [LMB] / [Space] - Swing melee weapon\n" +
            "* Step into the Checkpoint 1 Ring in the yard!";

        string checklist = GetPhase1Checklist();
        if (tutorialTable != null)
        {
            tutorialTable.SetPhaseDisplay(1, header, body, checklist);
        }

        RefreshPhase1UI();
    }

    private void UpdatePhase1()
    {
        // Detect movement input
        if (!hasMoved && Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                hasMoved = true;
                RefreshPhase1UI();
            }
        }

        // Detect attack input
        if (!hasAttacked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) hasAttacked = true;
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)) hasAttacked = true;
            if (player != null && player.IsAttacking) hasAttacked = true;

            if (hasAttacked) RefreshPhase1UI();
        }

        // Check completion
        if (hasMoved && hasAttacked && hasReachedWaypoint1)
        {
            StartCoroutine(TransitionToPhase2());
        }
    }

    private void OnWaypoint1Reached(TutorialWaypoint wp)
    {
        hasReachedWaypoint1 = true;
        wp.SetActive(false);
        RefreshPhase1UI();
    }

    private void RefreshPhase1UI()
    {
        string checklist = GetPhase1Checklist();
        if (tutorialTable != null && tutorialTable.objectiveChecklistText != null)
        {
            tutorialTable.objectiveChecklistText.text = checklist;
        }

        string m = hasMoved ? "<color=#55FF55>[✓]</color>" : "[ ]";
        string a = hasAttacked ? "<color=#55FF55>[✓]</color>" : "[ ]";
        string r = hasReachedWaypoint1 ? "<color=#55FF55>[✓]</color>" : "[ ]";
        SetObjectiveBanner("CHECKPOINT 1: HERO MANEUVERS & COMBAT",
            $"{m} Move (WASD)   •   {a} Strike (Space/LMB)   •   {r} Reach Beacon");
    }

    private string GetPhase1Checklist()
    {
        string m = hasMoved ? "<color=#55FF55>[x]</color>" : "[ ]";
        string a = hasAttacked ? "<color=#55FF55>[x]</color>" : "[ ]";
        string r = hasReachedWaypoint1 ? "<color=#55FF55>[x]</color>" : "[ ]";
        return $"{m} Move with WASD\n{a} Swing sword (LMB / Space)\n{r} Enter Checkpoint 1 Ring";
    }

    private IEnumerator TransitionToPhase2()
    {
        Debug.Log("[TutorialManager] Step 1 Complete!");
        yield return new WaitForSeconds(1.2f);
        StartPhase2();
    }

    // ──────────────────────────────────────────────────────────
    // PHASE 2: SUN GATHERING & TREE PLANTING
    // ──────────────────────────────────────────────────────────
    private void StartPhase2()
    {
        currentPhase = TutorialPhase.SunAndPlanting;

        // Activate waypoint 2 at the garden line
        if (waypoint2 != null)
        {
            waypoint2.SetActive(true);
        }

        // Spawn tutorial Sun orbs
        SpawnTutorialSunOrbs();

        string header = "STEP 2: SUN ECONOMY & PLANTING";
        string body = 
            "Time to establish your botanical defenses!\n\n" +
            "1. Walk over the floating Sun orbs to collect +25 Sun each.\n" +
            "2. Stand on an empty soil square.\n" +
            "3. Press [3] to select Sunflower (50 Sun) and press [E] to plant.\n" +
            "4. Press [1] to select Peashooter (100 Sun) and press [E] to plant.";

        string checklist = GetPhase2Checklist();
        if (tutorialTable != null)
        {
            tutorialTable.SetPhaseDisplay(2, header, body, checklist);
        }

        RefreshPhase2UI();
    }

    private void SpawnTutorialSunOrbs()
    {
        if (sunPrefab == null) return;

        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-1.5f, 0.4f, 1.5f),
            new Vector3(0.5f, 0.4f, 2.0f),
            new Vector3(-0.8f, 0.4f, -1.2f)
        };

        Vector3 origin = (waypoint2 != null) ? waypoint2.transform.position : transform.position;

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 spawnPos = origin + offsets[i];
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y + 0.35f;
            }
            Instantiate(sunPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void UpdatePhase2()
    {
        // Monitor planted crops in garden
        if (gardenSquares != null)
        {
            foreach (var sq in gardenSquares)
            {
                if (sq != null && sq.isOccupied && sq.currentPlant != null)
                {
                    string plantName = sq.currentPlant.gameObject.name.ToLower();
                    if (plantName.Contains("sunflower")) hasPlantedSunflower = true;
                    if (plantName.Contains("pea")) hasPlantedPeashooter = true;
                }
            }
        }

        RefreshPhase2UI();

        if (hasPlantedSunflower && hasPlantedPeashooter)
        {
            StartCoroutine(TransitionToPhase3());
        }
    }

    private void OnWaypoint2Reached(TutorialWaypoint wp)
    {
        wp.SetActive(false);
    }

    private void RefreshPhase2UI()
    {
        string checklist = GetPhase2Checklist();
        if (tutorialTable != null && tutorialTable.objectiveChecklistText != null)
        {
            tutorialTable.objectiveChecklistText.text = checklist;
        }

        int sun = (EconomyManager.Instance != null) ? EconomyManager.Instance.currentSun : 0;
        string s1 = (sun > initialSun || hasPlantedSunflower || hasPlantedPeashooter) ? "<color=#55FF55>[✓]</color>" : "[ ]";
        string s2 = hasPlantedSunflower ? "<color=#55FF55>[✓]</color>" : "[ ]";
        string s3 = hasPlantedPeashooter ? "<color=#55FF55>[✓]</color>" : "[ ]";
        SetObjectiveBanner("CHECKPOINT 2: SUN HARVEST & PLANT DEFENSES",
            $"{s1} Collect Sun (+25)   •   {s2} Plant Sunflower (3+E)   •   {s3} Plant Peashooter (1+E)");
    }

    private string GetPhase2Checklist()
    {
        int sun = (EconomyManager.Instance != null) ? EconomyManager.Instance.currentSun : 0;
        string s1 = (sun > initialSun || hasPlantedSunflower || hasPlantedPeashooter) ? "<color=#55FF55>[x]</color>" : "[ ]";
        string s2 = hasPlantedSunflower ? "<color=#55FF55>[x]</color>" : "[ ]";
        string s3 = hasPlantedPeashooter ? "<color=#55FF55>[x]</color>" : "[ ]";

        return $"{s1} Collect Sun orbs (+25 each)\n{s2} Select [3] & Plant Sunflower [E]\n{s3} Select [1] & Plant Peashooter [E]";
    }

    private IEnumerator TransitionToPhase3()
    {
        Debug.Log("[TutorialManager] Step 2 Complete!");
        yield return new WaitForSeconds(1.5f);
        StartPhase3();
    }

    // ──────────────────────────────────────────────────────────
    // PHASE 3: ZOMBIE WAVE & HOUSE DEFENSE
    // ──────────────────────────────────────────────────────────
    private void StartPhase3()
    {
        currentPhase = TutorialPhase.ZombieDefense;

        // Activate BattleStatus for combat wave
        Canvas canvas = GetUICanvas();
        if (canvas != null)
        {
            Transform bsTrans = canvas.transform.Find("BattleStatus");
            if (bsTrans != null)
            {
                bsTrans.gameObject.SetActive(true);
                Text waveTxt = bsTrans.Find("WaveText")?.GetComponent<Text>();
                if (waveTxt != null) waveTxt.text = "TUTORIAL WAVE";
            }
        }
        SetBannerPositionForCombat(true);

        string header = "STEP 3: REPEL THE ZOMBIE HORDE";
        string body = 
            "WARNING: Zombies have been spotted marching down the road!\n\n" +
            "* Your Peashooters will automatically fire peas when enemies are in range.\n" +
            "* You can also engage in melee combat: use [LMB] or [Space] to strike!\n" +
            "* Protect the Baker's House at all costs!";

        string checklist = GetPhase3Checklist(0, tutorialZombieCount);
        if (tutorialTable != null)
        {
            tutorialTable.SetPhaseDisplay(3, header, body, checklist);
        }

        SetObjectiveBanner("CHECKPOINT 3: REPEL THE ZOMBIE WAVE",
            $"[ ] Defend the Baker's House   •   [ ] Defeat Zombies (0/{tutorialZombieCount})");

        StartCoroutine(SpawnTutorialZombieWave());
    }

    private IEnumerator SpawnTutorialZombieWave()
    {
        isSpawningZombies = true;
        activeTutorialZombies.Clear();

        for (int i = 0; i < tutorialZombieCount; i++)
        {
            yield return new WaitForSeconds(i == 0 ? 1.5f : zombieSpawnInterval);

            if (zombiePrefab != null && tutorialRoute != null)
            {
                Vector3 spawnPos = (tutorialRoute.SpawnPoint != null) 
                    ? tutorialRoute.SpawnPoint.position 
                    : tutorialRoute.transform.position;

                GameObject zombieObj = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
                zombieObj.name = $"TutorialZombie_{i + 1}";

                ZombiePrototypeMover mover = zombieObj.GetComponent<ZombiePrototypeMover>();
                if (mover != null)
                {
                    Animator anim = zombieObj.GetComponentInChildren<Animator>();
                    mover.ConfigureRoute(anim, tutorialRoute, 1.6f);
                }

                ZombieHealth zHealth = zombieObj.GetComponent<ZombieHealth>();
                if (zHealth != null)
                {
                    activeTutorialZombies.Add(zHealth);
                }
            }
        }

        isSpawningZombies = false;
    }

    private void HandleZombieDied(GameObject deadObj)
    {
        if (deadObj == null) return;
        ZombieHealth z = deadObj.GetComponent<ZombieHealth>();
        if (z != null && activeTutorialZombies.Contains(z))
        {
            activeTutorialZombies.Remove(z);
            int remaining = activeTutorialZombies.Count;
            int eliminated = tutorialZombieCount - remaining;

            if (tutorialTable != null)
            {
                tutorialTable.objectiveChecklistText.text = GetPhase3Checklist(eliminated, tutorialZombieCount);
            }

            string zCheck = (eliminated >= tutorialZombieCount) ? "<color=#55FF55>[✓]</color>" : "[ ]";
            SetObjectiveBanner("CHECKPOINT 3: REPEL THE ZOMBIE WAVE",
                $"[ ] Defend the Baker's House   •   {zCheck} Defeat Zombies ({eliminated}/{tutorialZombieCount})");

            if (!isSpawningZombies && activeTutorialZombies.Count == 0)
            {
                StartCoroutine(CompleteTutorial());
            }
        }
    }

    private void UpdatePhase3()
    {
        // Clean up any destroyed zombies
        activeTutorialZombies.RemoveAll(z => z == null || z.currentHealth <= 0);

        if (!isSpawningZombies && activeTutorialZombies.Count == 0)
        {
            StartCoroutine(CompleteTutorial());
        }
    }

    private string GetPhase3Checklist(int eliminated, int total)
    {
        string check = (eliminated >= total) ? "<color=#55FF55>[x]</color>" : "[ ]";
        return $"{check} Defeat Tutorial Zombies ({eliminated}/{total})\n[x] Defend the Baker's House";
    }

    // ──────────────────────────────────────────────────────────
    // PHASE 4: TUTORIAL GRADUATION
    // ──────────────────────────────────────────────────────────
    private IEnumerator CompleteTutorial()
    {
        if (currentPhase == TutorialPhase.Completed) yield break;
        currentPhase = TutorialPhase.Completed;

        Debug.Log("[TutorialManager] 🎉 TUTORIAL COMPLETE!");

        string header = "TUTORIAL COMPLETE - READY FOR BATTLE!";
        string body = 
            "Congratulations Hero!\n" +
            "You have mastered movement, botanical economy, and zombie defense.\n\n" +
            "You are now fully prepared to defend the Baker's House across all official maps!";

        string checklist = "<color=#55FF55>[x] All Tutorial Checkpoints Completed!</color>";
        if (tutorialTable != null)
        {
            tutorialTable.SetPhaseDisplay(4, header, body, checklist);
        }

        SetObjectiveBanner("<color=#55FF55>TUTORIAL COMPLETED!</color>",
            "All tutorial objectives achieved! Ready for Battle.");

        yield return new WaitForSeconds(1.2f);

        // Hide top objective banner so it doesn't overlap or distract during the victory modal
        if (screenObjectiveBanner != null)
        {
            screenObjectiveBanner.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void LoadRound1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Map_Day");
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
