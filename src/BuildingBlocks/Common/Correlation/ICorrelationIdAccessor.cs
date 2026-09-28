namespace Common.Correlation;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}
