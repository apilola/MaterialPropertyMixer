using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace PropertyMixer
{
    
    [ExecuteAlways]
    public class MaterialPropertyMixer : MonoBehaviour, ISerializationCallbackReceiver
    {
        public struct MaterialData
        {
            public MaterialData(Material material)
            {
                SharedMaterial = material;
                RuntimeCopy = new Material(material);
                RuntimeCopy.name = material.name + " (Copy)";
            }
            public readonly Material RuntimeCopy;
            public readonly Material SharedMaterial;
        }

        private const string NAME_WARNINGFORMAT = nameof(MaterialPropertyMixer) + ":: {0} Failed to retrieve property with Name \"{1}\"";
        private const string ID_WARNINGFORMAT = nameof(MaterialPropertyMixer) + ":: {0} Failed to retrieve property with ID \"{1}\"";
        private const string KEYWORD_WARNINGFORMAT = nameof(MaterialPropertyMixer) + ":: {0} Failed to retrieve property with Keyword \"{1}\"";
        private const string KEYWORD_MISSING_WARNINGFORMAT = nameof(MaterialPropertyMixer) + ":: {0} The reference shader does not have the requested keyword \"{1}\"";

        private Material m_Material;

        internal Material GetReferenceMaterial()
        {
            if ((m_Material == null || m_Material.shader != m_Shader) && m_Shader != null)
            {
                if(m_Material != null)
                {
                    DestroyImmediate(m_Material);
                }

                if (m_Materials.Count != 0 && m_Materials[0])
                {
                    m_Material = m_Materials[0];
                }
                else
                {
                    m_Material = new Material(m_Shader);
                }
            }

            return m_Material;
        }

        [SerializeField, SelectFromMaterial] private Shader m_Shader;

        [SerializeField] private List<PropertyTemplate> m_DefaultProperties = new();
        

        internal List<Material> m_Materials = new List<Material>();
        private MaterialData data;

        private List<MaterialData> m_SharedMaterialData = new();

        List<Renderer> m_Renderers = new List<Renderer>();

        private bool m_IsSetUp;

        [NonSerialized]
        private List<FloatProperty> m_Floats = new();

        [NonSerialized]
        private List<IntProperty> m_Ints = new();

        [NonSerialized]
        private List<Vector2Property> m_Vector2s = new();

        [NonSerialized]
        private List<Vector3Property> m_Vector3s = new();

        [NonSerialized]
        private List<Vector4Property> m_Vector4s = new();

        [NonSerialized]
        private List<TextureProperty> m_Textures = new();

        [NonSerialized]
        private List<KeywordProperty> m_Keywords = new();

        [NonSerialized]
        private List<ColorProperty> m_Colors = new();

        internal MaterialPropertyBlock m_EditorProperties;

        public IReadOnlyCollection<Material> Materials
        {
            get
            {                
                return m_Materials.AsReadOnly();
            }
        }

        public IReadOnlyCollection<Renderer> Renderers
        {
            get
            {
                return m_Renderers.AsReadOnly();
            }
        }

        public bool IsSetUp => m_IsSetUp;

        void SetUp()
        {
            if (m_IsSetUp)
            {
                return;
            }

            if(m_Shader == null)
            {
                return;
            }

#if UNITY_EDITOR
            m_EditorProperties = new MaterialPropertyBlock();
#endif
            m_SharedMaterialData.Clear();
            m_Renderers.Clear();

            foreach (Material mat in m_Materials)
            {
                DestroyImmediate(mat);
            }

            m_Materials.Clear();

            m_Materials.Clear();
            GetComponentsInChildren<Renderer>(m_Renderers);
            for (var r = 0; r < m_Renderers.Count; r++)
            {
                var renderer = m_Renderers[r];
#if UNITY_EDITOR
                if(Application.isPlaying)
                {
                    RegisterRendererInternal(renderer);
                }
                else
                {
                    renderer.SetPropertyBlock(m_EditorProperties);
                }
#else
                RegisterRenderer(renderer);
#endif
            }

            m_IsSetUp = true;
        }

        void RegisterRendererInternal(Renderer renderer)
        {
            var sharedMaterials = renderer.sharedMaterials;
            var materials = new UnityEngine.Material[sharedMaterials.Length];

            for (int m = 0; m < sharedMaterials.Length; m++)
            {
                materials[m] = RegisterMaterial(sharedMaterials[m]);
            }
            renderer.materials = materials;
        }

        public Material RegisterMaterial(Material sharedMaterial)
        {
            if (sharedMaterial.shader == m_Shader)
            {
                var data = GetMaterialData(sharedMaterial);
                return data.RuntimeCopy;
            }
            else
            {
                return sharedMaterial;
            }
        }

        public void RegisterRenderer(Renderer renderer)
        {
            RegisterRendererInternal(renderer);

            m_SharedMaterialData.Clear();
            m_Renderers.Add(renderer);
        }

        public void Awake()
        {
            SetUp();
        }

        private void OnValidate()
        {
            SetUp();

            if(m_EditorProperties == null)
            {
                m_EditorProperties = new MaterialPropertyBlock();
            }
            if(!Application.isPlaying)
            {

                foreach (var defaultProp in m_DefaultProperties)
                {
                    if(defaultProp != null && defaultProp.Property != null)
                    {
                        defaultProp.Property.SetPropertyBlockValue(m_EditorProperties);
                    }
                }

                foreach (var renderer in m_Renderers)
                {
                    renderer.SetPropertyBlock(m_EditorProperties);
                }

            }
            else
            {
                foreach(var material in m_Materials)
                {
                    UpdateMaterial(m_Colors, material);
                    UpdateMaterial(m_Ints, material);
                    UpdateMaterial(m_Floats, material);
                    UpdateMaterial(m_Vector2s, material);
                    UpdateMaterial(m_Vector3s, material);
                    UpdateMaterial(m_Vector4s, material);
                    UpdateMaterial(m_Textures, material);
                    UpdateMaterial(m_Keywords, material);
                }
            }
        }

        private void TearDown()
        {
            m_EditorProperties = new MaterialPropertyBlock();

            foreach (Material mat in m_Materials)
            {
                DestroyImmediate(mat);
            }

            m_Materials.Clear();

            if (m_Material != null)
                DestroyImmediate(m_Material);

            m_Renderers.Clear();

            m_IsSetUp = false;
        }

        private void OnDestroy()
        {
            TearDown();
        }

        MaterialData GetMaterialData(Material material)
        {
            if (material == null)
            {
                return default;
            }

            var index = m_SharedMaterialData.FindIndex((smd) => smd.SharedMaterial == material);

            if (index < 0)
            {
                var data = new MaterialData(material);
                m_Materials.Add(data.RuntimeCopy);
                m_SharedMaterialData.Add(data);

                UpdateMaterial(m_Colors, data.RuntimeCopy);
                UpdateMaterial(m_Ints, data.RuntimeCopy);
                UpdateMaterial(m_Floats, data.RuntimeCopy);
                UpdateMaterial(m_Vector2s, data.RuntimeCopy);
                UpdateMaterial(m_Vector3s, data.RuntimeCopy);
                UpdateMaterial(m_Vector4s, data.RuntimeCopy);
                UpdateMaterial(m_Textures, data.RuntimeCopy);
                UpdateMaterial(m_Keywords, data.RuntimeCopy);

                return data;
            }

            return m_SharedMaterialData[index];
        }

        private static void UpdateMaterial<T>(List<T> list, Material mat) where T : IMixerProperty
        {
            foreach (var prop in list)
            {
                prop.SetMaterialValue(mat);
            }
        }


        public ColorProperty GetColor(int id)
        {
            var property = m_Colors.Find((x) => x.ID == id);

            if (property == null)
            {
                property = new ColorProperty(id, this);
                m_Colors.Add(property);
                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasColor(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(ColorProperty), id), this);
                }
            }

            return property;
        }

        public IntProperty GetInt(int id)
        {
            var property = m_Ints.Find(m => m.ID == id);

            if (property == null)
            {
                property = new IntProperty(id, this);
                m_Ints.Add(property);
                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasInt(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(IntProperty), id), this);
                }
            }

            return property;
        }

        public FloatProperty GetFloat(int id)
        {
            var property = m_Floats.Find(m => m.ID == id);

            if (property == null)
            {
                property = new FloatProperty(id, this);
                m_Floats.Add(property);

                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasFloat(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(FloatProperty), id), this);
                }
            }

            return property;
        }

        public Vector2Property GetVector2(int id)
        {
            var property = m_Vector2s.Find(m => m.ID == id);

            if (property == null)
            {

                property = new Vector2Property(id, this);
                m_Vector2s.Add(property);
                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasVector(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(Vector2Property), id), this);
                }
            }

            return property;
        }

        public Vector3Property GetVector3(int id)
        {
            var property = m_Vector3s.Find(m => m.ID == id);

            if (property == null)
            {
                property = new Vector3Property(id, this);
                m_Vector3s.Add(property);

                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasVector(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(Vector3Property), id), this);
                }
            }

            return property;
        }

        public Vector4Property GetVector4(int id)
        {
            var property = m_Vector4s.Find(m => m.ID == id);

            if (property == null)
            {
                property = new Vector4Property(id, this);
                m_Vector4s.Add(property);

                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasVector(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(Vector4Property), id), this);
                }
            }

            return property;
        }

        public TextureProperty GetTexture(int id)
        {
            var property = m_Textures.Find(m => m.ID == id);

            if (property == null)
            {
                property = new TextureProperty(id, this);
                m_Textures.Add(property);


                var refMat = GetReferenceMaterial();
                if (refMat != null && !refMat.HasTexture(id))
                {
                    Debug.LogWarning(string.Format(ID_WARNINGFORMAT, nameof(TextureProperty), id), this);
                }
            }

            return property;
        }

        public KeywordProperty GetKeyword(LocalKeyword keyword)
        {
            var property = m_Keywords.Find(m => m.ID == keyword);

            if (property == null)
            {
                var refMat = GetReferenceMaterial();
                if (refMat != null && refMat.shader.keywordSpace.keywords.Contains(keyword))
                {
                    property = new KeywordProperty(keyword, this);
                    m_Keywords.Add(property);
                }
                else
                {
                    Debug.LogWarning(string.Format(KEYWORD_WARNINGFORMAT, nameof(KeywordProperty), keyword), this);
                }
            }

            return property;
        }

        public ColorProperty GetColor(string name)
        {
            var id = Shader.PropertyToID(name);
            var property = GetColor(id);

            if (!GetReferenceMaterial().HasProperty(id))
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(ColorProperty), name), this);
            }

            return property;
        }

        public IntProperty GetInt(string name)
        {
            var id = Shader.PropertyToID(name);
            var property = GetInt(id);

            if (!GetReferenceMaterial().HasProperty(id))
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(IntProperty), name), this);
            }

            return property;
        }

        public FloatProperty GetFloat(string name)
        {
            var id = Shader.PropertyToID(name);
            var property = GetFloat(id);

            if (!GetReferenceMaterial().HasProperty(id))
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(FloatProperty), name), this);
            }

            return property;
        }

        public Vector2Property GetVector2(string name)
        {
            var property = GetVector2(Shader.PropertyToID(name));

            if (property == null)
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(Vector2Property), name), this);
            }

            return property;
        }

        public Vector3Property GetVector3(string name)
        {
            var property = GetVector3(Shader.PropertyToID(name));

            if (property == null)
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(Vector3Property), name), this);
            }

            return property;
        }

        public Vector4Property GetVector4(string name)
        {
            var property = GetVector4(Shader.PropertyToID(name));

            if (property == null)
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(Vector4Property), name), this);
            }

            return property;
        }

        public TextureProperty GetTexture(string name)
        {
            var property = GetTexture(Shader.PropertyToID(name));

            if (property == null)
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(TextureProperty), name), this);
            }

            return property;
        }

        public KeywordProperty GetKeyword(string name)
        {
            if(!TryLocateKeyword(out LocalKeyword keyword))
            {
                Debug.LogWarning(string.Format(KEYWORD_MISSING_WARNINGFORMAT, nameof(KeywordProperty), name), this);
                return null;
            }

            var property = GetKeyword(keyword);
            if (property == null)
            {
                Debug.LogWarning(string.Format(NAME_WARNINGFORMAT, nameof(KeywordProperty), name), this);
            }

            return property;

            bool TryLocateKeyword(out LocalKeyword localKeyword)
            {
                foreach(var item in m_Material.shader.keywordSpace.keywords)
                {
                    if(item.name == name)
                    {
                        localKeyword = item;
                        return true;
                    }
                }
                localKeyword = default;
                return false;
            }
        }

        internal IMixerProperty GetProperty(string name, Type type)
        {
            if (type == typeof(FloatProperty))
            {
                return GetFloat(name);
            }
            else if (type == typeof(IntProperty))
            {
                return GetInt(name);
            }
            else if (type == typeof(ColorProperty))
            {
                return GetColor(name);
            }
            else if (type == typeof(Vector2Property))
            {
                return GetVector2(name);
            }
            else if (type == typeof(Vector3Property))
            {
                return GetVector3(name);
            }
            else if (type == typeof(Vector4Property))
            {
                return GetVector4(name);
            }
            else if (type == typeof(KeywordProperty))
            {
                return GetKeyword(name);
            }
            else if (type == typeof(TextureProperty))
            {
                return GetTexture(name);
            }

            throw new NotImplementedException($"{type.Name} not implemented");
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            foreach (var propertyTemplate in m_DefaultProperties)
            {
                RegisterItemInternal(propertyTemplate);
            }
        }

        
        public void Reintitialize()
        {
            m_Floats.Clear();
            m_Ints.Clear();
            m_Keywords.Clear();
            m_Vector2s.Clear();
            m_Vector3s.Clear();
            m_Vector4s.Clear();
            m_Colors.Clear();
            m_Textures.Clear();

            TearDown();

            (this as ISerializationCallbackReceiver).OnAfterDeserialize();

            SetUp();


        }

        void RegisterItemInternal(PropertyTemplate propertyTemplate) 
        {
            if (propertyTemplate.Property == null)
                return;

            propertyTemplate.Property.SetMixer(this);
            switch(propertyTemplate.Property)
            {
                case IntProperty property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Ints;
                        var index = list.FindIndex((x)=> x.ID == id);
                        if(index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case FloatProperty property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Floats;
                        var index = list.FindIndex(x => x.ID == id);
                        if(index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case Vector2Property property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Vector2s;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case Vector3Property property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Vector3s;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case Vector4Property property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Vector4s;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case TextureProperty property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Textures;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case ColorProperty property:
                    {
                        var id = Shader.PropertyToID(propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Colors;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if(!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
                case KeywordProperty property:
                    {
                        var id = new LocalKeyword(m_Shader, propertyTemplate.Name);
                        property.ID = id;
                        var list = m_Keywords;
                        var index = list.FindIndex(x => x.ID == id);
                        if (index == -1)
                        {
                            list.Add(property);
                        }
                        else
                        {
                            if (!property.Equals(list[index]))
                            {
                                Debug.Log($"Invalid Collision Occured {property.GetType()}");
                            }
                        }
                        break;
                    }
            }
        }
    }

    public class SelectFromMaterial : PropertyAttribute { }

}