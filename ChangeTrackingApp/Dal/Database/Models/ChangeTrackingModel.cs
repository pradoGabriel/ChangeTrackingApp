using System.ComponentModel.DataAnnotations;

namespace ChangeTrackingApp.Dal.Database.Models
{
    public class ChangeTrackingModel
    {
        [Key]
        public int Id { get; set; }
        public string RandomString { get; set; }

    }
}
