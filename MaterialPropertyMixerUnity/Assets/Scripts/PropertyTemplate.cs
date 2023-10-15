using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization;
using System.Linq;

#if UNTIY_EDITOR
using UnityEditor;
#endif

namespace PropertyMixer
{
    [System.Serializable]
    public class PropertyTemplate
    {
        [SerializeField] string m_Name;
        [SerializeReference] IMixerProperty m_Property = new FloatProperty(0, null);

        public string Name => m_Name;
        public IMixerProperty Property => m_Property;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PropertyTemplate))]
    public class PropertyTemplatePropertyDrawer : PropertyDrawer
    {
        static float lineHeight => EditorGUIUtility.singleLineHeight;
        static float verticalSpacing => EditorGUIUtility.standardVerticalSpacing;
        protected System.Type DefaultType = typeof(FloatProperty);
        protected System.Type BaseType = typeof(IMixerProperty);

        System.Collections.Generic.List<Type> m_Types = new List<Type>();

        bool m_Initialized = false;

        protected virtual void Init()
        {
            if (m_Initialized)
                return;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (BaseType.IsAssignableFrom(type) && !type.IsGenericType && !type.IsAbstract && type.AssemblyQualifiedName != DefaultType.AssemblyQualifiedName)
                    {
                        m_Types.Add(type);
                    }
                }
            }
            m_Initialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            EditorGUI.BeginProperty(position, label, property);
            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            labelRect.height = lineHeight;
            var dropDownRect = position;
            dropDownRect.height = lineHeight;
            dropDownRect.x += EditorGUIUtility.labelWidth + verticalSpacing;
            dropDownRect.width -= EditorGUIUtility.labelWidth + verticalSpacing;


            var mixerPropProperty = property.FindPropertyRelative("m_Property");
            var nameProperty = property.FindPropertyRelative("m_Name");
            var valueProperty = mixerPropProperty.FindPropertyRelative("m_Value");

            bool hasValueProp = (valueProperty != null);

            DrawDropdpownContent(dropDownRect, nameProperty.stringValue, mixerPropProperty);
            DrawNameProperty(labelRect, nameProperty, mixerPropProperty);


            var valueRect = position;
            valueRect.y += lineHeight + verticalSpacing;
            valueRect.height = (hasValueProp) ? EditorGUI.GetPropertyHeight(valueProperty) : lineHeight;

            if (hasValueProp)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(valueRect, valueProperty, true);
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    var mixerProp = mixerPropProperty.managedReferenceValue as IMixerProperty;
                    if (mixerProp != null)
                    {
                        mixerProp.ForceMaterialUpdate();
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(valueRect, "Could not locate property");
            }
            //EditorGUI.TextField();
            //EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init();

            var mixerPropProperty = property.FindPropertyRelative("m_Property");
            var valueProperty = mixerPropProperty.FindPropertyRelative("m_Value");
            /*
            var mixerPropProperty = property.FindPropertyRelative("m_Property");
            if (mixerPropProperty.managedReferenceValue == null)
            {

                Debug.Log("Here3");
                FillProperty(property, DefaultType);
            }

            */
            //var valueProperty = mixerPropProperty.FindPropertyRelative("m_Value");
            return lineHeight + verticalSpacing + EditorGUI.GetPropertyHeight(valueProperty);
        }

        private void DrawNameProperty(Rect position, SerializedProperty nameProperty, SerializedProperty mixerPropProperty)
        {
            EditorGUI.BeginChangeCheck();
            var newName = EditorGUI.DelayedTextField(position, nameProperty.stringValue);
            if(EditorGUI.EndChangeCheck())
            {
                nameProperty.stringValue = newName;
                var currentVal = mixerPropProperty.managedReferenceValue;
                var currentType = (currentVal != null) ? currentVal.GetType() : DefaultType;
                FillProperty(nameProperty.stringValue, mixerPropProperty, currentType);
            }
        }

        public void DrawDropdpownContent(Rect position, string name, SerializedProperty property)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused);

            var oCol = GUI.contentColor;

            if (property.managedReferenceValue == null)
            {
                FillProperty(name, property, DefaultType);
            }

            var currentObject = property.managedReferenceValue;
            var currentSelectorType = currentObject.GetType();
            var currentContent = GetGUIContentForType(currentSelectorType);

            var rect = position;

            if (EditorGUI.DropdownButton(rect, currentContent, FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                for (var i = 0; i < m_Types.Count; i++)
                {
                    var type = m_Types[i];
                    var content = GetGUIContentForType(type);
                    bool isOn = currentContent.text == content.text;
                    menu.AddItem(content, currentContent.text == content.text, () =>
                    {
                        if (!isOn)
                        {
                            FillProperty(name, property, type);
                        }
                    });
                }

                menu.AddSeparator(null);

                var defaultContent = GetGUIContentForType(DefaultType);
                bool defaultIsOn = defaultContent.text == currentContent.text;
                menu.AddItem(GetGUIContentForType(DefaultType), defaultIsOn, () =>
                {
                    if (!defaultIsOn)
                    {
                        FillProperty(name, property, DefaultType);
                    }
                });
                menu.DropDown(rect);
            }

            GUI.contentColor = oCol;

            GUIContent GetGUIContentForType(System.Type type)
            {
                if (type.AssemblyQualifiedName == DefaultType.AssemblyQualifiedName)
                    return new GUIContent($"{type.Name} (Default)");
                return new GUIContent($"{type.Name}");
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void FillProperty(string name, SerializedProperty property, System.Type type)
        {
            
            var mixer = property.serializedObject.targetObject as MaterialPropertyMixer;
            IMixerProperty mixerProperty = (mixer != null) ? mixer.GetProperty(name, type): null;
            //property.managedReferenceValue = null;
            var original = property.managedReferenceValue;
            //SerializationUtility.HasManagedReferencesWithMissingTypes()
            if (original != null && mixerProperty != null)
            {
                CopyFields(original, mixerProperty);
            }
            property.managedReferenceValue = mixerProperty;
            property.serializedObject.ApplyModifiedProperties();
        }

        //Unclear as to why this stopped working.
        private static void CopyFields(object source, object destination)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();
            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts
            CopyFieldsInternal(BindingFlags.Instance | BindingFlags.NonPublic);
            CopyFieldsInternal(BindingFlags.Instance | BindingFlags.Public);
            void CopyFieldsInternal(BindingFlags flags)
            {
                FieldInfo[] srcFields = typeSrc.GetFields(flags);
                foreach (FieldInfo srcField in srcFields)
                {
                    if (srcField.Name.Contains("<ID>"))
                    {
                        continue;
                    }
                    FieldInfo targetField = typeDest.GetField(srcField.Name, flags);

                    if (targetField == null)
                    {
                        continue;
                    }

                    if (targetField.FieldType != srcField.FieldType)
                    {
                        continue;
                    }
                    targetField.SetValue(destination, srcField.GetValue(source));
                }
            }
        }
    }
#endif
}