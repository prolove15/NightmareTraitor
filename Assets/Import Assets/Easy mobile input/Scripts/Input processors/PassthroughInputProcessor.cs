using System;

namespace EasyMobileInput
{
    public class PassthroughInputProcessor<T> : InputProcessor<T> where T : struct, IEquatable<T>
    {
        public override T CurrentOutput
        {
            get;
            protected set;
        }

        public override void FeedInput(T input)
        {
            CurrentOutput = input;
        }

        public override T TransformInput(T input)
        {
            return input;
        }
    }
}
