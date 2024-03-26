using System;
using UnityEngine;

namespace EasyMobileInput
{
    public abstract class InterpolateInputProcessor<T> : InputProcessor<T>, IUpdatableInputProcessor where T : struct, IEquatable<T>
    {
        [SerializeField]
        private float speed = 1.0f;

        private T target;

        public override T CurrentOutput
        {
            get;
            protected set;
        }

        public override void FeedInput(T input)
        {
            target = input;
        }

        public virtual void Update()
        {
            CurrentOutput = Interpolate(target, Time.deltaTime * speed);
        }

        protected abstract T Interpolate(T target, float factor);
    }
}