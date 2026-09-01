using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Game/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    public string equipmentName;
    public EquipmentType equipmentType;
    public GameObject equipmentPrefab;
    public Sprite icon;
    public EquipSlot slot = EquipSlot.RightHand;

    [Header("3D Portrait Preview Settings")]
    public Vector3 previewRotation = new Vector3(0, 0, -45f);
    public Vector3 previewOffset = Vector3.zero;
    public float previewScale = 1.0f;
    public float cameraOrthographicSize = 0.8f;
}

