using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EasyMobileInput
{
    public class Joystick : GenericJoystick<Vector3>, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public event Action OnInputStarted;
        public event Action OnInputEnded;
        
        [SerializeField]
        [HideInInspector]
        private string valueFetchStageGuid;

        [SerializeField]
        private bool moveToTouchPosition = true;

        [SerializeField]
        private bool resetInputOnPointerUp = true;

        [SerializeField]
        private float maxKnobDistance = 150.0f;

        protected override Vector3 CurrentRawValue { get; set; }

        public BaseInputProcessor ValueFetchStage
        {
            get
            {
                return GetProcessorWithGuid(valueFetchStageGuid);
            }

            set
            {
                if (value == null)
                {
                    valueFetchStageGuid = string.Empty;
                    return;
                }

                if (Processors.Find(x => x == value) == null)
                {
                    Debug.LogError("Can't set fetch stage that doesn't belong to this joystick");
                    return;
                }

                valueFetchStageGuid = value.GUID;
            }
        }

        [SerializeField]
        private RectTransform knob;
        public RectTransform Knob
        {
            get
            {
                return knob;
            }

            set
            {
                knob = value;
            }
        }

        [SerializeField]
        private RectTransform back;
        public RectTransform Back
        {
            get
            {
                return back;
            }

            set
            {
                back = value;
            }
        }

        private int pointerIdDraggingJoystick = -1;

        private BaseInputProcessor GetProcessorWithGuid(string guid)
        {
            return Processors == null ? null : Processors.Find(x => x != null && x is BaseInputProcessor && (x as BaseInputProcessor).GUID == guid) as BaseInputProcessor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerIdDraggingJoystick != -1)
                return;

            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, eventData.pressEventCamera, out point))
                return;

            if (moveToTouchPosition && back)
                back.localPosition = point;

            pointerIdDraggingJoystick = eventData.pointerId;
            UpdatePosition(eventData);

            if (OnInputStarted != null)
                OnInputStarted();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerIdDraggingJoystick)
                return;

            pointerIdDraggingJoystick = -1;

            if (resetInputOnPointerUp)
                CurrentRawValue = Vector2.zero;
            
            if (OnInputEnded != null)
                OnInputEnded();
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdatePosition(eventData);
        }

        private void UpdatePosition(PointerEventData eventData)
        {
            if (!knob)
                return;

            if (eventData.pointerId != pointerIdDraggingJoystick)
                return;

            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(knob.parent as RectTransform, eventData.position, eventData.pressEventCamera, out point))
                return;

            var clampedPoint = Vector2.ClampMagnitude(point, maxKnobDistance);

            CurrentRawValue = clampedPoint / maxKnobDistance;
        }

        protected override void Update()
        {
            base.Update();

            if (!knob)
                return;
            
            var valueToUse = ValueFetchStage == null ? CurrentRawValue : (ValueFetchStage as IInputProcessor<Vector3>).CurrentOutput;

            knob.anchoredPosition = valueToUse * maxKnobDistance;
        }
    }
}
