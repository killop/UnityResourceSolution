using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URS 
{
    public static class URSShaderVariantConstant 
    {
        public const string SHADER_VARIANT_SAVE_PATH = "Assets/GameResources/ShaderVarians/ShaderVariantCollection.shadervariants";
        public const string BLACK_SHADER_VARIANT_STRIPPER_SAVE_PATH = "Assets/GameResources/ShaderVarians/BlackShaderVariantCollection.shadervariants";

        public const int WARM_ONE_SHADER_VARIANT_COUNT = 7;
        public const string WARM_SHADER_FILE_FORMAT = "Assets/GameResources/ShaderVarians/WarmShaderVariantCollection_{0}.shadervariants";
        public const string WARM_SHADER_JSON_FILE = "Assets/GameResources/ShaderVarians/WarmShaderVariantCollections.json";

        public const string BUILD_IN_WARM_SHADER_SAVE_PATH_FORMAT = "Assets/GameResources/ShaderVarians/BuildIn/BuildInWarmShaderVariantCollection_{0}.shadervariants";
    }


    public class WarmShaderVariants
    {
        [SerializeField]
        public string[] paths;
    }
}

