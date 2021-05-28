using Microsoft.Azure.Cosmos.Table;

namespace ChangeTrackingApp.Dal.Database.Models
{
    public class ChangeTrackingVersionModel : TableEntity
    {
        public long Version { get; set; }
    }
}
