using Microsoft.EntityFrameworkCore;

namespace UI.Infrastructure.Persistence
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
    {
    }
  }
}
