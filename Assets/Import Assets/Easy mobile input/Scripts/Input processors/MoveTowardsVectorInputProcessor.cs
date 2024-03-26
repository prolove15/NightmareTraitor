using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Move towards")]
    public class MoveTowardsVectorInputProcessor : InterpolateInputProcessor<Vector3>
    {
        protected override Vector3 Interpolate(Vector3 target, float factor)
        {
            return Vector3.MoveTowards(CurrentOutput, target, factor);
        }
    }
}
