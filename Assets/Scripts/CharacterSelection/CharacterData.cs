using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public CharacterClass characterClass;
    public GameObject characterPrefab;
    public Sprite portrait;

    [Header("Default Equipment")]
    public EquipmentData defaultRightHand;
    public EquipmentData defaultLeftHand;

    [Header("Allowed Equipment")]
    public List<EquipmentType> allowedEquipmentTypes = new List<EquipmentType>();

    [Header("3D Portrait Preview Settings")]
    public Vector3 previewRotation = new Vector3(0, 165f, 0);
    public Vector3 previewOffset = new Vector3(0, -0.65f, 0);
    public float previewScale = 1.0f;
    public float cameraOrthographicSize = 0.65f;
}

