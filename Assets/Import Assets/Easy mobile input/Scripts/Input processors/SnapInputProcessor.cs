using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Snap")]
    public class SnapInputProcessor : InputProcessor<Vector3>
    {
        [SerializeField]
        private List<Vector3> axies = new List<Vector3>() { new Vector3(1, 0), new Vector3(0, 1), new Vector3(-1, 0), new Vector3(0, -1) };

        public override Vector3 CurrentOutput
        {
            get;
            protected set;
        }

        public override Vector3 TransformInput(Vector3 input)
        {
            var closestAngle = float.MaxValue;
            var closestIndex = -1;

            for (var index = 0; index < axies.Count; index++)
            {
                var angle = Vector3.Angle(input, axies[index]);
                if (closestAngle > angle)
                {
                    closestAngle = angle;
                    closestIndex = index;
                }
            }

            return closestIndex == -1 ? input : Vector3.Project(input, axies[closestIndex]);
        }
    }
}
