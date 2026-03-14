using System.Collections;
using UnityEngine;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;

    [Header("UI To Hide While Shooting")]
    [SerializeField] private GameObject uiRoot;

    [Header("Audio")]
    [SerializeField] private AudioClip cameraShotSound;

    public void CapturePhoto(System.Action<Texture2D> onDone)
    {
        StartCoroutine(CaptureRoutine(onDone));
    }

    private IEnumerator CaptureRoutine(System.Action<Texture2D> onDone)
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);

        yield return new WaitForEndOfFrame();

        // CAMERA SOUND
        AudioManager.Instance?.PlaySFX(cameraShotSound);

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        if (uiRoot != null)
            uiRoot.SetActive(true);

        Texture2D resized = Resize(tex, width, height);
        Destroy(tex);

        onDone?.Invoke(resized);
    }

    private Texture2D Resize(Texture2D src, int w, int h)
    {
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGB24, false);

        for (int y = 0; y < h; y++)
        {
            int sy = Mathf.FloorToInt((float)y / h * src.height);
            for (int x = 0; x < w; x++)
            {
                int sx = Mathf.FloorToInt((float)x / w * src.width);
                dst.SetPixel(x, y, src.GetPixel(sx, sy));
            }
        }

        dst.Apply();
        return dst;
    }
}