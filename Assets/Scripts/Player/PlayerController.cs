using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public struct PlantData
{
    public string name;
    public GameObject prefab;
    public int cost;
    public float cooldownTime;
    [HideInInspector] public float currentCooldown;
    public Sprite portrait;
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private float velocityY = 0f;

    [Header("Map Boundaries")]
    public bool useBounds = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minZ = -100f;
    public float maxZ = 100f;

    [Header("Planting System")]
    public PlantData[] plants;
    [Tooltip("New plants are resized to this fraction of the player's CharacterController height.")]
    [Min(0.1f)] public float plantedHeightRelativeToPlayer = 1.25f;
    private int currentPlantIndex = 0;
    
    [Header("References")]
    public GameObject indicatorPrefab;
    private GameObject currentIndicator;
    private Material indicatorMaterial;

    private CharacterController controller;
    private Animator animator;
    private PlantableSquare currentSquare;
    private PlantableSquare plantingSquare;
    
    private bool isShovelMode = false;

    // Planting delay state
    private bool isPlanting = false;
    private float plantingTimer = 0f;
    public float plantingDuration = 1.0f;

    // Targeting state for shovel flash
    private PlantBase targetedPlant;
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private Dictionary<Renderer, Color> originalBaseColors = new Dictionary<Renderer, Color>();

    [Header("Combat")]
    [Tooltip("Melee damage dealt per attack swing")]
    public int attackDamage = 35;
    [Tooltip("Total duration of the attack animation lock / cooldown in seconds")]
    public float attackDuration = 0.85f;
    [Tooltip("Delay in seconds from the start of the attack animation until damage lands (impact moment)")]
    public float attackDamageDelay = 0.38f;
    [Tooltip("Movement speed multiplier while performing an attack swing (0 = rooted, 0.15 = slow step)")]
    [Range(0f, 1f)]
    public float attackMovementMultiplier = 0.15f;
    [Tooltip("Range ahead of the player to check for enemies")]
    public float attackRange = 1.6f;
    [Tooltip("Radius of the melee attack hit sphere")]
    public float attackRadius = 1.0f;
    [Tooltip("Layer mask for enemy colliders")]
    public LayerMask enemyLayerMask = ~0;

    private float attackTimer = 0f;
    private bool isAttacking = false;
    private Coroutine attackCoroutine = null;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public int CurrentPlantIndex => currentPlantIndex;
    public bool IsShovelMode => isShovelMode;
    public bool IsAttacking => isAttacking;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        
        SetupIndicator();
        
        if (plants != null && plants.Length > 0)
        {
            SelectPlant(0);
        }

        // Snap to terrain on spawn so the character doesn't float
        SnapToGround();
    }

    /// <summary>Teleport the character down onto the terrain/collider beneath it at spawn.</summary>
    void SnapToGround()
    {
        // Try terrain first (cheap)
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 pos = transform.position;
            float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
            if (Mathf.Abs(pos.y - terrainY) < 10f) // only snap if reasonably close
            {
                controller.enabled = false;
                transform.position = new Vector3(pos.x, terrainY, pos.z);
                controller.enabled = true;
                return;
            }
        }

        // Fallback: raycast downward
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 20f))
        {
            controller.enabled = false;
            transform.position = hit.point;
            controller.enabled = true;
        }
    }

    void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
    }

    void SetupIndicator()
    {
        if (indicatorPrefab != null)
        {
            currentIndicator = Instantiate(indicatorPrefab);
            currentIndicator.SetActive(false);
            
            Renderer r = currentIndicator.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                indicatorMaterial = new Material(r.sharedMaterial);
                r.sharedMaterial = indicatorMaterial;
            }
        }
        else
        {
            currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentIndicator.transform.localScale = new Vector3(1f, 0.1f, 1f);
            currentIndicator.GetComponent<Collider>().enabled = false;
            currentIndicator.SetActive(false);
            
            Renderer r = currentIndicator.GetComponent<Renderer>();
            
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            indicatorMaterial = new Material(shader);

            if (indicatorMaterial.HasProperty("_Surface"))
            {
                // URP Transparent
                indicatorMaterial.SetFloat("_Surface", 1);
                indicatorMaterial.SetInt("_Blend", 0);
                indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                indicatorMaterial.SetInt("_ZWrite", 0);
                indicatorMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                // Standard transparent
                indicatorMaterial.SetFloat("_Mode", 3);
                indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                indicatorMaterial.SetInt("_ZWrite", 0);
                indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
                indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
                indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                indicatorMaterial.renderQueue = 3000;
            }
            
            r.sharedMaterial = indicatorMaterial;
        }
    }

    void Update()
    {
        if (plants != null)
        {
            for (int i = 0; i < plants.Length; i++)
            {
                if (plants[i].currentCooldown > 0)
                {
                    plants[i].currentCooldown -= Time.deltaTime;
                }
            }
        }

        if (isPlanting)
        {
            HandlePlantingSequence();
            return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        HandleMovement();
        HandleAttackInput();

        if (!isAttacking)
        {
            HandleSelectionInput();
            CheckCurrentSquare();
            HandleActionInput();
            UpdateTargetFlash();
        }

        ApplyBoundaries();
    }

    void ApplyBoundaries()
    {
        if (useBounds)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            
            // Only force position if it actually exceeded the bounds
            if (pos != transform.position)
            {
                // Temporarily disable the CharacterController to teleport it safely
                controller.enabled = false;
                transform.position = pos;
                controller.enabled = true;
            }
        }
    }

    void HandleSelectionInput()
    {
        if (Keyboard.current == null || plants == null || plants.Length == 0) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame && plants.Length > 0) SelectPlant(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && plants.Length > 1) SelectPlant(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && plants.Length > 2) SelectPlant(2);

        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)
            SetShovelMode(true);
    }

    public void SelectPlant(int index)
    {
        isShovelMode = false;
        currentPlantIndex = index;
        Debug.Log("Equipped: " + plants[index].name);
        UpdateIndicatorColor(Color.yellow);
        UpdateTargetedPlant(null);
    }

    public void SetShovelMode(bool on)
    {
        isShovelMode = on;
        if (on)
        {
            Debug.Log("Equipped: Shovel");
            UpdateIndicatorColor(Color.red);
        }
        else
        {
            UpdateTargetedPlant(null);
        }
    }

    void UpdateIndicatorColor(Color color)
    {
        if (indicatorMaterial != null)
        {
            color.a = 0.5f;
            if (indicatorMaterial.HasProperty("_BaseColor"))
                indicatorMaterial.SetColor("_BaseColor", color);
            else if (indicatorMaterial.HasProperty("_Color"))
                indicatorMaterial.color = color;
        }
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (controller.isGrounded && velocityY < 0f)
        {
            velocityY = -4f; // Stronger ground stick force
        }
        
        velocityY += -9.81f * 4f * Time.deltaTime; // Stronger gravity so feet stay grounded

        Vector3 move = Vector3.zero;

        if (direction.magnitude >= 0.1f)
        {
            if (Camera.main != null)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                direction = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
            else
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }

            float speed = isAttacking ? moveSpeed * attackMovementMultiplier : moveSpeed;
            move = direction * speed;
            if (animator != null) animator.SetBool("IsMoving", !isAttacking);
        }
        else
        {
            if (animator != null) animator.SetBool("IsMoving", false);
        }

        move.y = velocityY;
        controller.Move(move * Time.deltaTime);
    }

    void CheckCurrentSquare()
    {
        PlantableSquare square = FindPlantableSquareBelow();

        if (square != null)
        {
            currentSquare = square;
            currentIndicator.transform.position = square.transform.position + Vector3.up * 0.06f;

            if (isShovelMode)
            {
                if (square.isOccupied)
                {
                    currentIndicator.SetActive(true);
                    UpdateIndicatorColor(Color.red);
                    UpdateTargetedPlant(square.currentPlant);
                }
                else
                {
                    currentIndicator.SetActive(false);
                    UpdateTargetedPlant(null);
                }
            }
            else
            {
                if (square.isOccupied)
                {
                    currentIndicator.SetActive(false);
                }
                else
                {
                    currentIndicator.SetActive(true);
                    UpdateIndicatorColor(Color.yellow);
                }
                UpdateTargetedPlant(null);
            }
        }
        else
        {
            currentSquare = null;
            currentIndicator.SetActive(false);
            UpdateTargetedPlant(null);
        }
    }

    /// <summary>
    /// Finds the plantable square at the player's feet. This explicitly
    /// includes trigger colliders: PlantableSquare is intentionally a trigger
    /// so it never blocks player movement.
    /// </summary>
    PlantableSquare FindPlantableSquareBelow()
    {
        // A downward ray starting inside a trigger may not return that trigger.
        // Check a small area at the player's feet first, which works while the
        // player is standing directly on a plantable square.
        float feetY = controller != null ? controller.bounds.min.y + 0.2f : transform.position.y + 0.2f;
        Vector3 feet = new Vector3(transform.position.x, feetY, transform.position.z);
        Collider[] nearby = Physics.OverlapSphere(
            feet, 0.35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        PlantableSquare closest = null;
        float closestDistance = float.MaxValue;
        foreach (Collider collider in nearby)
        {
            PlantableSquare square = collider.GetComponentInParent<PlantableSquare>();
            if (square == null) continue;

            Vector3 offset = square.transform.position - feet;
            offset.y = 0f;
            float distance = offset.sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = square;
                closestDistance = distance;
            }
        }

        if (closest != null) return closest;

        // Fallback for uneven geometry: cast from above the whole character,
        // never from inside the square trigger.
        float castHeight = controller != null ? controller.height + 1f : 3f;
        Vector3 rayOrigin = new Vector3(transform.position.x, feetY + castHeight, transform.position.z);
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin, Vector3.down, castHeight + 2f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            PlantableSquare square = hit.collider.GetComponentInParent<PlantableSquare>();
            if (square != null && hit.distance < closestDistance)
            {
                closest = square;
                closestDistance = hit.distance;
            }
        }
        return closest;
    }
    
    void UpdateTargetedPlant(PlantBase newTarget)
    {
        if (targetedPlant == newTarget) return;

        // Restore old target colors
        if (targetedPlant != null)
        {
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null && kvp.Key.material.HasProperty("_Color")) 
                    kvp.Key.material.color = kvp.Value;
            }
            foreach (var kvp in originalBaseColors)
            {
                if (kvp.Key != null && kvp.Key.material.HasProperty("_BaseColor")) 
                    kvp.Key.material.SetColor("_BaseColor", kvp.Value);
            }
        }

        targetedPlant = newTarget;
        originalColors.Clear();
        originalBaseColors.Clear();

        // Save new target colors
        if (targetedPlant != null)
        {
            Renderer[] renderers = targetedPlant.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    originalColors[r] = r.material.color;
                }
                if (r.material.HasProperty("_BaseColor"))
                {
                    originalBaseColors[r] = r.material.GetColor("_BaseColor");
                }
            }
        }
    }
    
    void UpdateTargetFlash()
    {
        if (isShovelMode && targetedPlant != null)
        {
            float t = Mathf.Sin(Time.time * 8f) * 0.4f + 0.6f;
            
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.material.color = Color.Lerp(kvp.Value, Color.red, t);
                }
            }
            foreach (var kvp in originalBaseColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.material.SetColor("_BaseColor", Color.Lerp(kvp.Value, Color.red, t));
                }
            }
        }
    }

    void HandleActionInput()
    {
        if (currentSquare == null) return;
        
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (isShovelMode)
            {
                if (currentSquare.isOccupied && currentSquare.currentPlant != null)
                {
                    UpdateTargetedPlant(null); // Clear flash before destroy
                    currentSquare.currentPlant.OnShoveled(); // PlantBase handles cleanup
                    Debug.Log("Shoveled plant!");
                }
            }
            else // Planting Mode
            {
                if (currentSquare.isOccupied) return;
                
                if (plants == null || currentPlantIndex < 0 || currentPlantIndex >= plants.Length) return;
                PlantData activePlant = plants[currentPlantIndex];
                
                if (activePlant.prefab == null) return;

                if (activePlant.currentCooldown > 0f)
                {
                    Debug.Log(activePlant.name + " is on cooldown!");
                    return;
                }

                if (EconomyManager.Instance != null && EconomyManager.Instance.currentSun < activePlant.cost)
                {
                    Debug.Log("Not enough sun for " + activePlant.name + "!");
                    return;
                }

                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.SpendSun(activePlant.cost);
                }

                plants[currentPlantIndex].currentCooldown = activePlant.cooldownTime;
                
                isPlanting = true;
                plantingTimer = plantingDuration;
                plantingSquare = currentSquare;
                
                currentIndicator.SetActive(false); 
            }
        }
    }

    void HandlePlantingSequence()
    {
        plantingTimer -= Time.deltaTime;
        
        if (plantingTimer <= 0f)
        {
            if (plantingSquare != null && !plantingSquare.isOccupied &&
                plants != null && currentPlantIndex >= 0 && currentPlantIndex < plants.Length)
            {
                GameObject prefab = plants[currentPlantIndex].prefab;
                if (prefab != null)
                {
                    Quaternion finalRotation = plantingSquare.transform.rotation * prefab.transform.rotation;
                    GameObject planted = Instantiate(prefab, plantingSquare.transform.position + Vector3.up * 0.05f, finalRotation);
                    ScalePlantToPlayerHeight(planted);
                    
                    // Register plant with the square using PlantBase
                    PlantBase plantComponent = planted.GetComponent<PlantBase>();
                    if (plantComponent != null)
                    {
                        plantingSquare.PlantHere(plantComponent);

                        // Face outward along the cardinal lane, toward enemies
                        // approaching from beyond the house's plant grid. The
                        // combat component reads each model's SpawnPoint to
                        // resolve its real muzzle direction.
                        PeashooterCombat combat = planted.GetComponent<PeashooterCombat>();
                        if (combat != null)
                        {
                            Vector3 squarePosition = plantingSquare.transform.position;
                            Vector3 outwardLaneDirection = GetOutwardLaneDirection(squarePosition);
                            combat.SetAimDirection(outwardLaneDirection);
                        }
                    }
                    else
                    {
                        // Fallback for prefabs without PlantBase (shouldn't happen)
                        plantingSquare.SetOccupied(true);
                    }
                }
            }
            
            isPlanting = false;
            plantingSquare = null;
        }
    }

    /// <summary>Maps every cardinal planting zone to its enemy-entry direction.</summary>
    private static Vector3 GetOutwardLaneDirection(Vector3 squarePosition)
    {
        // North: +Z, South: -Z, East: +X, West: -X.
        // This is intentionally based on the zone axis, not the square's
        // individual rotation, because square rotations are decorative.
        if (Mathf.Abs(squarePosition.z) >= Mathf.Abs(squarePosition.x))
            return squarePosition.z >= 0f ? Vector3.forward : Vector3.back;

        return squarePosition.x >= 0f ? Vector3.right : Vector3.left;
    }

    private void ScalePlantToPlayerHeight(GameObject planted)
    {
        if (planted == null || controller == null) return;

        Renderer[] renderers = planted.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y < 0.01f) return;

        float targetHeight = controller.height * plantedHeightRelativeToPlayer;
        float scaleFactor = Mathf.Clamp(targetHeight / bounds.size.y, 0.5f, 4f);
        planted.transform.localScale *= scaleFactor;
    }

    void HandleAttackInput()
    {
        if (isAttacking || attackTimer > 0f || isPlanting)
            return;

        bool attackPressed = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            attackPressed = true;
        }
        else if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame))
        {
            attackPressed = true;
        }

        if (attackPressed)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackSequence());
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        isAttacking = true;
        attackTimer = attackDuration;

        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        // Wait until the downward chop connects in the animation before dealing damage
        yield return new WaitForSeconds(attackDamageDelay);

        // Deal damage at the exact moment of impact
        ApplyMeleeDamage();

        // Wait for the remaining recovery duration of the attack animation
        float recoveryTime = Mathf.Max(0f, attackDuration - attackDamageDelay);
        if (recoveryTime > 0f)
        {
            yield return new WaitForSeconds(recoveryTime);
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    void ApplyMeleeDamage()
    {
        Vector3 hitOrigin = transform.position + transform.forward * attackRange * 0.5f + Vector3.up * 0.7f;
        Collider[] hits = Physics.OverlapSphere(hitOrigin, attackRadius, enemyLayerMask);

        HashSet<ZombieHealth> damagedEnemies = new HashSet<ZombieHealth>();

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Zombie"))
            {
                ZombieHealth enemyHealth = hit.GetComponentInParent<ZombieHealth>();
                if (enemyHealth != null && !damagedEnemies.Contains(enemyHealth))
                {
                    damagedEnemies.Add(enemyHealth);
                    enemyHealth.TakeDamage(attackDamage);
                    Debug.Log($"[PlayerCombat] Hit {enemyHealth.gameObject.name} for {attackDamage} damage! HP left: {enemyHealth.currentHealth}");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 hitOrigin = transform.position + transform.forward * attackRange * 0.5f + Vector3.up * 0.7f;
        Gizmos.DrawWireSphere(hitOrigin, attackRadius);
    }
}
