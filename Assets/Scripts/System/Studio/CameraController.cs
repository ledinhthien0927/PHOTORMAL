using System.Collections;
using UnityEngine;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;

    // IMPORTANT: this method now returns via callback because it waits for end of frame
    public void CapturePhoto(System.Action<Texture2D> onDone)
    {
        StartCoroutine(CaptureRoutine(onDone));
    }

    private IEnumerator CaptureRoutine(System.Action<Texture2D> onDone)
    {
        // Wait until the frame is fully rendered
        yield return new WaitForEndOfFrame();

        // Capture the screen (player POV)
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        // Optional: resize to width/height (fast nearest-neighbor)
        Texture2D resized = Resize(tex, width, height);
        Destroy(tex);

        onDone?.Invoke(resized);
    }

    private Texture2D Resize(Texture2D src, int w, int h)
    {
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGB24, false);
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            int sy = Mathf.FloorToInt((float)y / h * src.height);
            for (int x = 0; x < w; x++)
            {
                int sx = Mathf.FloorToInt((float)x / w * src.width);
                pixels[y * w + x] = src.GetPixel(sx, sy);
            }
        }

        dst.SetPixels(pixels);
        dst.Apply();
        return dst;
    }
}