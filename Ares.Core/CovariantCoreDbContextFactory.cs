using Microsoft.EntityFrameworkCore;

namespace Ares.Core;

/// <summary>
/// Can be used to extend IDbContextFactory for derived contexts if one context is desired.
/// ex.: say you have AppDbContext that extends CoreDatabaseContext and you want to make a
/// single context factory that returns a AppDbContext but can be injected as IDbContextFactory
/// <CoreDatabaseContext>
/// as well
/// you could then do services.AddDbContextFactory
/// <AppDbContext>
/// ();
/// and then: services.AddTransient<IDbContextFactory
/// <CoreDatabaseContext>
/// >(provider
/// => new CovariantCoreDbContextFactory
/// <CoreDatabaseContext, AppDbContext>(provider.GetRequiredService<IDbContextFactory<AppDbContext>>()));
/// </summary>
/// <typeparam name="TContextOut"></typeparam>
/// <typeparam name="TContextIn"></typeparam>
public class CovariantCoreDbContextFactory<TContextOut, TContextIn>
  : IDbContextFactory<TContextOut>
  where TContextOut : CoreDatabaseContext
  where TContextIn : TContextOut
{
  private readonly IDbContextFactory<TContextIn> _contextInFactory;

  public CovariantCoreDbContextFactory(IDbContextFactory<TContextIn> contextInFactory)
  {
    _contextInFactory = contextInFactory;
  }

  public TContextOut CreateDbContext()
  {
    return _contextInFactory.CreateDbContext();
  }

  public async Task<TContextOut> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    return await _contextInFactory.CreateDbContextAsync(cancellationToken);
  }
}
