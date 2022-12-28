using System;

namespace Daihenka.AssetPipeline
{
    public enum StringConvention
    {
        None = 0,
        CamelCase = 1 << 0,
        PascalCase = 1 << 1,
        SnakeCase = 1 << 2,
        UpperSnakeCase = 1 << 3,
        KebabCase = 1 << 4,
        LowerCase = 1 << 5,
        UpperCase = 1 << 6,
    }

    [Flags]
    public enum StringConventionFlags
    {
        None = 0,
        CamelCase = 1 << 0,
        PascalCase = 1 << 1,
        SnakeCase = 1 << 2,
        UpperSnakeCase = 1 << 3,
        KebabCase = 1 << 4,
        LowerCase = 1 << 5,
        UpperCase = 1 << 6,
    }
}