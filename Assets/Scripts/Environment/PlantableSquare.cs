using UnityEngine;

public class PlantableSquare : MonoBehaviour
{
    private static readonly System.Collections.Generic.Dictionary<int, PlantableSquare> Registry = new();
    public bool isOccupied = false;

    /// <summary>
    /// Direct reference to the plant on this square. No physics queries needed.
    /// </summary>
    public PlantBase currentPlant;

    public GameObject hoverGlow;
    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private MaterialPropertyBlock originalPropertyBlock;
    private Color originalColor;
    private bool serverReserved;
    public int StableId { get; private set; }

    private void Awake()
    {
        StableId = ComputeStableId(transform);
        if (Registry.TryGetValue(StableId, out PlantableSquare existing) && existing != this)
            Debug.LogError($"[NET][PLANT] Duplicate square id={StableId}: {existing.name} and {name}.");
        Registry[StableId] = this;
    }

    private void OnDestroy()
    {
        if (Registry.TryGetValue(StableId, out PlantableSquare current) && current == this)
            Registry.Remove(StableId);
    }

    public static PlantableSquare Find(int stableId)
    {
        Registry.TryGetValue(stableId, out PlantableSquare square);
        return square;
    }

    public bool ServerTryReserve()
    {
        if (!NetworkGameplayAuthority.IsServer || isOccupied || serverReserved) return false;
        serverReserved = true;
        return true;
    }

    public void ServerCancelReservation()
    {
        if (NetworkGameplayAuthority.IsServer) serverReserved = false;
    }

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Transform dirt = transform.Find("DirtBed");
            if (dirt != null) rend = dirt.GetComponent<Renderer>();
            else rend = GetComponentInChildren<Renderer>();
        }
        Transform h = transform.Find("HoverGlow");
        if (h != null) hoverGlow = h.gameObject;
        propBlock = new MaterialPropertyBlock();
        originalPropertyBlock = new MaterialPropertyBlock();
        if (rend != null)
        {
            // Preserve any scene/prefab overrides exactly. Restoring only a
            // sampled colour can turn textured dirt black after a plant dies.
            rend.GetPropertyBlock(originalPropertyBlock);

            // Read color without cloning material
            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                originalColor = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color"))
                originalColor = rend.sharedMaterial.color;
            else
                originalColor = Color.white;
        }

        UpdateVisual();
    }

    /// <summary>
    /// Register a plant on this square.
    /// </summary>
    public void PlantHere(PlantBase plant)
    {
        serverReserved = false;
        isOccupied = true;
        currentPlant = plant;
        plant.mySquare = this;
        UpdateVisual();
    }

    /// <summary>
    /// Clear the plant from this square.
    /// </summary>
    public void RemovePlant()
    {
        serverReserved = false;
        isOccupied = false;
        currentPlant = null;
        UpdateVisual();
    }

    private static int ComputeStableId(Transform value)
    {
        unchecked
        {
            string path = value.GetSiblingIndex() + ":" + value.name;
            Transform current = value;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.GetSiblingIndex() + ":" + current.name + "/" + path;
            }
            string key = value.gameObject.scene.name + "/" + path;
            uint hash = 2166136261;
            for (int i = 0; i < key.Length; i++) { hash ^= key[i]; hash *= 16777619; }
            return (int)(hash & 0x7fffffff);
        }
    }

    /// <summary>
    /// Legacy method � prefer PlantHere/RemovePlant.
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        if (!occupied)
        {
            currentPlant = null;
        }
        UpdateVisual();
    }

    public void SetHover(bool hover)
    {
        if (hoverGlow != null) hoverGlow.SetActive(hover);
    }

    void UpdateVisual()
    {
        if (rend == null || propBlock == null || originalPropertyBlock == null) return;

        if (!isOccupied)
        {
            // Restore the renderer exactly as it was before occupancy tinting.
            rend.SetPropertyBlock(originalPropertyBlock);
            return;
        }

        Color c = originalColor * 0.85f;

        // Start from the original override set so repeated plant/remove cycles
        // never accumulate a darker tint.
        rend.SetPropertyBlock(originalPropertyBlock);
        rend.GetPropertyBlock(propBlock);
        if (rend.sharedMaterial.HasProperty("_BaseColor"))
            propBlock.SetColor("_BaseColor", c);
        else
            propBlock.SetColor("_Color", c);
        rend.SetPropertyBlock(propBlock);
    }
}
