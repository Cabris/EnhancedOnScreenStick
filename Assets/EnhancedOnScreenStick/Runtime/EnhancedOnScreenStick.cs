using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Serialization;

namespace EnhancedOnScreenControls
{
    public enum StickType
    {
        Fixed = 0,
        Floating = 1,
        Dynamic = 2
    }

    public enum AxisOptions
    {
        Both = 0,
        Horizontal = 1,
        Vertical = 2
    }

    [AddComponentMenu("Input/Enhanced On-Screen Stick")]
    [RequireComponent(typeof(RectTransform))]
    public class EnhancedOnScreenStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [InputControl(layout = "Vector2")]
        [FormerlySerializedAs("controlPath")]
        [SerializeField]
        string internalControlPath;

        [SerializeField] StickType stickType;
        [SerializeField] AxisOptions axisOptions = AxisOptions.Both;
        [SerializeField] float movementRange = 50f;
        [SerializeField, Range(0f, 1f)] float deadZone = 0f;
        [SerializeField] bool showOnlyWhenPressed;

        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;

        protected override string controlPathInternal
        {
            get => internalControlPath;
            set => internalControlPath = value;
        }

        public StickType StickType
        {
            get => stickType;
            set => stickType = value;
        }

        public float MovementRange
        {
            get => movementRange;
            set => movementRange = value;
        }

        public float DeadZone
        {
            get => deadZone;
            set => deadZone = value;
        }

        RectTransform rectTransform;
        Canvas canvas;

        protected void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();

            if (showOnlyWhenPressed) background.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            background.gameObject.SetActive(true);

            if (stickType != StickType.Fixed)
            {
                background.localPosition = ScreenToAnchoredPosition(eventData.position);
            }

            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SentDefaultValueToControl();

            handle.anchoredPosition = Vector2.zero;

            if (showOnlyWhenPressed) background.gameObject.SetActive(false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Camera camera = canvas.worldCamera;
            if(canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                camera = null;
            var position = RectTransformUtility.WorldToScreenPoint(camera, background.position);

            var input = (eventData.position - position) / (movementRange * canvas.scaleFactor) * EnabledAxis();
            var rawMagnitude = input.magnitude;
            var normalized = input.normalized;

            if (rawMagnitude < deadZone) input = Vector2.zero;
            else if (rawMagnitude > 1f) input = input.normalized;

            SendValueToControl(input);

            if (stickType == StickType.Dynamic && rawMagnitude > 1f)
            {
                var difference = movementRange * (rawMagnitude - 1f) * normalized;
                background.anchoredPosition += difference;
            }

            handle.anchoredPosition = input * movementRange;
        }

        Vector2 ScreenToAnchoredPosition(Vector2 screenPosition)
        {
            Camera camera = canvas.worldCamera;
            if(canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                camera = null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, camera, out var localPoint))
            {
                var pivotOffset = rectTransform.pivot * rectTransform.sizeDelta;
                return localPoint - (background.anchorMax * rectTransform.sizeDelta) + pivotOffset;
            }
            return Vector2.zero;
        }

        Vector2 EnabledAxis()
        {
            if (axisOptions == AxisOptions.Horizontal)
                return Vector2.right;
            else if (axisOptions == AxisOptions.Vertical)
                return Vector2.up;
            return Vector2.one;
        }
    }
}