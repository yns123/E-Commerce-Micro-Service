namespace Common.Correlation;

/// <summary>
/// AsyncLocal tabanlı: bir HTTP isteğinin ya da tüketilen bir mesajın işlenmesi boyunca
/// aynı async akışta (nested await'ler dahil) tutarlı kalır, eşzamanlı isteklerle karışmaz.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
