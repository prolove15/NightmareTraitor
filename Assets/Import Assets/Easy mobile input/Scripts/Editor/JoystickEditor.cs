using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMobileInput
{
    [CustomEditor(typeof(Joystick))]
    public class JoystickEditor : InputElementEditor
    {
        protected override void OnBeforeBasicProperties()
        {
            var go = (target as Component).gameObject;
            if (go.GetComponentsInParent<Graphic>(true).Length == 0)
                EditorGUILayout.HelpBox("Joystick requires some graphics component to be attached or RaycastZone in order to work", MessageType.Warning);
        }

        protected override void OnAfterBasicProperties()
        {
            var fetchStage = (target as Joystick).ValueFetchStage;
            var processors = (target as Joystick).Processors;

            if (processors == null)
                processors = new List<BaseInputProcessor>();

            var currentIndex = processors.FindIndex(x => x as BaseInputProcessor == fetchStage);

            if (EditorGUILayout.DropdownButton(new GUIContent(fetchStage == null ? "Fetch stage: Raw" : string.Format("Fetch stage: [{0}] {1}", currentIndex, GetDecorativeName(fetchStage.GetType()))), FocusType.Keyboard))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Raw"), fetchStage == null, () =>
                    {
                        (target as Joystick).ValueFetchStage = null;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    });

                var index = 0;
                foreach (var processor in processors)
                {
                    if (!(processor is BaseInputProcessor))
                        continue;

                    var cachedProcessor = processor;
                    menu.AddItem(new GUIContent(string.Format("[{0}] {1}", index++, GetDecorativeName(cachedProcessor.GetType()))), fetchStage == processor as BaseInputProcessor, () =>
                        {
                            (target as Joystick).ValueFetchStage = cachedProcessor as BaseInputProcessor;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(target);
                        });
                }

                menu.ShowAsContext();
            }

            base.OnAfterBasicProperties();

            serializedObject.ApplyModifiedProperties();
        }
    }
}