using System;
using System.Collections;
using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Delay")]
    public class DelayInputProcessor<T> : InputProcessor<T> where T : struct, IEquatable<T>
    {
        [SerializeField]
        private float delay = 0.5f;
        
        public override T CurrentOutput
        {
            get;
            protected set;
        }

        public override void FeedInput(T input)
        {
            if (delay > 0.0f)
                StartCoroutine(DelayInput(input));
            else
                CurrentOutput = input;
        }

        private IEnumerator DelayInput(T value)
        {
            yield return new WaitForSeconds(delay);

            CurrentOutput = value;
        }
    }
}