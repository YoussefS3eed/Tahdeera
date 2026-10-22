using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class StudentAccount : BaseEntity
{
    public Guid WorkspaceStudentId { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public Guid? CreatedByUserId { get; private set; }

    // Navigations
    public virtual WorkspaceStudent? WorkspaceStudent { get; private set; }
    public virtual AppUser? CreatedByUser { get; private set; }

    private readonly List<StudentCharge> _charges = new();
    public virtual IReadOnlyCollection<StudentCharge> Charges => _charges.AsReadOnly();

    private readonly List<StudentPayment> _payments = new();
    public virtual IReadOnlyCollection<StudentPayment> Payments => _payments.AsReadOnly();

    private StudentAccount() { } // EF Core

    public static StudentAccount Create(Guid workspaceStudentId, Guid? createdByUserId = null, string currency = "EGP")
    {
        var now = DateTimeOffset.UtcNow;
        return new StudentAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceStudentId = workspaceStudentId,
            Currency = string.IsNullOrWhiteSpace(currency) ? "EGP" : currency.Trim().ToUpperInvariant(),
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
