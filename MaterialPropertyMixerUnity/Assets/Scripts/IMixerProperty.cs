using UnityEngine;

namespace PropertyMixer
{
    public interface IMixerProperty
    {
        public MaterialPropertyMixer Mixer { get; }

        public bool IsValid => Mixer != null;

        internal void SetMixer(MaterialPropertyMixer mixer);
        void SetMaterialValue(Material mat);
        void SetPropertyBlockValue(MaterialPropertyBlock properties);

        public void ForceMaterialUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
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