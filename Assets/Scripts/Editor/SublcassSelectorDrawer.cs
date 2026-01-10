using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute), true)]
public class SubclassSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, "Use SubclassSelector with managed reference types.");
            EditorGUI.EndProperty();
            return;
        }

        Rect foldoutLabelRect = new Rect(position);
        foldoutLabelRect.height = EditorGUIUtility.singleLineHeight;

        Rect popupPosition = EditorGUI.PrefixLabel(foldoutLabelRect, label);

        string typeName = property.managedReferenceValue?.GetType().Name ?? "None";
        if (EditorGUI.DropdownButton(popupPosition, new GUIContent(typeName), FocusType.Keyboard))
        {
            Type baseType = fieldInfo.FieldType;

            CreateAbstractTypeDropdown.Show(popupPosition, baseType, type =>
            {
                property.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
                property.serializedObject.ApplyModifiedProperties();
            });
        }


        if (property.managedReferenceValue != null)
        {
            // Draw the property (Unity handles label + foldout + child properties)
            EditorGUI.PropertyField(foldoutLabelRect, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
