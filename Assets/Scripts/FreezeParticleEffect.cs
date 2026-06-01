using UnityEngine;

// Code-driven frost burst used when the player grabs the Fuel Freeze powerup.
// Builds a ParticleSystem entirely at runtime (no prefab/material setup needed),
// so it works the same in URP 2D without any inspector wiring.
[RequireComponent(typeof(ParticleSystem))]
public class FreezeParticleEffect : MonoBehaviour
{
    private static Material sharedMaterial;
    private static Texture2D sharedTexture;

    // Spawns a tight frost burst centered on the pickup itself.
    public static FreezeParticleEffect Spawn(Vector3 position)
    {
        GameObject go = new GameObject("FreezeFrostEffect");
        go.transform.position = position;

        FreezeParticleEffect effect = go.AddComponent<FreezeParticleEffect>();
        effect.Configure();
        return effect;
    }

    private void Configure()
    {
        const float particleLife = 1.1f;

        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, particleLife);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.03f; // drift gently upward like cold mist
        // Local space keeps the flakes hugging the item instead of smearing
        // across the screen as the world scrolls.
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 80;
        main.startColor = BuildFrostGradient();

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        // A single quick puff of ice crystals localized to the pickup.
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.308f;
        shape.radiusThickness = 1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSharedMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 50; // draw above gameplay sprites

        ps.Play();

        // Tear down once the last flakes have faded.
        Destroy(gameObject, particleLife + 0.3f);
    }

    // Mostly blue/white frost with an occasional sprinkle of cyan, icy purple
    // and pale pink so the burst feels lively rather than flat.
    private static ParticleSystem.MinMaxGradient BuildFrostGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0.0f),       // white
                new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0.25f),  // icy light blue
                new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0.5f),   // blue
                new GradientColorKey(new Color(0.45f, 0.95f, 1f), 0.7f), // cyan sprinkle
                new GradientColorKey(new Color(0.75f, 0.65f, 1f), 0.85f),// icy purple sprinkle
                new GradientColorKey(new Color(1f, 0.8f, 0.95f), 1.0f),  // pale pink sprinkle
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

        return new ParticleSystem.MinMaxGradient(gradient)
        {
            mode = ParticleSystemGradientMode.RandomColor
        };
    }

    private static Material GetSharedMaterial()
    {
        if (sharedMaterial != null)
            return sharedMaterial;

        // Sprites/Default exists across render pipelines (incl. URP 2D) and
        // respects per-particle vertex colors.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        sharedMaterial = new Material(shader)
        {
            mainTexture = GetSharedTexture()
        };
        return sharedMaterial;
    }

    // Crystalline ice sparkle: a soft core glow with six radiating spikes so
    // each particle reads as a frost crystal / snowflake glint rather than a
    // plain fuzzy dot.
    private static Texture2D GetSharedTexture()
    {
        if (sharedTexture != null)
            return sharedTexture;

        const int size = 64;
        sharedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float nd = Mathf.Sqrt(dx * dx + dy * dy) / maxDist; // 0 center..1 edge

                // Soft central glow.
                float glow = Mathf.Clamp01(1f - nd);
                glow *= glow;

                // Six thin spikes (a frost-crystal star) that fade outward.
                float angle = Mathf.Atan2(dy, dx);
                float spikes = Mathf.Pow(Mathf.Abs(Mathf.Cos(3f * angle)), 14f);
                float streak = spikes * Mathf.Clamp01(1f - nd) * 0.9f;

                float alpha = Mathf.Clamp01(glow * 0.85f + streak);
                sharedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        sharedTexture.Apply();
        return sharedTexture;
    }
}
