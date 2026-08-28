using UnityEngine;

public class PlantableSquare : MonoBehaviour
{
    public bool isOccupied = false;
    
    // Optional visual feedback
    private Renderer rend;
    private Color originalColor;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }
    
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        
        if (rend != null)
        {
            if (occupied)
            {
                rend.material.color = originalColor * 0.5f; // Darken when occupied
            }
            else
            {
                rend.material.color = originalColor;
            }
        }
    }
}
