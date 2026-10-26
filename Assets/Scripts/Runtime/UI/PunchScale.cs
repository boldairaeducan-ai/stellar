using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>Quick "pop" feedback: scales up then springs back to 1.</summary>
    public sealed class PunchScale : MonoBehaviour
    {
        float amount;

        public void Play(float strength = 0.08f)
        {
            amount = Mathf.Max(amount, strength);
            enabled = true;
        }

        void Update()
        {
            amount = Mathf.Lerp(amount, 0, Time.unscaledDeltaTime * 14f);
            if (amount < 0.001f)
            {
                amount = 0;
                enabled = false;
            }
            transform.localScale = Vector3.one * (1 + amount);
        }
    }
}
