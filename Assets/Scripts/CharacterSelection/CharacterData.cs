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
}
