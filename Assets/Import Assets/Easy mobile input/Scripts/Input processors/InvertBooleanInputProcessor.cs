using UnityEngine;

namespace EasyMobileInput
{
    [DecorativeName("Invert")]
    public class InvertBooleanInputProcessor : InputProcessor<bool>
    {
        public override bool CurrentOutput
        {
            get;
            protected set;
        }
        
        public override bool TransformInput(bool input)
        {
            return !input;
        }
    }
}