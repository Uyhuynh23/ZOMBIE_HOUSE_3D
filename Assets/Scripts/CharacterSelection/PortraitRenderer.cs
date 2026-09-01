using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime 3D Portrait Renderer.
/// Renders 3D models (weapons, shields, characters) using a dedicated Orthographic camera
/// and the "Portrait" layer into transparent UI Sprites in real-time.
/// </summary>
public class PortraitRenderer : MonoBehaviour
{
    [Header("Hierarchy References")]
    public Camera portraitCamera;
    public Transform modelPreviewSpot;
    public Light portraitLight;

    [Header("Settings")]
    public string portraitLayerName = "Portrait";
    public int textureWidth = 256;
    public int textureHeight = 256;

    private Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
    private int portraitLayer = -1;

    void Awake()
    {
        InitializeRenderer();
    }

    public void InitializeRenderer()
    {
        portraitLayer = LayerMask.NameToLayer(portraitLayerName);
        if (portraitLayer == -1)
        {
            portraitLayer = 6; // Layer 6 index from TagManager
        }

        if (modelPreviewSpot == null)
        {
            Transform spot = transform.Find("ModelPreviewSpot");
            if (spot != null)
            {
                modelPreviewSpot = spot;
            }
            else
            {
                GameObject spotObj = new GameObject("ModelPreviewSpot");
                spotObj.transform.SetParent(transform, false);
                spotObj.transform.localPosition = Vector3.zero;
                modelPreviewSpot = spotObj.transform;
            }
        }

        if (portraitCamera == null)
        {
            Transform camTransform = transform.Find("PortraitCamera");
            if (camTransform != null)
            {
                portraitCamera = camTransform.GetComponent<Camera>();
            }
            else
            {
                GameObject camObj = new GameObject("PortraitCamera", typeof(Camera));
                camObj.transform.SetParent(transform, false);
                camObj.transform.localPosition = new Vector3(0, 0, -5f);
                camObj.transform.localRotation = Quaternion.identity;
                portraitCamera = camObj.GetComponent<Camera>();
            }
        }

        if (portraitCamera != null)
        {
            portraitCamera.orthographic = true;
            portraitCamera.orthographicSize = 0.8f;
            portraitCamera.nearClipPlane = 0.1f;
            portraitCamera.farClipPlane = 20f;
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
            portraitCamera.cullingMask = 1 << portraitLayer;
            portraitCamera.enabled = false; // Render on-demand
        }

        if (portraitLight == null)
        {
            Transform lightTransform = transform.Find("PortraitLight");
            if (lightTransform != null)
            {
                portraitLight = lightTransform.GetComponent<Light>();
            }
            else
            {
                GameObject lightObj = new GameObject("PortraitLight", typeof(Light));
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localEulerAngles = new Vector3(35f, -30f, 0);
                portraitLight = lightObj.GetComponent<Light>();
                portraitLight.type = LightType.Directional;
                portraitLight.intensity = 1.2f;
                portraitLight.color = new Color(1f, 0.98f, 0.95f);
            }
        }

        if (portraitLight != null)
        {
            portraitLight.cullingMask = 1 << portraitLayer;
        }
    }

