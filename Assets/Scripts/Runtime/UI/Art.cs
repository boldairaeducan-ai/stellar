using System.Collections.Generic;
using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>
    /// Sprite slots. Drop an image in Assets/Resources/Art/ with the expected name and it replaces the
    /// generated placeholder automatically (no code change). Works whether the texture is imported
    /// as "Sprite (2D and UI)" or as a default texture.
    ///
    ///   Art/background          full-screen background (portrait)
    ///   Art/planet              the tappable planet (square, transparent corners)
    ///   Art/cargo_pod           bonus drop pod
    ///   Art/Generators/{id}     one icon per generator id from GameBalance.json (drone, harvester, ...)
    ///   Art/Icons/crystal, Art/Icons/dark_matter
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background => Custom("background");
        public static Sprite Planet
        {
            get
            {
                var custom = Custom("planet");
                return custom != null ? custom : ProceduralSprites.Planet; // no "??" on Unity objects
            }
        }
        public static Sprite CargoPod => Custom("cargo_pod");
        public static Sprite Generator(string id) => Custom("Generators/" + id);
        public static Sprite CrystalIcon => Custom("Icons/crystal");
        public static Sprite DarkMatterIcon => Custom("Icons/dark_matter");

        /// <summary>The custom sprite at Resources/Art/{path}, or null if none was provided.</summary>
        public static Sprite Custom(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;

            string full = "Art/" + path;
            var sprite = Resources.Load<Sprite>(full);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(full);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            }
            Cache[path] = sprite;
            return sprite;
        }
    }
}
