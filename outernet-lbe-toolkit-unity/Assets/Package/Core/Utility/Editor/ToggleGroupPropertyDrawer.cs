using UnityEngine;
using UnityEditor;

namespace Outernet.LBEToolkit
{
    [CustomPropertyDrawer(typeof(ToggleGroupAttribute))]
    public class ToggleGroupPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var toggleAttribute = (ToggleGroupAttribute)attribute;
            bool show = Resolve(property, toggleAttribute);

            if (!show && !toggleAttribute.disable)
                return;

            bool wasEnabled = GUI.enabled;

            if (toggleAttribute.disable)
                GUI.enabled = show && wasEnabled;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var toggleAttribute = (ToggleGroupAttribute)attribute;
            bool show = toggleAttribute.disable || Resolve(property, toggleAttribute);

            return show ? EditorGUI.GetPropertyHeight(property, label, true) : -EditorGUIUtility.standardVerticalSpacing;
        }

        private static bool Resolve(SerializedProperty property, ToggleGroupAttribute toggleAttribute)
        {
            var toggleProperty = property.serializedObject.FindProperty(toggleAttribute.toggleProperty);
            bool show = toggleProperty.propertyType == SerializedPropertyType.Enum
                ? toggleProperty.enumValueIndex == toggleAttribute.enumValue
                : toggleProperty.boolValue;

            return toggleAttribute.invert ? !show : show;
        }
    }
}