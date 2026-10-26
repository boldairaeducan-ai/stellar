using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Pooled burst of small glowing dots, used when tapping the planet.</summary>
    public sealed class TapParticles : MonoBehaviour
    {
        const float Lifetime = 0.55f;

        struct Particle
        {
            public Image Image;
            public Vector2 Velocity;
            public float Age;
        }

        readonly List<Particle> active = new List<Particle>();
        readonly Stack<Image> free = new Stack<Image>();

        public void Burst(Vector2 localPosition, int count = 7)
        {
            for (int i = 0; i < count; i++)
            {
                var image = free.Count > 0 ? free.Pop() : UIFactory.Image("Particle", transform, Color.white, ProceduralSprites.Circle);
                float size = Random.Range(10f, 22f);
                image.rectTransform.sizeDelta = new Vector2(size, size);
                image.rectTransform.anchoredPosition = localPosition;
                image.color = Color.Lerp(Theme.Accent, Theme.PlanetLight, Random.value);
                image.gameObject.SetActive(true);

                float angle = Random.Range(0f, Mathf.PI * 2);
                float speed = Random.Range(250f, 520f);
                active.Add(new Particle { Image = image, Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Age += dt;
                if (p.Age >= Lifetime)
                {
                    p.Image.gameObject.SetActive(false);
                    free.Push(p.Image);
                    active.RemoveAt(i);
                    continue;
                }
                p.Velocity *= 1 - dt * 4f; // drag
                p.Image.rectTransform.anchoredPosition += p.Velocity * dt;
                var c = p.Image.color;
                c.a = 1 - p.Age / Lifetime;
                p.Image.color = c;
                active[i] = p;
            }
        }
    }
}
