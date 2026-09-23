using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Sport
    {
        [NotMapped]
        public int SportSequence { get; set; }

        [NotMapped]
        public string SportNumber => $"SPT-{SportSequence:D4}";
    }
}