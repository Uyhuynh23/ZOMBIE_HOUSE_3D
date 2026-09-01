using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Game/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    public string equipmentName;
    public EquipmentType equipmentType;
    public GameObject equipmentPrefab;
    public Sprite icon;
    public EquipSlot slot = EquipSlot.RightHand;
}
