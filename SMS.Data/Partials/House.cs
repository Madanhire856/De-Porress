using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class House
    {
        [NotMapped]
        public int HouseSequence { get; set; }

        [NotMapped]
        public string HouseNumber => $"HSE-{HouseSequence:D4}";
    }
}