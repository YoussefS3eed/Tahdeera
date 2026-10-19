namespace Tahdeera.Domain.Enums;

public enum OutboxMessageStatus
{
    Pending = 1,
    PublishedToBroker = 2,
    Processing = 3,
    Sent = 4,
    Delivered = 5,
    FailedTransient = 6,
    FailedPermanent = 7,
    Cancelled = 8
}
