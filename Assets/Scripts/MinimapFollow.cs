using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;
    public float height = 20f;
    public bool rotateWithTarget = false;

    [Header("Map Boundaries")]
    public bool useBounds = false;
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    void LateUpdate()
    {
        if (target != null)
        {
            // Follow the X, Z position of the target, keep Y height fixed
            Vector3 newPosition = target.position;
            newPosition.y = height;
            
            // Apply map boundaries to prevent camera from moving outside
            if (useBounds)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            }
            
            transform.position = newPosition;

            // Optionally rotate the map with the target
            if (rotateWithTarget)
            {
                transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
            }
            else
            {
                // Lock rotation to point North
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }
}
