using System;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>An (entity, list DTO, view DTO) triple — the unit the whole mapping layer is keyed by.</summary>
public sealed record MappingTriple(Type Entity, Type ListDto, Type ViewDto)
{
    public override string ToString() => $"{Entity.Name} / {ListDto.Name} / {ViewDto.Name}";
}

/// <summary>Where a triple was found, and through what it is reached at runtime.</summary>
public sealed record TripleSite(MappingTriple Triple, Type? RepositoryType, string Origin)
{
    public override string ToString() => $"{Triple}  [{Origin}]";
}

/// <summary>
/// How a triple's non-AutoMapper mapping is actually resolved. This is not bookkeeping — it is the difference
/// between a parity run that measures something and one that compares AutoMapper against itself.
/// </summary>
public enum ArmKind
{
    /// <summary>The repository overrides the mapping methods directly (ProductRepository).</summary>
    RepositoryOverride,

    /// <summary>An explicit mapper is configured — UseMapper / UseGeneratedMapper — and it is not AutoMapper-backed.</summary>
    Configured,

    /// <summary>Resolved from DI (ShiftTagMapper is registered this way).</summary>
    DiRegistered,

    /// <summary>
    /// A generated mapper EXISTS in the registry but the repository did not resolve it — which before Step D1
    /// meant the triple silently ran on AutoMapper instead (gap B-1). A real finding, reported not failed.
    /// </summary>
    RegistryOnly,

    /// <summary>
    /// No mapper of any kind. Not a per-request surprise: with no fallback left,
    /// <c>ShiftEntityMapperValidation</c> lists every uncovered triple and fails STARTUP.
    /// </summary>
    None,
}
