using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>
    /// Placeholder art generated at runtime so the project runs with zero imported assets.
    /// Replace with real sprites (Assets/Art) when you have them.
    /// </summary>
    public static class ProceduralSprites
    {
        static Sprite circle, rounded, planet;

        public static Sprite Circle => circle ? circle : circle = CreateCircle(128);
        public static Sprite Rounded => rounded ? rounded : rounded = CreateRounded(64, 20);
        public static Sprite Planet => planet ? planet : planet = CreatePlanet(512);

        static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        static Sprite CreateCircle(int size)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        /// <summary>White rounded rectangle, 9-sliced so it scales to any size.</summary>
        static Sprite CreateRounded(int size, int radius)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        /// <summary>Shaded planet with bands, lit from the top-left.</summary>
        static Sprite CreatePlanet(int size)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            var light = new Vector2(-0.45f, 0.45f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2 - 1, (y + 0.5f) / size * 2 - 1);
                float r = p.magnitude;
                float edge = Mathf.Clamp01((1 - r) * size / 2);
                float shade = Mathf.Clamp01(1.1f - Vector2.Distance(p, light) / 1.5f);
                float bands = Mathf.Sin(p.y * 14 + Mathf.Sin(p.x * 3) * 1.5f) * 0.07f;
                var c = Color.Lerp(Theme.PlanetDark, Theme.PlanetLight, Mathf.Clamp01(shade + bands));
                c.a = edge;
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
