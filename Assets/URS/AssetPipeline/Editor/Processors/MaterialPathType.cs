namespace Daihenka.AssetPipeline.Processors
{
    internal enum MaterialPathType
    {
        SameAsAsset = 0,
        MaterialFolderWithAsset = 1,
        Relative = 2,
        Absolute = 3,
        TargetFolder = 4
    }
}