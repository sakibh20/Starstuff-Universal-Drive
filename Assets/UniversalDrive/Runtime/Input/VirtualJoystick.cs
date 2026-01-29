using UnityEngine;
using UnityEngine.EventSystems;

namespace UniversalDrive
{
    /// <summary>
    /// Simple on-screen joystick that reports normalized X/Y input.
    /// Drag direction determines throttle (Y) and steering (X).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        private RectTransform _background;
        private RectTransform _handle;
        private Vector2 _inputVector;

        [SerializeField] private float handleRange = 150f; // max handle displacement in pixels

        public Vector2 InputVector => _inputVector;

        private void Awake()
        {
            _background = GetComponent<RectTransform>();
            _handle = transform.GetChild(0).GetComponent<RectTransform>(); // assume handle is first child
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out position);

            // Normalize relative to handle range
            position = Vector2.ClampMagnitude(position, handleRange);
            _handle.anchoredPosition = position;

            _inputVector = position / handleRange;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
        }
    }
}