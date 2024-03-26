using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Dead zone")]
    public abstract class DeadZoneInputProcessor<T> : InputProcessor<T> where T : struct, IEquatable<T>
    {
        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float zone = 0.1f;

        private T currentValue;

        protected virtual T Zero
        {
            get
            {
                return default(T);
            }
        }

        public override T TransformInput(T input)
        {
            return IsHigher(input) ? input : Zero;
        }

        protected abstract bool IsHigher(T value);
    }
}
