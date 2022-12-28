using System;

namespace Daihenka.AssetPipeline.Import
{
    public enum ImportAssetType
    {
        Textures = 0,
        Models = 1,
        Audio = 2,
        Videos = 3,
        Fonts = 4,
        Animations = 5,
        Materials = 6,
        Prefabs = 7,
        SpriteAtlases = 8,
        Other = 9
    }

    [Flags]
    public enum ImportAssetTypeFlag
    {
        Textures = 1 << 0,
        Models = 1 << 1,
        Audio = 1 << 2,
        Videos = 1 << 3,
        Fonts = 1 << 4,
        Animations = 1 << 5,
        Materials = 1 << 6,
        Prefabs = 1 << 7,
        SpriteAtlases = 1 << 8,
        Other = 1 << 9,
        All = Textures | Models | Audio | Animations | Materials | Prefabs | SpriteAtlases | Fonts | Videos | Other
    }
}