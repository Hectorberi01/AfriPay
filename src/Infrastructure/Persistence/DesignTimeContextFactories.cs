
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AfriPay.Infrastructure.Persistence;

public sealed class LiveDbContextFactory
    : IDesignTimeDbContextFactory<LiveDbContext>
{
    public LiveDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<LiveDbContext>()
            .UseNpgsql(
                "Host=193.168.145.162;Port=5432;Database=afripay_live;Username=postgres;Password=Hba@Str0ng#2026;Include Error Detail=true",
                //n => n.MigrationsAssembly(typeof(LiveDbContext).Assembly.FullName)
                n=>n.MigrationsHistoryTable("__ef_migrations"))
            .Options;

        return new LiveDbContext(opts);
    }
}

public sealed class SandboxDbContextFactory : IDesignTimeDbContextFactory<SandboxDbContext>
{
    public SandboxDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseNpgsql(
                "Host=193.168.145.162;Port=5432;Database=afripay_sandbox;Username=postgres;Password=Hba@Str0ng#2026;Include Error Detail=true",
                //n => n.MigrationsAssembly(typeof(SandboxDbContext).Assembly.FullName)
                    n=>n.MigrationsHistoryTable("__ef_migrations"))
            .Options;

        return new SandboxDbContext(opts);
    }
}