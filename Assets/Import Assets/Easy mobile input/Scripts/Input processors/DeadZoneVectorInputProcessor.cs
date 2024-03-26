using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EasyMobileInput
{
    public class DeadZoneVectorInputProcessor : DeadZoneInputProcessor<Vector3>
    {
        public override Vector3 CurrentOutput
        {
            get;
            protected set;
        }

        protected override bool IsHigher(Vector3 value)
        {
            return value.magnitude >= zone;
        }
    }
}