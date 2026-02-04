using Microsoft.EntityFrameworkCore;

namespace UI.Data
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
    {
    }
  }
}
