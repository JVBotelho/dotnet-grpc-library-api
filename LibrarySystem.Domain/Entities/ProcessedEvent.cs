namespace LibrarySystem.Domain.Entities;

public class ProcessedEvent
{
    private ProcessedEvent() { }

    public ProcessedEvent(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(idempotencyKey));
            
        IdempotencyKey = idempotencyKey;
        ProcessedAt = DateTime.UtcNow;
    }

    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }
}
