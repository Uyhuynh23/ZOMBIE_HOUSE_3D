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

    [Header("Planting System")]
    public PlantData[] plants;
    private int currentPlantIndex = 0;
    
    [Header("References")]
    public GameObject indicatorPrefab;
    private GameObject currentIndicator;
    private Material indicatorMaterial;

    private CharacterController controller;
    private Animator animator;
    private PlantableSquare currentSquare;
    
    private bool isShovelMode = false;

    // Planting delay state
    private bool isPlanting = false;
    private float plantingTimer = 0f;
    public float plantingDuration = 1.0f; // Seconds the planting animation takes

    // Targeting state for shovel flash
    private GameObject targetedPlant;
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private Dictionary<Renderer, Color> originalBaseColors = new Dictionary<Renderer, Color>();

    public int CurrentPlantIndex => currentPlantIndex;
    public bool IsShovelMode => isShovelMode;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        
        SetupIndicator();
        
        if (plants != null && plants.Length > 0)
        {
            SelectPlant(0);
        }
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
            indicatorMaterial = new Material(Shader.Find("Standard"));
            
            // Setup transparent material
            indicatorMaterial.SetFloat("_Mode", 3);
            indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            indicatorMaterial.SetInt("_ZWrite", 0);
            indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
            indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
            indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            indicatorMaterial.renderQueue = 3000;
            
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

        HandleMovement();
        HandleSelectionInput();
        CheckCurrentSquare();
        HandleActionInput();
        UpdateTargetFlash();
    }

    void HandleSelectionInput()
    {
        if (Keyboard.current == null || plants == null || plants.Length == 0) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame && plants.Length > 0) SelectPlant(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && plants.Length > 1) SelectPlant(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && plants.Length > 2) SelectPlant(2);

        // Shovel Mode on 4 or R
        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)
            SetShovelMode(true);
    }

    public void SelectPlant(int index)
    {
        isShovelMode = false;
        currentPlantIndex = index;
        Debug.Log("Equipped: " + plants[index].name);
        UpdateIndicatorColor(Color.yellow);
        UpdateTargetedPlant(null); // Clear shovel target
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

            controller.Move(direction * moveSpeed * Time.deltaTime);
            if (animator != null) animator.SetBool("IsMoving", true);
        }
        else
        {
            if (animator != null) animator.SetBool("IsMoving", false);
        }

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    void CheckCurrentSquare()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
        {
            PlantableSquare square = hit.collider.GetComponent<PlantableSquare>();
            
            if (square != null)
            {
                currentSquare = square;
                currentIndicator.SetActive(true);
                currentIndicator.transform.position = square.transform.position + Vector3.up * 0.06f; 

                if (isShovelMode)
                {
                    UpdateIndicatorColor(square.isOccupied ? Color.red : Color.gray);
                    
                    if (square.isOccupied)
                    {
                        GameObject foundPlant = FindPlantOnSquare(square);
                        UpdateTargetedPlant(foundPlant);
                    }
                    else
                    {
                        UpdateTargetedPlant(null);
                    }
                }
                else
                {
                    UpdateIndicatorColor(square.isOccupied ? Color.red : Color.yellow);
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
        else
        {
            currentSquare = null;
            currentIndicator.SetActive(false);
            UpdateTargetedPlant(null);
        }
    }
    
    GameObject FindPlantOnSquare(PlantableSquare square)
    {
        // Half extents 0.3f to keep it strictly inside the 1x1 tile
        Collider[] hits = Physics.OverlapBox(square.transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue; // Ignore aggro spheres!
            if (h.gameObject == gameObject) continue; // Ignore player
            
            // Check if it's a plant component
            if (h.GetComponentInParent<PeashooterCombat>() != null || h.GetComponentInParent<SunflowerLogic>() != null)
            {
                return h.transform.root.gameObject;
            }
        }
        return null;
    }
    
    void UpdateTargetedPlant(GameObject newTarget)
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
            float t = Mathf.Sin(Time.time * 8f) * 0.4f + 0.6f; // Pulse between 0.2 and 1.0 mix
            
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
                if (currentSquare.isOccupied)
                {
                    GameObject plantToDestroy = FindPlantOnSquare(currentSquare);
                    if (plantToDestroy != null)
                    {
                        UpdateTargetedPlant(null); // Clear flash before destroy
                        Destroy(plantToDestroy);
                        currentSquare.SetOccupied(false);
                        Debug.Log("Shoveled plant!");
                    }
                }
            }
            else // Planting Mode
            {
                if (currentSquare.isOccupied) return;
                
                if (plants == null || plants.Length == 0) return;
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

                // Proceed with planting
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.SpendSun(activePlant.cost);
                }

                plants[currentPlantIndex].currentCooldown = activePlant.cooldownTime;
                
                isPlanting = true;
                plantingTimer = plantingDuration;
                
                currentIndicator.SetActive(false); 
            }
        }
    }

    void HandlePlantingSequence()
    {
        plantingTimer -= Time.deltaTime;
        
        if (plantingTimer <= 0f)
        {
            if (currentSquare != null && plants != null && plants.Length > currentPlantIndex)
            {
                GameObject prefab = plants[currentPlantIndex].prefab;
                if (prefab != null)
                {
                    Quaternion finalRotation = currentSquare.transform.rotation * prefab.transform.rotation;
                    Instantiate(prefab, currentSquare.transform.position + Vector3.up * 0.05f, finalRotation);
                    currentSquare.SetOccupied(true);
                }
            }
            
            isPlanting = false;
        }
    }
}
