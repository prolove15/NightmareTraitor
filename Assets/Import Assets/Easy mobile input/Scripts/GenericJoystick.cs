using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyMobileInput
{
    public abstract class GenericJoystick<T> : InputElement<T> where T : struct, IEquatable<T>
    {
    }
}