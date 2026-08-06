using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag your Player object here")]
    public Transform target; 
    
    [Header("Camera Settings")]
    [Tooltip("How far away the camera should be (X, Y, Z)")]
    public Vector3 offset = new Vector3(0f, 6f, -8f); 
    
    [Tooltip("How fast the camera catches up to the player")]
    public float smoothSpeed = 5f;

    void LateUpdate() // LateUpdate is best for cameras so it moves AFTER the player moves
    {
        if (target == null)
            return; // Do nothing if we haven't assigned the player yet

        // 1. Calculate where the camera SHOULD be
        Vector3 desiredPosition = target.position + offset;
        
        // 2. Smoothly move the camera from its current position to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. Make sure the camera is always pointing at the player (slightly above their feet so it looks at their body/head)
        transform.LookAt(target.position + Vector3.up * 1.5f); 
    }
}
