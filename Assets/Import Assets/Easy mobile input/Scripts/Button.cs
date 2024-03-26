using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EasyMobileInput
{
    public class Button : InputElement<bool>, IPointerDownHandler, IPointerUpHandler
    {
        protected override bool CurrentRawValue
        {
            get;
            set;
        }

        public event Action OnPressed;
        public event Action OnReleased;

        protected override void Awake()
        {
            base.Awake();

            OnInputChanged += (previous, current) =>
                {
                    if (previous)
                    {
                        if (OnReleased != null)
                            OnReleased();
                    }
                    else
                    {
                        if (OnPressed != null)
                            OnPressed();
                    }
                };
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CurrentRawValue = true;
            UpdateInput(CurrentRawValue);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CurrentRawValue = false;
            UpdateInput(CurrentRawValue);
        }
    }
}
