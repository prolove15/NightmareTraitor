using System;
using System.Collections;
using UnityEngine;

namespace EasyMobileInput
{
    public interface IInputProcessor<T> where T : struct, IEquatable<T>
    {
        T CurrentOutput
        {
            get;
        }

        void FeedInput(T input);
        T TransformInput(T input);
    }

    public class DecorativeNameAttribute : Attribute
    {
        public readonly string Name;
        public DecorativeNameAttribute(string name)
        {
            Name = name;
        }
    }

    public abstract class BaseInputProcessor : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private string guid;

        public string GUID
        {
            get
            {
                return guid;
            }
        }

        public void CheckGUID()
        {
            if (!string.IsNullOrEmpty(guid))
                return;

            guid = Guid.NewGuid().ToString();
        }

        public virtual void OnValidate()
        {
            foreach (var processors in GetComponentsInChildren<BaseInputProcessor>())
                processors.hideFlags = HideFlags.HideInInspector;

            CheckGUID();
        }
    }

    public interface IUpdatableInputProcessor
    {
        void Update();
    }

    public abstract class InputProcessor<T> : BaseInputProcessor, IInputProcessor<T> where T : struct, IEquatable<T> 
    {
        public abstract T CurrentOutput
        {
            get;
            protected set;
        }

        public virtual void FeedInput(T input)
        {
            CurrentOutput = TransformInput(input);
        }

        public virtual T TransformInput(T input)
        {
            return input;
        }
    }
}