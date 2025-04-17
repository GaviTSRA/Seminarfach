using UnityEngine;
using UnityEditor;
using System.Globalization;

[CustomPropertyDrawer(typeof(Vector3d))]
public class Vector3dDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        EditorGUIUtility.labelWidth = 14f;

        float fieldWidth = (position.width - 4f) / 3f;

        SerializedProperty xProp = property.FindPropertyRelative("x");
        SerializedProperty yProp = property.FindPropertyRelative("y");
        SerializedProperty zProp = property.FindPropertyRelative("z");

        Rect xRect = new(position.x, position.y, fieldWidth, position.height);
        Rect yRect = new(position.x + fieldWidth + 2f, position.y, fieldWidth, position.height);
        Rect zRect = new(position.x + 2f * (fieldWidth + 2f), position.y, fieldWidth, position.height);

        xProp.doubleValue = DrawDoubleField(xRect, "X", xProp.doubleValue);
        yProp.doubleValue = DrawDoubleField(yRect, "Y", yProp.doubleValue);
        zProp.doubleValue = DrawDoubleField(zRect, "Z", zProp.doubleValue);

        EditorGUI.EndProperty();
    }

    private double DrawDoubleField(Rect rect, string label, double currentValue)
    {
        string input = EditorGUI.DelayedTextField(rect, new GUIContent(label), currentValue.ToString("G17"));
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }
        return currentValue;
    }
}   