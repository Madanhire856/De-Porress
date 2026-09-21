using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Village
    {
        [NotMapped]
        public int VillageSequence { get; set; }

        [NotMapped]
        public string VillageNumber => $"VLG-{VillageSequence:D4}";
    }
}