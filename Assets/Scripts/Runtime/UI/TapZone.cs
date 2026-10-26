using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StellarIdle.UI
{
    /// <summary>Fires on pointer down (more responsive than a Button click) and supports multi-touch.</summary>
    public sealed class TapZone : MonoBehaviour, IPointerDownHandler
    {
        public event Action<PointerEventData> Tapped;
        public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke(eventData);
    }
}
