using UnityEngine;
using UnityEditor;

namespace PropertyMixer
{
    [CustomPropertyDrawer(typeof(SelectFromMaterial))]
    public class FromShaderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Material)
            {
                EditorGUI.BeginChangeCheck();
                var mat = (Material) EditorGUI.ObjectField(position, label, null, typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if(mat != null)
                    {
                        property.objectReferenceValue = mat.shader;
                    }
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}