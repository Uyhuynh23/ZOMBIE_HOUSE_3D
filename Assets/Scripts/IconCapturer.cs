using UnityEngine;
using System.IO;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IconCapturer : MonoBehaviour
{
    public Camera captureCamera;
    public string savePath = "Assets/CapturedIcons/";
    public string fileName = "WeaponIcon";
    public int imageWidth = 256;
    public int imageHeight = 256;

    private void Reset()
    {
        captureCamera = GetComponent<Camera>();
    }

    [ContextMenu("Capture Icon")]
    public void CaptureIcon()
    {
        if (captureCamera == null)
        {
            Debug.LogError("Capture Camera is not assigned!");
            return;
        }

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        if (Application.isPlaying)
        {
            StartCoroutine(CaptureRoutine());
        }
        else
        {
            Debug.LogWarning("You are using a Custom Render Pipeline (URP). Please enter Play Mode to capture the icon properly, or the image will be black.");
        }
    }

    private IEnumerator CaptureRoutine()
    {
        // Setup a RenderTexture with transparency support
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        captureCamera.targetTexture = rt;
        
        CameraClearFlags originalFlags = captureCamera.clearFlags;
        Color originalColor = captureCamera.backgroundColor;
        
        // Force transparent background
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);

        // Wait for URP to finish rendering the frame
        yield return new WaitForEndOfFrame();

        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenShot.Apply();
        
        // Cleanup
        captureCamera.targetTexture = null;
        captureCamera.clearFlags = originalFlags;
        captureCamera.backgroundColor = originalColor;
        RenderTexture.active = null; 
        
        Destroy(rt);
        
        byte[] bytes = screenShot.EncodeToPNG();
        string fullPath = Path.Combine(savePath, fileName + ".png");
        File.WriteAllBytes(fullPath, bytes);
        
        Debug.Log($"Icon saved to {fullPath}");
        
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}
