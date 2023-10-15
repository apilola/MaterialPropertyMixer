using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PropertyMixer
{
    [System.Serializable]
    public abstract class MixerPropertyGeneric<IDType, ValueType> : IMixerProperty where IDType : IEquatable<IDType>
    {
        public IDType ID { get; internal set; }

        [SerializeField] private ValueType m_Value;

        public MaterialPropertyMixer Mixer { get; internal set; }

        internal MixerPropertyGeneric(IDType id, MaterialPropertyMixer mixer)
        {
            this.ID = id;
            Mixer = mixer;

            if (Mixer)
            {
                var refMat = Mixer.GetReferenceMaterial();
                if (refMat != null)
                {
                    m_Value = GetMaterialValue(refMat);
                }
            }
        }

        void IMixerProperty.SetMixer(MaterialPropertyMixer mixer)
        {
            if(Mixer == null)
            {
                Mixer = mixer;
            }
        }

        public abstract void SetMaterialValue(Material mat);
        public abstract void SetPropertyBlockValue(MaterialPropertyBlock properties);

        public ValueType Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!Equals(value))
                {
                    m_Value = value;

                    if(Mixer != null)
                    {
#if UNITY_EDITOR
                        if(!Application.isPlaying) 
                        {
                            SetPropertyBlockValue(Mixer.m_EditorProperties);

                            foreach (Renderer renderer in Mixer.Renderers)
                            {
                                renderer.SetPropertyBlock(Mixer.m_EditorProperties);
                            }
                            return;                        
                        }
#endif
                        foreach (Material material in Mixer.Materials)
                        {
                            SetMaterialValue(material);
                        }
                    }
                }
            }
        }

        public abstract bool Equals(ValueType value);

        public abstract ValueType GetMaterialValue(Material mat);
    }

    [System.Serializable]
    public abstract class StandardMixerProperty<ValueType> : MixerPropertyGeneric<int, ValueType>
    {
        internal StandardMixerProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }
    }

    [System.Serializable]
    public abstract class KeywordMixerProperty<ValueType> : MixerPropertyGeneric<LocalKeyword, ValueType>
    {
        internal KeywordMixerProperty(LocalKeyword id, MaterialPropertyMixer mixer) : base(id, mixer) { }
    }

    [System.Serializable]
    public abstract class BasicMixerProperty<ValueType> : StandardMixerProperty<ValueType> where ValueType : IEquatable<ValueType>
    {
        internal BasicMixerProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }
        public override bool Equals(ValueType value)
        {
            return Value.Equals(value);
        }
    }

    [System.Serializable]
    public class IntProperty : BasicMixerProperty<int>
    {
        internal IntProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetInt(ID, Value);
        }

        public override int GetMaterialValue(Material mat)
        {
            return (mat.HasInt(ID)) ? mat.GetInt(ID) : default;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {

            properties.SetInt(ID, Value);
        }
    }

    [System.Serializable]
    public class FloatProperty : BasicMixerProperty<float>
    {
        internal FloatProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetFloat(ID, Value);
        }

        public override float GetMaterialValue(Material mat)
        {
            return mat.HasFloat(ID) ? mat.GetFloat(ID) : default;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetFloat(ID, Value);
        }
    }

    [System.Serializable]
    public class ColorProperty : BasicMixerProperty<Color>
    {
        internal ColorProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override Color GetMaterialValue(Material mat)
        {
            return mat.HasColor(ID) ? mat.GetColor(ID) : default;
        }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetColor(ID, Value);
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetColor(ID, Value);
        }
    }

    [System.Serializable]
    public class Vector2Property : BasicMixerProperty<Vector2>
    {
        internal Vector2Property(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetVector(ID, Value);
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetVector(ID, Value);
        }

        public override Vector2 GetMaterialValue(Material mat)
        {
            return mat.HasVector(ID) ? mat.GetVector(ID) : default;
        }
    }

    [System.Serializable]
    public class Vector3Property : BasicMixerProperty<Vector3>
    {
        internal Vector3Property(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetVector(ID, Value);
        }

        public override Vector3 GetMaterialValue(Material mat)
        {
            return mat.HasVector(ID) ? mat.GetVector(ID) : default;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetVector(ID, Value);
        }
    }

    [System.Serializable]
    public class Vector4Property : BasicMixerProperty<Vector4>
    {
        internal Vector4Property(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetVector(ID, Value);
        }

        public override Vector4 GetMaterialValue(Material mat)
        {
            return mat.HasVector(ID) ? mat.GetVector(ID) : default;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetVector(ID, Value);
        }
    }

    [System.Serializable]
    public class TextureProperty : StandardMixerProperty<Texture>
    {
        internal TextureProperty(int id, MaterialPropertyMixer mixer) : base(id, mixer) { }

        public override void SetMaterialValue(Material mat)
        {
            mat.SetTexture(ID, Value);
        }

        public override Texture GetMaterialValue(Material mat)
        {
            return mat.HasTexture(ID) ? mat.GetTexture(ID)  : default;
        }

        public override bool Equals(Texture value)
        {
            return Value == value;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            properties.SetTexture(ID, Value);
        }
    }

    [System.Serializable]
    public class KeywordProperty : MixerPropertyGeneric<LocalKeyword, bool>
    {
        internal KeywordProperty(LocalKeyword keyword, MaterialPropertyMixer mixer) : base(keyword, mixer) { }
        public override void SetMaterialValue(Material mat)
        {
            mat.SetKeyword(ID, Value);
        }
        
        public override bool GetMaterialValue(Material mat)
        {
            return ID.isValid ? mat.IsKeywordEnabled(ID) : default;
        }

        public override bool Equals(bool value)
        {
            return Value == value;
        }

        public override void SetPropertyBlockValue(MaterialPropertyBlock properties)
        {
            //For Edit mode purposes we will just ignore this as there is no way to turn off a keyword via material property block
        }
    }

}