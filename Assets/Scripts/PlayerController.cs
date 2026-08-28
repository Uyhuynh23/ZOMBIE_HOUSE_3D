using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class PlantData
{
    public string name;
    public GameObject prefab;
    public int cost;
    public float cooldownTime;
    [HideInInspector] public float currentCooldown = 0f;
}

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Planting Roster")]
    public PlantData[] plants;
    private int currentPlantIndex = 0;
    private bool isShovelMode = false;
    public float plantingDuration = 1f;

    [Header("Visual Indicator")]
    public GameObject indicatorPrefab; 
    private GameObject currentIndicator;
    private Material indicatorMaterial;

    private CharacterController controller;
    private Animator animator;

    private bool isPlanting = false;
    private float plantingTimer = 0f;
    private PlantableSquare currentSquare;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        
        SetupIndicator();
    }

    void SetupIndicator()
    {
        if (indicatorPrefab == null)
        {
            currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            currentIndicator.name = "PlantIndicator";
            Destroy(currentIndicator.GetComponent<Collider>()); 
            currentIndicator.transform.rotation = Quaternion.Euler(90, 0, 0);
            currentIndicator.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            
            indicatorMaterial = new Material(shader);
            if (shader.name.Contains("Universal"))
            {
                indicatorMaterial.SetColor("_BaseColor", new Color(1f, 1f, 0f, 0.5f));
                indicatorMaterial.SetFloat("_Surface", 1); 
                indicatorMaterial.renderQueue = 3000;
            }
            else
            {
                indicatorMaterial.color = new Color(1f, 1f, 0f, 0.5f);
                indicatorMaterial.SetFloat("_Mode", 3); 
                indicatorMaterial.SetInt("_ZWrite", 0);
                indicatorMaterial.renderQueue = 3000;
            }
            currentIndicator.GetComponent<MeshRenderer>().material = indicatorMaterial;
        }
        else
        {
            currentIndicator = Instantiate(indicatorPrefab);
            indicatorMaterial = currentIndicator.GetComponentInChildren<Renderer>().material;
        }
        
        currentIndicator.SetActive(false);
    }

    void Update()
    {
        // Update Cooldowns
        if (plants != null)
        {
            foreach (var plant in plants)
            {
                if (plant.currentCooldown > 0)
                {
                    plant.currentCooldown -= Time.deltaTime;
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
    }

    void HandleSelectionInput()
    {
        if (Keyboard.current == null || plants == null || plants.Length == 0) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame && plants.Length > 0) SelectPlant(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && plants.Length > 1) SelectPlant(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && plants.Length > 2) SelectPlant(2);
        
        // Shovel Mode on 4
        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)
        {
            isShovelMode = true;
            Debug.Log("Equipped: Shovel");
            UpdateIndicatorColor(Color.red);
        }
    }

    void SelectPlant(int index)
    {
        isShovelMode = false;
        currentPlantIndex = index;
        Debug.Log("Equipped: " + plants[index].name);
        UpdateIndicatorColor(Color.yellow);
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

                // Change indicator color if invalid (planting on occupied, or shoveling empty)
                if (isShovelMode)
                {
                    UpdateIndicatorColor(square.isOccupied ? Color.red : Color.gray);
                }
                else
                {
                    UpdateIndicatorColor(square.isOccupied ? Color.red : Color.yellow);
                }
            }
            else
            {
                currentSquare = null;
                currentIndicator.SetActive(false);
            }
        }
        else
        {
            currentSquare = null;
            currentIndicator.SetActive(false);
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
                    // Find the plant object and destroy it
                    Collider[] hits = Physics.OverlapBox(currentSquare.transform.position + Vector3.up * 0.5f, Vector3.one * 0.4f);
                    foreach (var h in hits)
                    {
                        if (h.gameObject != gameObject && !h.CompareTag("PlantableNode") && !h.gameObject.name.Contains("Sun"))
                        {
                            Destroy(h.transform.root.gameObject);
                        }
                    }
                    currentSquare.SetOccupied(false);
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

                // Proceed with planting
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.SpendSun(activePlant.cost);
                }

                activePlant.currentCooldown = activePlant.cooldownTime;
                
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
