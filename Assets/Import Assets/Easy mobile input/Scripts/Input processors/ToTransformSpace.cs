using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("To transform space")]
    public class ToTransformSpace : InputProcessor<Vector3>
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private bool flattenY = true;

        public override Vector3 CurrentOutput
        {
            get;
            protected set;
        }

        public Transform Target
        {
            get
            {
                return target;
            }

            set
            {
                target = value;
            }
        }

        public override Vector3 TransformInput(Vector3 input)
        {
            if (target == null)
                return input;

            var forward = target.forward;
            if (flattenY)
            {
                forward.y = 0.0f;
                forward.Normalize();
            }

            var right = target.right;
            if (flattenY)
            {
                right.y = 0.0f;
                right.Normalize();
            }

            return forward * input.y + right * input.x;
        }
    }
}
