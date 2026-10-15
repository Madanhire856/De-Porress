using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Subject
    {
        [NotMapped]
        public int SubjectSequence { get; set; }

        [NotMapped]
        public string SubjectNumber => $"SUB-{SubjectSequence:D4}";
    }
}