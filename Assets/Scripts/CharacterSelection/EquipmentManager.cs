using UnityEngine;

/// <summary>
/// Manages visual equipment attachment on a character prefab.
/// Finds handslot.l and handslot.r in the skeleton hierarchy and
/// instantiates/destroys weapon/shield models as children.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    private Transform rightHandSlot;
    private Transform leftHandSlot;

    private GameObject currentRightEquip;
    private GameObject currentLeftEquip;

    private EquipmentData rightHandData;
    private EquipmentData leftHandData;

    public EquipmentData RightHandData => rightHandData;
    public EquipmentData LeftHandData => leftHandData;

    void Awake()
    {
        FindHandSlots();
    }

    void FindHandSlots()
    {
        // Search entire hierarchy for handslot transforms
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            string nameLower = t.name.ToLower();
            if (nameLower == "handslot.r" || nameLower == "handslot_r" || nameLower == "handslotr")
            {
                rightHandSlot = t;
            }
            else if (nameLower == "handslot.l" || nameLower == "handslot_l" || nameLower == "handslotl")
            {
                leftHandSlot = t;
            }
        }

        if (rightHandSlot == null)
            Debug.LogWarning($"[EquipmentManager] Could not find right hand slot on {gameObject.name}");
        if (leftHandSlot == null)
            Debug.LogWarning($"[EquipmentManager] Could not find left hand slot on {gameObject.name}");
    }

    /// <summary>
    /// Equip an item to the appropriate hand slot based on EquipmentData.slot.
    /// </summary>
    public void Equip(EquipmentData equipment)
    {
        if (equipment == null) return;

        if (equipment.slot == EquipSlot.RightHand)
            EquipRight(equipment);
        else
            EquipLeft(equipment);
    }

    /// <summary>
    /// Equip a weapon/item to the right hand.
    /// </summary>
    public void EquipRight(EquipmentData equipment)
    {
        if (equipment == null || equipment.equipmentPrefab == null) return;

        ClearSlot(EquipSlot.RightHand);

        if (rightHandSlot != null)
        {
            currentRightEquip = Instantiate(equipment.equipmentPrefab, rightHandSlot);
            currentRightEquip.transform.localPosition = Vector3.zero;
            currentRightEquip.transform.localRotation = Quaternion.identity;
            rightHandData = equipment;
        }
    }

    /// <summary>
    /// Equip a shield/offhand item to the left hand.
    /// </summary>
    public void EquipLeft(EquipmentData equipment)
    {
        if (equipment == null || equipment.equipmentPrefab == null) return;

        ClearSlot(EquipSlot.LeftHand);

        if (leftHandSlot != null)
        {
            currentLeftEquip = Instantiate(equipment.equipmentPrefab, leftHandSlot);
            currentLeftEquip.transform.localPosition = Vector3.zero;
            currentLeftEquip.transform.localRotation = Quaternion.identity;
            leftHandData = equipment;
        }
    }

    /// <summary>
    /// Remove equipment from a specific slot.
    /// </summary>
    public void ClearSlot(EquipSlot slot)
    {
        if (slot == EquipSlot.RightHand)
        {
            if (currentRightEquip != null)
            {
                if (Application.isPlaying)
                    Destroy(currentRightEquip);
                else
                    DestroyImmediate(currentRightEquip);
            }
            currentRightEquip = null;
            rightHandData = null;
        }
        else
        {
            if (currentLeftEquip != null)
            {
                if (Application.isPlaying)
                    Destroy(currentLeftEquip);
                else
                    DestroyImmediate(currentLeftEquip);
            }
            currentLeftEquip = null;
            leftHandData = null;
        }
    }

    /// <summary>
    /// Remove all equipment.
    /// </summary>
    public void ClearAll()
    {
        ClearSlot(EquipSlot.RightHand);
        ClearSlot(EquipSlot.LeftHand);
    }

    /// <summary>
    /// Clear any pre-existing weapon/shield meshes that are baked into the FBX
    /// (e.g., Knight's built-in swords and shields under handslot children).
    /// Call this before equipping to start clean.
    /// </summary>
    public void ClearBuiltInEquipment()
    {
        ClearChildMeshes(rightHandSlot);
        ClearChildMeshes(leftHandSlot);
    }

    private void ClearChildMeshes(Transform slot)
    {
        if (slot == null) return;

        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            Transform child = slot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
