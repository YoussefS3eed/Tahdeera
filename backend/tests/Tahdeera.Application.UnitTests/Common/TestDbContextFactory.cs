using Mapster;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Mappings;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.Application.UnitTests.Common;

public static class TestDbContextFactory
{
    private static bool _mappingsInitialized;
    private static readonly object _lock = new();

    static TestDbContextFactory()
    {
        EnsureMappingsInitialized();
    }

    public static void EnsureMappingsInitialized()
    {
        if (_mappingsInitialized) return;
        lock (_lock)
        {
            if (_mappingsInitialized) return;
            new MappingConfig().Register(TypeAdapterConfig.GlobalSettings);
            _mappingsInitialized = true;
        }
    }

    public static TahdeeraDbContext Create(string? databaseName = null)
    {
        EnsureMappingsInitialized();
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TahdeeraDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new TahdeeraDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
