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
    public float plantingDuration = 1.0f;

    // Targeting state for shovel flash
    private PlantBase targetedPlant;
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
            velocityY = -2f; // Stick to ground
        }
        
        velocityY += -9.81f * 2f * Time.deltaTime; // Apply gravity

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

            move = direction * moveSpeed;
            if (animator != null) animator.SetBool("IsMoving", true);
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
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
        {
            PlantableSquare square = hit.collider.GetComponent<PlantableSquare>();
            
            if (square != null)
            {
                currentSquare = square;
                currentIndicator.transform.position = square.transform.position + Vector3.up * 0.06f; 

                if (isShovelMode)
                {
                    if (square.isOccupied)
                    {
                        // Has plant + shovel mode -> red indicator
                        currentIndicator.SetActive(true);
                        UpdateIndicatorColor(Color.red);
                        
                        if (square.currentPlant != null)
                        {
                            UpdateTargetedPlant(square.currentPlant);
                        }
                    }
                    else
                    {
                        // No plant + shovel mode -> hidden indicator
                        currentIndicator.SetActive(false);
                        UpdateTargetedPlant(null);
                    }
                }
                else // Planting mode
                {
                    if (square.isOccupied)
                    {
                        // Has plant -> no indicator at all
                        currentIndicator.SetActive(false);
                        UpdateTargetedPlant(null);
                    }
                    else
                    {
                        // No plant -> yellow indicator
                        currentIndicator.SetActive(true);
                        UpdateIndicatorColor(Color.yellow);
                        UpdateTargetedPlant(null);
                    }
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
                    GameObject planted = Instantiate(prefab, currentSquare.transform.position + Vector3.up * 0.05f, finalRotation);
                    
                    // Register plant with the square using PlantBase
                    PlantBase plantComponent = planted.GetComponent<PlantBase>();
                    if (plantComponent != null)
                    {
                        currentSquare.PlantHere(plantComponent);
                    }
                    else
                    {
                        // Fallback for prefabs without PlantBase (shouldn't happen)
                        currentSquare.SetOccupied(true);
                    }
                }
            }
            
            isPlanting = false;
        }
    }
}
