using UnityEngine;
using UnityEngine.UI;

// Frost overlay shown while the Fuel Freeze powerup is active. Can render the
// built-in procedural frost or any supplied frost image (e.g. the Frost Effect
// package's Ice.tga), either framing the fuel tank or covering the full screen.
// Built/driven entirely from code so it works in URP with no post-processing.
[RequireComponent(typeof(Image))]
public class FuelTankFrost : MonoBehaviour
{
    public enum Placement { FuelTank, FullScreen }

    private float fadeInSpeed = 5f;
    private float fadeOutSpeed = 1.5f;
    private float maxAlpha = 0.9f;
    private bool alwaysOn;

    private Image image;
    private float alpha;
    private static Sprite proceduralSprite;

    // gauge: the fuel ring (used for FuelTank placement).
    // canvas: the UI canvas (used for FullScreen placement / fallback).
    // customTexture: optional frost image; when null the procedural frost is used.
    public static FuelTankFrost Create(Image gauge, Transform canvas, Texture2D customTexture,
        Color tint, float maxAlpha, Placement placement, bool alwaysOn = false)
    {
        Transform parent = placement == Placement.FullScreen
            ? canvas
            : (gauge != null ? gauge.transform : canvas);
        if (parent == null) return null;

        string name = placement == Placement.FullScreen ? "FreezeFrostScreen" : "FuelTankFrost";
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();

        FuelTankFrost frost = go.AddComponent<FuelTankFrost>();
        frost.maxAlpha = maxAlpha;
        frost.alwaysOn = alwaysOn;
        frost.Init(customTexture, tint);
        return frost;
    }

    private void Init(Texture2D customTexture, Color tint)
    {
        image = GetComponent<Image>();
        if (customTexture != null)
            image.sprite = Sprite.Create(customTexture,
                new Rect(0, 0, customTexture.width, customTexture.height),
                new Vector2(0.5f, 0.5f));
        else
            image.sprite = GetProceduralSprite();

        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        image.preserveAspect = false;

        Color c = tint;
        c.a = 0f;
        image.color = c;
        alpha = 0f;
    }

    void Update()
    {
        bool frozen = alwaysOn || (SpeedManager.Instance != null && SpeedManager.Instance.IsFrozen);
        float target = frozen ? maxAlpha : 0f;
        float speed = frozen ? fadeInSpeed : fadeOutSpeed;
        alpha = Mathf.MoveTowards(alpha, target, speed * Time.deltaTime);

        // Subtle shimmer so the ice looks alive while it lingers.
        float shimmer = frozen ? 1f - 0.08f * Mathf.Abs(Mathf.Sin(Time.time * 3f)) : 1f;

        if (image != null)
        {
            Color c = image.color;
            c.a = alpha * shimmer;
            image.color = c;
        }
    }

    // Procedural frosted-glass disc: a frosted base across the whole circle, a
    // bold icy rim ring and crystalline veins. Heaviest at the rim so a fuel
    // gauge stays readable in the middle.
    private static Sprite GetProceduralSprite()
    {
        if (proceduralSprite != null)
            return proceduralSprite;

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        const float ox = 13.37f;
        const float oy = 71.91f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center.x) / half;
                float ny = (y - center.y) / half;
                float r = Mathf.Sqrt(nx * nx + ny * ny); // 0 center .. 1 edge

                // GLSL-style edge smoothstep (Mathf.SmoothStep(0,1,t) IS that).
                float circle = 1f - Mathf.SmoothStep(0f, 1f, (r - 0.95f) / (1.05f - 0.95f));
                if (circle <= 0f)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    continue;
                }

                float u = x / (float)size;
                float v = y / (float)size;
                float cloud =
                    Mathf.PerlinNoise(u * 6f + ox, v * 6f + oy) * 0.6f +
                    Mathf.PerlinNoise(u * 14f + ox, v * 14f + oy) * 0.3f +
                    Mathf.PerlinNoise(u * 28f + ox, v * 28f + oy) * 0.1f;

                float ridgeN = Mathf.PerlinNoise(u * 18f - ox, v * 18f - oy);
                float veins = Mathf.Pow(1f - Mathf.Abs(2f * ridgeN - 1f), 5f);

                float rimRing = Mathf.Exp(-((r - 0.82f) * (r - 0.82f)) / (2f * 0.13f * 0.13f));

                float frost = Mathf.Clamp01(0.35f + rimRing * 0.55f + veins * 0.7f + cloud * 0.3f);
                float a = frost * circle;

                Color iceWhite = new Color(0.92f, 0.97f, 1f);
                Color iceBlue = new Color(0.45f, 0.75f, 1f);
                Color col = Color.Lerp(iceWhite, iceBlue, Mathf.Clamp01(veins * 0.6f + rimRing * 0.35f));
                col.a = a;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();

        proceduralSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return proceduralSprite;
    }
}
