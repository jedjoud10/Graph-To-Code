using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InlineTransform))]
public class IngredientDrawerAoao2 : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        // Start property drawing
        EditorGUI.BeginProperty(position, label, property);

        // Calculate rects
        var lineHeight = EditorGUIUtility.singleLineHeight;
        var padding = EditorGUIUtility.standardVerticalSpacing;

        Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect positionRect = new Rect(position.x, position.y + (lineHeight + padding) * 1, position.width, lineHeight);
        Rect rotationRect = new Rect(position.x, position.y + (lineHeight + padding) * 2, position.width, lineHeight);
        Rect scaleRect = new Rect(position.x, position.y + (lineHeight + padding) * 3, position.width, lineHeight);

        // Draw the label
        EditorGUI.LabelField(labelRect, label);

        SerializedProperty positionProp = property.FindPropertyRelative("position");
        SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
        SerializedProperty scaleProp = property.FindPropertyRelative("scale");

        Vector3 positionValue = positionProp.vector3Value;
        Vector3 rotationValue = rotationProp.quaternionValue.eulerAngles;
        Vector3 scaleValue = scaleProp.vector3Value;

        // Draw position, rotation, and scale
        positionProp.vector3Value = EditorGUI.Vector3Field(positionRect, "Position", positionValue);
        rotationProp.quaternionValue = Quaternion.Euler(EditorGUI.Vector3Field(rotationRect, "Rotation", rotationValue));
        scaleProp.vector3Value = EditorGUI.Vector3Field(scaleRect, "Scale", scaleValue);

        // End property drawing
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 5;
    }
}