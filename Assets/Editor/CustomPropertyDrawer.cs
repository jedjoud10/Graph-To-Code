using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Inject<>))]
public class IngredientDrawerAoao : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        // Get the "x" field inside the Aoao<T> class
        SerializedProperty xProperty = property.FindPropertyRelative("x");

        // Draw the "x" field directly
        EditorGUI.PropertyField(position, xProperty, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        // Get the height of the "x" field
        SerializedProperty xProperty = property.FindPropertyRelative("x");
        return EditorGUI.GetPropertyHeight(xProperty, true);
    }
}