    /// <summary>
    /// Renders or returns cached 3D portrait sprite for an equipment item.
    /// </summary>
    public Sprite GetEquipmentPortrait(EquipmentData equip)
    {
        if (equip == null) return null;
        if (equip.equipmentPrefab == null) return equip.icon;

        string cacheKey = $"Equip_{equip.name}_{equip.equipmentName}";
        if (cachedSprites.TryGetValue(cacheKey, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Vector3 rot = equip.previewRotation;
        Vector3 offset = equip.previewOffset;
        float scale = equip.previewScale > 0.001f ? equip.previewScale : 1.0f;
        float orthoSize = equip.cameraOrthographicSize > 0.001f ? equip.cameraOrthographicSize : 0.8f;

        // Apply tailored default angles per equipment type
        if (equip.equipmentType == EquipmentType.Shield)
        {
            if (rot == new Vector3(0, 0, -45f) || rot == Vector3.zero || rot == new Vector3(10f, -25f, 0f))
            {
                // 155 degrees Y faces the FRONT of the shield towards the camera with a subtle 3D tilt
                rot = new Vector3(10f, 155f, 0f);
                orthoSize = 0.7f;
            }
        }
        else if (equip.name.Contains("Spellbook") || equip.equipmentName.Contains("Spellbook"))
        {
            if (rot == new Vector3(0, 0, -45f) || rot == Vector3.zero)
            {
                rot = new Vector3(35f, 180f, 0f);
                orthoSize = 0.65f;
            }
        }
        else if (equip.equipmentType == EquipmentType.Staff)
        {
            if (orthoSize == 0.8f) orthoSize = 1.2f;
        }
        else if (equip.equipmentType == EquipmentType.Dagger)
        {
            if (orthoSize == 0.8f) orthoSize = 0.5f;
        }
        else if (equip.equipmentType == EquipmentType.Wand)
        {
            if (orthoSize == 0.8f) orthoSize = 0.6f;
        }
        else if (equip.equipmentType == EquipmentType.Crossbow1H || equip.equipmentType == EquipmentType.Crossbow2H)
        {
            if (rot == new Vector3(0, 0, -45f) || rot == Vector3.zero)
            {
                rot = new Vector3(25f, 145f, 15f);
                orthoSize = 0.75f;
            }
        }

        Sprite generated = RenderModelToSprite(equip.equipmentPrefab, rot, offset, scale, orthoSize, cacheKey);
        if (generated != null)
        {
            cachedSprites[cacheKey] = generated;
            return generated;
        }

        return equip.icon;
    }

    /// <summary>
    /// Renders or returns cached 3D portrait sprite for a character.
    /// </summary>
    public Sprite GetCharacterPortrait(CharacterData character)
    {
        if (character == null) return null;
        if (character.characterPrefab == null) return character.portrait;

        string cacheKey = $"Char_{character.name}_{character.characterName}";
        if (cachedSprites.TryGetValue(cacheKey, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Vector3 rot = character.previewRotation;
        Vector3 offset = character.previewOffset;
        float scale = character.previewScale > 0.001f ? character.previewScale : 1.0f;
        float orthoSize = character.cameraOrthographicSize > 0.001f ? character.cameraOrthographicSize : 0.65f;

        if (rot == Vector3.zero) rot = new Vector3(0, 165f, 0);
        if (offset == Vector3.zero) offset = new Vector3(0, -0.65f, 0);

        Sprite generated = RenderModelToSprite(character.characterPrefab, rot, offset, scale, orthoSize, cacheKey, false);
        if (generated != null)
        {
            cachedSprites[cacheKey] = generated;
            return generated;
        }

        return character.portrait;
    }

    /// <summary>
    /// Instantiates the prefab at ModelPreviewSpot on the Portrait layer, renders with the Orthographic PortraitCamera, and returns a Sprite.
    /// </summary>
    public Sprite RenderModelToSprite(GameObject prefab, Vector3 rotation, Vector3 offset, float scale, float orthoSize, string cacheKey, bool autoCenter = true)
    {
        if (prefab == null) return null;
        if (portraitCamera == null || modelPreviewSpot == null || portraitLayer == -1)
        {
            InitializeRenderer();
        }

        if (portraitCamera == null || modelPreviewSpot == null) return null;

        // Ensure no leftover models exist from previous renders
        foreach (Transform child in modelPreviewSpot)
        {
            DestroyImmediate(child.gameObject);
        }

        // Instantiate model under preview spot
        GameObject previewObj = Instantiate(prefab, modelPreviewSpot);
        previewObj.transform.localPosition = Vector3.zero;
        previewObj.transform.localEulerAngles = rotation;
        previewObj.transform.localScale = Vector3.one * scale;

        // Set all children to the Portrait layer
        SetLayerRecursively(previewObj, portraitLayer);

        // Disable physics/animators on preview instance
        foreach (var col in previewObj.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var rb in previewObj.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        foreach (var anim in previewObj.GetComponentsInChildren<Animator>(true)) anim.enabled = false;

        // Auto-center the mesh bounds so pivot offset (e.g. handle) does not push the weapon out of frame
        if (autoCenter)
        {
            Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 centerShift = modelPreviewSpot.position - combinedBounds.center;
                previewObj.transform.position += centerShift + offset;

                // Adjust orthoSize if left as default so entire model fits with comfortable padding
                float maxExtent = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y);
                if (Mathf.Approximately(orthoSize, 0.8f) || orthoSize <= 0.05f)
                {
                    orthoSize = Mathf.Max(maxExtent * 1.3f, 0.5f);
                }
            }
            else
            {
                previewObj.transform.localPosition = offset;
            }
        }
        else
        {
            previewObj.transform.localPosition = offset;
        }

        portraitCamera.orthographicSize = orthoSize;

        RenderTexture rt = RenderTexture.GetTemporary(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        
        RenderTexture prevTarget = portraitCamera.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        portraitCamera.targetTexture = rt;
        portraitCamera.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        tex.Apply();

        portraitCamera.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        // Always use DestroyImmediate so this model is not captured in subsequent renders in the same frame
        DestroyImmediate(previewObj);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = cacheKey + "_3DPortrait";
        return sprite;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
