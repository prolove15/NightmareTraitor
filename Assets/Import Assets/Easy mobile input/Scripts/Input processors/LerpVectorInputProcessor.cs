using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Linear interpolation")]
    public class LerpVectorInputProcessor : InterpolateInputProcessor<Vector3>
    {
        protected override Vector3 Interpolate(Vector3 target, float factor)
        {
            return Vector3.Lerp(CurrentOutput, target, factor);
        }
    }
}
