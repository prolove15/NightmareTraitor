using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EasyMobileInput
{
    [System.Serializable]
    public class BooleanEvent : UnityEvent<bool>
    {

    }

    [DecorativeName("Event")]
    public class EventBooleanInputProcessor : EventInputProcessor<bool, BooleanEvent>
    {
    }
}