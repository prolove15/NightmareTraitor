using UnityEngine;
using EasyMobileInput;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace EasyMobileInput
{
    [CustomEditor(typeof(BaseInputProcessor), true)]
    public class InputProcessorEditor : Editor
    {
        private static bool shouldDraw = false;

        private static Dictionary<BaseInputProcessor, float> drawTime = new Dictionary<BaseInputProcessor, float>();
        
        public override void OnInspectorGUI()
        {
        }

        public virtual void OnInspectorGUI(Rect rect)
        {
            var attributes = (target as BaseInputProcessor).GetType().GetCustomAttributes(typeof(DecorativeNameAttribute), true);
            var attribute = attributes.Length > 0 ? attributes[0] as DecorativeNameAttribute : null;
            var labelPosition = rect;
            labelPosition.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(labelPosition, attribute == null ? target.GetType().Name : attribute.Name, EditorStyles.boldLabel);

            rect.y += EditorGUIUtility.singleLineHeight;
            var valueSize = EditorGUIUtility.singleLineHeight;
            var valueRect = rect;
            valueRect.height = valueSize;
            valueRect.width = valueSize;
            valueRect.x = rect.x + rect.width - valueSize;
            valueRect.y -= EditorGUIUtility.singleLineHeight;

            rect.width -= EditorGUIUtility.singleLineHeight * 0.5f;
            rect.x += EditorGUIUtility.singleLineHeight * 0.5f;

            DrawCurrentValue(valueRect);

            Space(ref rect);

            var root = serializedObject.GetIterator();
            root.NextVisible(true);
            var drawnCount = 0;
            while (root.NextVisible(false))
            {
                drawnCount++;
                var height = EditorGUI.GetPropertyHeight(root);

                rect.height = height;

                EditorGUI.PropertyField(rect, root, true);

                rect.y += height;
                Space(ref rect);
            }

            if (drawnCount == 0)
                EditorGUI.LabelField(rect, "No parameters");
        }

        private void Space(ref Rect rect)
        {
            rect.y += EditorGUIUtility.standardVerticalSpacing;
        }

        protected void DrawCurrentValue(Rect rect)
        {
            var type = target.GetType();
            var property = type.GetProperty("CurrentOutput", BindingFlags.Public | BindingFlags.Instance);

            if (property.PropertyType == typeof(Vector3))
                DrawCurrentValue(rect, (Vector3)property.GetValue(target));
            else if (property.PropertyType == typeof(bool))
                DrawCurrentValue(rect, (bool)property.GetValue(target));
        }

        protected void DrawCurrentValue(Rect position, Vector3 value)
        {
            var rect = position;
            EditorGUI.DrawRect(rect, Color.black);

            var center = rect;
            center.width = 2;
            center.height = 2;
            center.center = rect.center;

            EditorGUI.DrawRect(center, Color.gray);

            var valueRect = center;
            var scale = Mathf.Lerp(1.0f, 3.0f, (value.z + 1.0f) * 0.5f);
            valueRect.width = scale;
            valueRect.height = scale;

            value.y *= -1.0f;

            var xy = (Vector2)value;

            valueRect.center = rect.size * xy * 0.45f + center.center;

            var color = value.z > 0.0f ? Color.Lerp(Color.green, Color.yellow, value.z) : Color.Lerp(Color.green, Color.yellow, -value.z);

            EditorGUI.DrawRect(valueRect, color);
        }

        protected void DrawCurrentValue(Rect position, bool value)
        {
            EditorGUI.DrawRect(position, value ? Color.green : Color.black);
        }

        public virtual float GetHeight()
        {
            serializedObject.Update();
            var height = 0.0f;
            var root = serializedObject.GetIterator();
            root.NextVisible(true);
            while (root.NextVisible(false))
                height += EditorGUI.GetPropertyHeight(root);

            return Mathf.Max((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2.0f + EditorGUIUtility.standardVerticalSpacing, height + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.standardVerticalSpacing);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }
    }
}
