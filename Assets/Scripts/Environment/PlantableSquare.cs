using UnityEngine;

public class PlantableSquare : MonoBehaviour
{
    public bool isOccupied = false;

    /// <summary>
    /// Direct reference to the plant on this square. No physics queries needed.
    /// </summary>
    public PlantBase currentPlant;

    public GameObject hoverGlow;
    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private Color originalColor;

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
        if (rend != null)
        {
            // Read color without cloning material
            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                originalColor = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color"))
                originalColor = rend.sharedMaterial.color;
            else
                originalColor = Color.white;
        }
    }

    /// <summary>
    /// Register a plant on this square.
    /// </summary>
    public void PlantHere(PlantBase plant)
    {
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
        isOccupied = false;
        currentPlant = null;
        UpdateVisual();
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
        if (rend == null || propBlock == null) return;

        Color c = isOccupied ? originalColor * 0.5f : originalColor;

        rend.GetPropertyBlock(propBlock);
        if (rend.sharedMaterial.HasProperty("_BaseColor"))
            propBlock.SetColor("_BaseColor", c);
        else
            propBlock.SetColor("_Color", c);
        rend.SetPropertyBlock(propBlock);
    }
}
