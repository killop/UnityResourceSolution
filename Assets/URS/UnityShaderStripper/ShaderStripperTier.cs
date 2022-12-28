#if UNITY_EDITOR && UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace Sigtrap.Editors.ShaderStripper {
    /// <summary>
	/// Strips shaders by shader tier.
	/// </summary>
	[CreateAssetMenu(menuName="Sigtrap/Shader Stripper Tier")]
    public class ShaderStripperTier : ShaderStripperBase, ISerializationCallbackReceiver {
        [System.Serializable]
        struct TierData {
            public ShaderCompilerPlatform platform;
            [Tooltip("If true, strip Tier1 (Low) variants on this platform.")]
            public bool stripTier1;
            [Tooltip("If true, strip Tier2 (Med) variants on this platform.")]
            public bool stripTier2;
            [Tooltip("If true, strip Tier3 (High) variants on this platform.")]
            public bool stripTier3;
        }

        [SerializeField][Tooltip("Tiers to strip per-platform.")]
        TierData[] _tiers;

        Dictionary<ShaderCompilerPlatform, TierData> _data;
        List<ShaderCompilerPlatform> _tempCheckDupes = new List<ShaderCompilerPlatform>();

        public void OnAfterDeserialize(){
            _data = new Dictionary<ShaderCompilerPlatform, TierData>();
            foreach (var t in _tiers){
                _data[t.platform] = t;
            }
        }
        public void OnBeforeSerialize(){}

        void OnValidate(){
            _tempCheckDupes.Clear();
            foreach (var t in _tiers){
                if (_tempCheckDupes.Contains(t.platform)){
                    Debug.LogWarning("Cannot have duplicate platforms in ShaderStripperTier settings", this);
                } else {
                    _tempCheckDupes.Add(t.platform);
                }
            }
        }

        protected override bool _checkPass {get {return false;}}
        protected override bool _checkVariants {get {return true;}}
        protected override bool _checkShader {get {return false;}}

        protected override bool MatchVariant(ShaderCompilerData variantData){
            TierData t;
            if (_data.TryGetValue(variantData.shaderCompilerPlatform, out t)){
                switch(variantData.graphicsTier){
                    case GraphicsTier.Tier1:
                        return t.stripTier1;
                    case GraphicsTier.Tier2:
                        return t.stripTier2;
                    case GraphicsTier.Tier3:
                        return t.stripTier3;
                }
            }
            return false;
        }
    }
}
#endif