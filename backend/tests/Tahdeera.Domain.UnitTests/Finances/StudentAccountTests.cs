using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Finances;

public class StudentAccountTests
{
    [Fact]
    public void Create_ShouldInitializeValidAccount_WithDefaultEgpCurrency()
    {
        // Arrange
        var workspaceStudentId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();

        // Act
        var account = StudentAccount.Create(workspaceStudentId, createdByUserId);

        // Assert
        account.Id.Should().NotBeEmpty();
        account.WorkspaceStudentId.Should().Be(workspaceStudentId);
        account.Currency.Should().Be("EGP");
        account.CreatedByUserId.Should().Be(createdByUserId);
        account.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        account.Charges.Should().BeEmpty();
        account.Payments.Should().BeEmpty();
    }

    [Fact]
    public void StudentAccount_ShouldNotExposeBalanceProperty_PerConstitutionPrinciple8()
    {
        // Principle 8 (DEC-04): Account balance is derived algebraically in queries, never stored in a cached column.
        var property = typeof(StudentAccount).GetProperty("Balance");
        property.Should().BeNull("StudentAccount must strictly not contain a stored Balance column or mutable property");
    }
}
