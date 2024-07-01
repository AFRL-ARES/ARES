using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ARESCore;

public class ARESIdentityContext : IdentityDbContext<ARESUser>
{
  public ARESIdentityContext(DbContextOptions<ARESIdentityContext> options) : base(options)
  {
  }
}
