using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.InterfaceSets.Configuration;

public class InterfaceSetExtension: IDbContextOptionsExtension
{
    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string? _logFragment;

        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment
        {
            get
            {
                if (_logFragment == null) _logFragment = string.Empty;

                return _logFragment;
            }
        }

        public new InterfaceSetExtension Extension => (InterfaceSetExtension)base.Extension;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override int GetServiceProviderHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(1);

            return hashCode.ToHashCode();
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return other is ExtensionInfo;
        }
    }

    private ExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // No services needed - handlers are auto-discovered via OperationHandlerRegistry
    }

    public void Validate(IDbContextOptions options)
    {
    }
}

public static class InterfaceSetExtensionExtensions
{
    public static DbContextOptionsBuilder<TContext> UseInventoryServices<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder) where TContext : DbContext
    {
        var extension = optionsBuilder.Options.FindExtension<InterfaceSetExtension>() ??
                        new InterfaceSetExtension();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}