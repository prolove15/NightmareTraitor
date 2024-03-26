using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EasyMobileInput
{
    public abstract class EventInputProcessor<T, E> : InputProcessor<T> where T : struct, IEquatable<T> where E : UnityEvent<T>
    {
        [Serializable]
        public class Event : UnityEvent<T> { }

        [SerializeField]
        private E inputEvent;

        public E InputEvent
        {
            get
            {
                return inputEvent;
            }
        }

        public override T CurrentOutput { get; protected set; }

        public override void FeedInput(T input)
        {
            var currentOutput = CurrentOutput;
            base.FeedInput(input);
            if (!currentOutput.Equals(input))
                inputEvent.Invoke(input);
        }
    }
}