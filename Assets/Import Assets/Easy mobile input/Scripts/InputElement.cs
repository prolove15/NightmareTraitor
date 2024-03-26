using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyMobileInput
{
    public abstract class BaseInputElement : MonoBehaviour
    {
        public abstract IList InputProcessors { get; }

        public abstract BaseInputProcessor TryAddProcessor(Type type);
        public abstract T TryAddProcessor<T>() where T : BaseInputProcessor;
    }

    [System.Serializable]
    public struct SerializedInputProcessor
    {
        public string Type;
        public string Data;

        public SerializedInputProcessor(string type, string data)
        {
            Type = type;
            Data = data;
        }
    }

    [DefaultExecutionOrder(-10000)]
    public abstract class InputElement<T> : BaseInputElement, ISerializationCallbackReceiver where T : struct, IEquatable<T>
    {
        public delegate void OnInputChangedDelegate(T oldValue, T newValue);
      
        public event OnInputChangedDelegate OnInputChanged;

        [SerializeField]
        [HideInInspector]
        private List<BaseInputProcessor> processors = new List<BaseInputProcessor>();

        public override IList InputProcessors
        {
            get
            {
                return Processors;
            }
        }

        public List<BaseInputProcessor> Processors
        {
            get
            {
                return processors;
            }
        }

        protected abstract T CurrentRawValue
        {
            get;
            set;
        }

        public T CurrentProcessedValue
        {
            get;
            private set;
        }

        public override BaseInputProcessor TryAddProcessor(Type type)
        {
            if (!typeof(InputProcessor<T>).IsAssignableFrom(type))
                return null;

            var processor = gameObject.AddComponent(type) as InputProcessor<T>;
            processor.hideFlags = HideFlags.HideInInspector;
            processors.Add(processor);
            return processor;
        }

        public override T TryAddProcessor<T>()
        {
            return TryAddProcessor(typeof(T)) as T;
        }

        protected virtual void Awake()
        {
            UpdateDefaultValue(default(T));
        }

        protected void UpdateInput(T input)
        {
            if (processors.Count > 0)
            {
                (processors[0] as InputProcessor<T>).FeedInput(input);

                if (processors[0] is IUpdatableInputProcessor)
                    (processors[0] as IUpdatableInputProcessor).Update();

                for (var index = 0; index < processors.Count - 1; index++)
                {
                    var previous = processors[index] as InputProcessor<T>;
                    var current = processors[index + 1] as InputProcessor<T>;
                    current.FeedInput(previous.CurrentOutput);
                    if (current is IUpdatableInputProcessor)
                        (current as IUpdatableInputProcessor).Update();
                }

                var currentValue = (processors[processors.Count - 1] as InputProcessor<T>).CurrentOutput;
                if (CurrentProcessedValue.Equals(currentValue))
                    return;

                var previousValue = CurrentProcessedValue;
                CurrentProcessedValue = currentValue;

                if (OnInputChanged != null)
                    OnInputChanged(previousValue, currentValue);
            }
            else
            {
                var previousValue = CurrentProcessedValue;
                CurrentProcessedValue = input;

                if (!previousValue.Equals(input) && OnInputChanged != null)
                    OnInputChanged(previousValue, CurrentProcessedValue);
            }
        }

        private void UpdateDefaultValue(T input)
        {
            var currentInputValue = input;
            for (var index = 0; index < processors.Count; index++)
                currentInputValue = (processors[index] as InputProcessor<T>).TransformInput(currentInputValue);

            CurrentProcessedValue = currentInputValue;
        }

        protected virtual void Update()
        {
            UpdateInput(CurrentRawValue);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach (var processor in processors)
            {
                if (processor is BaseInputProcessor)
                    (processor as BaseInputProcessor).CheckGUID();
            }
        }
    }
}