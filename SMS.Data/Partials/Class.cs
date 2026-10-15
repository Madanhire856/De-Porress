using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Class
    {
        [NotMapped]
        public int ClassSequence { get; set; }

        [NotMapped]
        public string ClassNumber => $"CLS-{ClassSequence:D4}";
    }
}