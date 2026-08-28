using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridLength = 10;
    public float cellSize = 2f;
    public Vector3 gridOrigin = new Vector3(-10f, 0f, -10f); // Adjust based on map geometry

    [Header("Visuals")]
    public Material nodeMaterial;

    public class GridNode
    {
        public Vector2Int gridPosition;
        public Vector3 worldPosition;
        public bool isOccupied;
        public GameObject visualObject;
    }

    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GenerateGrid();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void GenerateGrid()
    {
        // Fallback material if not assigned in Inspector
        if (nodeMaterial == null)
        {
            nodeMaterial = new Material(Shader.Find("Standard"));
            nodeMaterial.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Semi-transparent green
            nodeMaterial.SetFloat("_Mode", 3);
            nodeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            nodeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            nodeMaterial.SetInt("_ZWrite", 0);
            nodeMaterial.DisableKeyword("_ALPHATEST_ON");
            nodeMaterial.EnableKeyword("_ALPHABLEND_ON");
            nodeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            nodeMaterial.renderQueue = 3000;
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                // Slightly above ground to prevent Z-fighting
                Vector3 worldPos = gridOrigin + new Vector3(x * cellSize, 0.05f, z * cellSize);

                GridNode node = new GridNode
                {
                    gridPosition = gridPos,
                    worldPosition = worldPos,
                    isOccupied = false
                };

                // Create visual node
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                visual.name = $"Node_{x}_{z}";
                visual.transform.SetParent(transform);
                visual.transform.position = worldPos;
                visual.transform.rotation = Quaternion.Euler(90, 0, 0); // Flat on ground
                visual.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1f);
                
                // Add tag (Requires 'PlantableNode' tag to exist in the project)
                try 
                { 
                    visual.tag = "PlantableNode"; 
                } 
                catch 
                { 
                    Debug.LogWarning("Tag 'PlantableNode' not found. Please add it in project settings. Disabling collider for safety."); 
                }

                // Assign material
                MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = nodeMaterial;
                }
                
                node.visualObject = visual;
                grid.Add(gridPos, node);
            }
        }
    }

    /// <summary>
    /// Gets the grid node corresponding to a specific world position.
    /// </summary>
    public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellSize);
        Vector2Int gridPos = new Vector2Int(x, z);

        if (grid.TryGetValue(gridPos, out GridNode node))
        {
            return node;
        }
        return null;
    }
}
