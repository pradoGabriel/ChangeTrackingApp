using ChangeTrackingApp.Dal.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ChangeTrackingApp.Dal.Database
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options){ }

        public DbSet<ChangeTrackingModel> ChangeTracking { get; set; }
    }
}
