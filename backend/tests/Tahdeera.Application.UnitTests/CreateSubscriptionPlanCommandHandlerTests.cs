using FluentAssertions;
using Mapster;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Mappings;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests;

public class CreateSubscriptionPlanCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateSubscriptionPlanCommandHandler _handler;

    public CreateSubscriptionPlanCommandHandlerTests()
    {
        var config = TypeAdapterConfig.GlobalSettings;
        new MappingConfig().Register(config);

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _handler = new CreateSubscriptionPlanCommandHandler(
            _unitOfWork,
            _auditLogService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflictResult()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Code: "EXISTING_CODE",
            NameAr: "باقة موجودة",
            NameEn: "Existing Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 100m);

        _unitOfWork.SubscriptionPlans
            .ExistsByCodeAsync("EXISTING_CODE", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN_CODE_EXISTS");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _unitOfWork.SubscriptionPlans.DidNotReceive().AddAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreatePlanAndReturnSuccessResult()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Code: "NEW_CODE",
            NameAr: "باقة جديدة",
            NameEn: "New Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 200m);

        _unitOfWork.SubscriptionPlans
            .ExistsByCodeAsync("NEW_CODE", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Code.Should().Be("NEW_CODE");
        result.Value.DefaultPrice.Should().Be(200m);
        await _unitOfWork.SubscriptionPlans.Received(1).AddAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
