using UnityEngine;
using EasyMobileInput;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditorInternal;
using System.Collections;
using UnityEditor.SceneManagement;

namespace EasyMobileInput
{
    [CustomEditor(typeof(BaseInputElement), true)]
    public class InputElementEditor : Editor
    {
        private ReorderableList list = null;
        
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        protected string GetDecorativeName(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(DecorativeNameAttribute), true);
            var attribute = attributes.Length > 0 ? attributes[0] as DecorativeNameAttribute : null;
            return attribute == null ? type.Name : attribute.Name;
        }

        protected virtual void OnAfterBasicProperties()
        {
            if (list == null)
            {
                list = new ReorderableList(new List<BaseInputProcessor>(), typeof(BaseInputProcessor));

                list.drawHeaderCallback = rect =>
                    {
                        EditorGUI.LabelField(rect, new GUIContent("Input processors"));
                    };

                list.onRemoveCallback = list =>
                    {
                        if (list.index < 0 || list.index >= list.list.Count)
                            return;

                        var component = list.list[list.index] as BaseInputProcessor;
                        DestroyImmediate(component);

                        list.list.RemoveAt(list.index);
                        EditorUtility.SetDirty(target);
                    };

                list.onAddCallback = list =>
                    {
                        var menu = new GenericMenu();

                        var listOfTypes = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                           from assemblyType in domainAssembly.GetTypes()
                                           where assemblyType.IsSubclassOf(typeof(BaseInputProcessor)) && !assemblyType.IsAbstract
                                           select assemblyType).ToArray();

                        var targetType = GetGenericType(target.GetType());
                        foreach (var type in listOfTypes)
                        {
                            var genericType = GetGenericType(type);
                            if (genericType == targetType)
                            {
                                var cachedType = type;
                                menu.AddItem(new GUIContent(GetDecorativeName(type)), false, () =>
                                    {
                                        var instance = (target as BaseInputElement).TryAddProcessor(cachedType);
                                        
                                        var guid = string.Empty;
                                        var id = 0L;
                                        var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot((target as Component).gameObject);

                                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier((target as Component).gameObject, out guid, out id))
                                        {
                                            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                                            AssetDatabase.AddObjectToAsset(instance, assetPath);
                                        }

                                        if (!Application.isPlaying)
                                        {
                                            EditorSceneManager.MarkSceneDirty((target as Component).gameObject.scene);
                                            EditorUtility.SetDirty(instance);
                                            EditorUtility.SetDirty(target);
                                            EditorUtility.SetDirty((target as Component).gameObject);
                                        }

                                        serializedObject.ApplyModifiedProperties();
                                    });
                            }
                        }

                        menu.ShowAsContext();
                    };

                list.elementHeightCallback = index =>
                    {
                        if (index >= list.list.Count)
                            return 0.0f;
                        
                        Editor editor = null;
                        var processor = list.list[index] as BaseInputProcessor;
                        if (processor == null)
                            return 0.0f;

                        Editor.CreateCachedEditor(processor, null, ref editor);

                        editor.serializedObject.Update();
                        return (editor as InputProcessorEditor).GetHeight();
                    };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        Editor editor = null;
                        var processor = list.list[index] as BaseInputProcessor;
                        if (processor == null)
                            return;

                        Editor.CreateCachedEditor(processor, null, ref editor);

                        var fullSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        editor.serializedObject.Update();

                        (editor as InputProcessorEditor).OnInspectorGUI(rect);

                        editor.serializedObject.ApplyModifiedProperties();
                    };
            }

            var element = target as BaseInputElement;

            list.list = element.InputProcessors;

            list.DoLayoutList();
        }

        protected virtual void OnBeforeBasicProperties()
        {

        }
        
        public override void OnInspectorGUI()
        {
            OnBeforeBasicProperties();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            OnAfterBasicProperties();
        }

        private Type GetGenericType(Type type)
        {
            var types = type.GenericTypeArguments;
            if (types.Length == 0)
            {
                if (type.BaseType != null)
                    return GetGenericType(type.BaseType);
                else
                    return null;
            }

            return types[0];
        }
    }
}
