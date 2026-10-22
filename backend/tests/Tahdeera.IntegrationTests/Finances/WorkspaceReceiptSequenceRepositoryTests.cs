using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class WorkspaceReceiptSequenceRepositoryTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspaceReceiptSequenceRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetNextSequenceAsync_ConcurrentCalls_ShouldProduceMonotonicallyIncreasingGaplessSequences()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var admin = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace1 = new Workspace { Name = "Finance_Seq_WS_1", Kind = "Center", CreatedByUserId = admin.Id };
        var workspace2 = new Workspace { Name = "Finance_Seq_WS_2", Kind = "Center", CreatedByUserId = admin.Id };

        await db.Workspaces.AddRangeAsync(workspace1, workspace2);
        await db.SaveChangesAsync();

        const int yearMonth = 202610;
        const int concurrentRequests = 20;

        // Act: Execute 20 concurrent parallel calls to GetNextSequenceAsync for workspace1 in month 202610
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            using var taskScope = _factory.Services.CreateScope();
            var repo = taskScope.ServiceProvider.GetRequiredService<IWorkspaceReceiptSequenceRepository>();
            return await repo.GetNextSequenceAsync(workspace1.Id, yearMonth);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert 1: Exactly 20 distinct sequences generated with 0% collisions
        results.Should().HaveCount(concurrentRequests);
        results.Distinct().Should().HaveCount(concurrentRequests, "Every concurrent sequence request must produce a distinct number");

        // Assert 2: Sequences are strictly the set {1, 2, 3, ..., 20}
        results.OrderBy(x => x).Should().Equal(Enumerable.Range(1, concurrentRequests));

        // Act & Assert 3: Rollover to next month (202611) resets counter to 1
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var repo = verifyScope.ServiceProvider.GetRequiredService<IWorkspaceReceiptSequenceRepository>();
            var nextMonthSeq = await repo.GetNextSequenceAsync(workspace1.Id, 202611);
            nextMonthSeq.Should().Be(1, "New calendar month must atomically initialize sequence counter at 1");
        }

        // Act & Assert 4: Different workspace (workspace2) starts sequence counter at 1
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var repo = verifyScope.ServiceProvider.GetRequiredService<IWorkspaceReceiptSequenceRepository>();
            var otherWsSeq = await repo.GetNextSequenceAsync(workspace2.Id, yearMonth);
            otherWsSeq.Should().Be(1, "Different workspace in the same month must have its own isolated sequence starting at 1");
        }
    }
}
