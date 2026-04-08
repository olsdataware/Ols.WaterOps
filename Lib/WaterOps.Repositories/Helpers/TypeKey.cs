namespace WaterOps.Repositories.Helpers;

/// <summary>
/// Provides a stable, assembly-qualified type key for use as both
/// a Cosmos DB partition key and a LiteDB collection name.
/// </summary>
public static class TypeKey
{
    /// <summary>
    /// Returns a unique type key in the format <c>AssemblyName_FullTypeName</c>.
    /// All dots are replaced with underscores for LiteDB collection name compatibility.
    /// Safe for both Cosmos DB partition keys and LiteDB collection names.
    /// </summary>
    /// <example>WaterOS_Calibrations_WaterOS_Calibrations_Models_Config</example>
    public static string Of<T>() =>
        $"{typeof(T).Assembly.GetName().Name}_{typeof(T).FullName ?? typeof(T).Name}"
            .Replace('.', '_')
            .Replace('+', '_');
}
