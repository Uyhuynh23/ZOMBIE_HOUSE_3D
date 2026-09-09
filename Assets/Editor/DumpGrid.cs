using UnityEngine;

public class DumpGrid
{
    public static void Dump()
    {
        var squares = Object.FindObjectsOfType<PlantableSquare>();
        Debug.Log($"Found {squares.Length} PlantableSquares");
        foreach(var sq in squares)
        {
            Debug.Log($"Square at {sq.transform.position}");
        }
    }
}
