using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class User
    {
        [NotMapped]
        public int UserSequence { get; set; }

        [NotMapped]
        public string UserNumber => $"USR-{UserSequence:D4}";

        [NotMapped]
        public string ActiveStatusText => IsActive ? "Active" : "Inactive";
    }
}