
using UnityEngine;
using UnityEngine.UI;

public class KernelPreviewUI : MonoBehaviour
{
    [Header("UI")]
    public RawImage targetImage;

    [Header("Preview Settings")]
    public int textureSize = 512;               
    public Color backgroundColor = Color.white; 
    public Color pointColor = Color.blue;
    public float pointRadius = 3.0f; 

    private Texture2D _texture;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }

        _texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        _texture.wrapMode = TextureWrapMode.Clamp;

        // Bilinear filtering helps smooth the circles
        _texture.filterMode = FilterMode.Bilinear;

        ClearTexture();
        _texture.Apply();

        if (targetImage != null)
        {
            targetImage.texture = _texture;
        }
    }

    /// <summary>
    /// Call this whenever MLKernelStore.Kernel is updated.
    /// </summary>
    public void RefreshPreview()
    {
        if (_texture == null) return;

        ClearTexture();

        var kernel = MLKernelStore.Kernel;
        if (kernel == null || kernel.Length == 0)
        {
            _texture.Apply();
            return;
        }

        int w = _texture.width;
        int h = _texture.height;

        foreach (var v in kernel)
        {
            // v.x, v.y in [-1,1] -> [0,1]
            float u = (v.x * 0.5f) + 0.5f;
            float vv = (v.y * 0.5f) + 0.5f;

            int px = Mathf.RoundToInt(u * (w - 1));
            int py = Mathf.RoundToInt(vv * (h - 1));

            DrawDiskAA(px, py, pointRadius, pointColor);
        }

        _texture.Apply();
    }

    private void ClearTexture()
    {
        if (_texture == null) return;

        Color[] pixels = _texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;

        _texture.SetPixels(pixels);
    }

    /// <summary>
    /// Simple anti-aliased disk: center fully colored, edges smoothly fade.
    /// </summary>
    private void DrawDiskAA(int cx, int cy, float radius, Color col)
    {
        int w = _texture.width;
        int h = _texture.height;

        // extra 1px band for smooth edge
        float aaRadius = radius + 1.0f;
        float r2 = radius * radius;
        float aaR2 = aaRadius * aaRadius;

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - aaRadius));
        int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + aaRadius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - aaRadius));
        int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + aaRadius));

        for (int y = minY; y <= maxY; y++)
        {
            float dy = y - cy;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float d2 = dx * dx + dy * dy;

                if (d2 > aaR2) continue; // outside AA band

                // t = 1 inside radius, fades to 0 at aaRadius
                float t;
                if (d2 <= r2)
                {
                    t = 1.0f;
                }
                else
                {
                    float d = Mathf.Sqrt(d2);
                    t = Mathf.Clamp01((aaRadius - d) / (aaRadius - radius));
                }

                if (t <= 0f) continue;

                Color baseCol = _texture.GetPixel(x, y);
                // simple alpha blend / lerp towards point color
                Color outCol = Color.Lerp(baseCol, col, t);
                _texture.SetPixel(x, y, outCol);
            }
        }
    }
}
