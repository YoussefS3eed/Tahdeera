using FluentAssertions;
using Tahdeera.Application.Common.Models.WhatsApp;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppTemplates;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class WhatsAppTemplateRegistryTests
{
    [Fact]
    public void Catalog_ShouldContainAllSixApprovedUtilityTemplates()
    {
        // Assert
        WhatsAppTemplateRegistry.Catalog.Should().HaveCount(6);
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("attendance_check_in_v1");
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("absence_alert_v1");
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("attendance_correction_v1");
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("exam_result_v1");
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("weekly_parent_report_v1");
        WhatsAppTemplateRegistry.Catalog.Should().ContainKey("payment_receipt_v1");
    }

    [Theory]
    [InlineData("{{اسم_الطالب}}", true)]
    [InlineData("{{اسم_السنتر}}", true)]
    [InlineData("{{student_name}}", true)]
    [InlineData("{{workspace_name}}", true)]
    [InlineData("{{unknown_variable}}", false)]
    [InlineData("{{script_tag}}", false)]
    public void IsValidPlaceholder_ShouldEnforceApprovedAllowlist(string placeholder, bool expectedValid)
    {
        // Act
        var isValid = WhatsAppTemplateRegistry.IsValidPlaceholder(placeholder);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public async Task Handle_GetWhatsAppTemplatesQuery_ShouldReturnCompleteCatalog()
    {
        // Arrange
        var handler = new GetWhatsAppTemplatesQueryHandler();
        var query = new GetWhatsAppTemplatesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Templates.Should().HaveCount(6);
        result.Value.AllowedPlaceholders.Ar.Should().NotBeEmpty();
        result.Value.AllowedPlaceholders.En.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_GetWhatsAppTemplateByNameQuery_WhenExists_ShouldReturnTemplate()
    {
        // Arrange
        var handler = new GetWhatsAppTemplatesQueryHandler();
        var query = new GetWhatsAppTemplateByNameQuery("attendance_check_in_v1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateName.Should().Be("attendance_check_in_v1");
        result.Value.Category.Should().Be("UTILITY");
    }

    [Fact]
    public async Task Handle_GetWhatsAppTemplateByNameQuery_WhenNotFound_ShouldReturnError()
    {
        // Arrange
        var handler = new GetWhatsAppTemplatesQueryHandler();
        var query = new GetWhatsAppTemplateByNameQuery("non_existent_template");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TEMPLATE_NOT_FOUND");
    }
}
