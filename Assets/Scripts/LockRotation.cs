using UnityEngine;

public class LockRotation : MonoBehaviour
{
    public Vector3 fixedEulerAngles = new Vector3(0, 0, 0);

    void LateUpdate()
    {
        // Lock the rotation to fixed angles (prevents inheriting parent rotation)
        transform.rotation = Quaternion.Euler(fixedEulerAngles);
    }
}
