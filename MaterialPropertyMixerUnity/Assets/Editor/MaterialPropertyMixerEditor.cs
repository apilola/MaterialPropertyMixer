using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace PropertyMixer
{
    [CustomEditor(typeof(MaterialPropertyMixer)), CanEditMultipleObjects()]
    public class MaterialPropertyMixerEditor : Editor
    {
        MaterialPropertyMixer[] m_Mixers;

        private void OnEnable()
        {
            m_Mixers = Array.ConvertAll(targets, x => x as MaterialPropertyMixer); 
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if(GUILayout.Button("Reinitialize"))
            {
                foreach (var mixer in m_Mixers)
                {
                    mixer.Reintitialize();
                }

            }

            serializedObject.ApplyModifiedProperties();            
        }
    }
}