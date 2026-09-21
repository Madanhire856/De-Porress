using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Term
    {
        [NotMapped]
        public int TermSequence { get; set; }

        [NotMapped]
        public string TermDisplayId => $"TRM-{TermSequence:D4}";

        [NotMapped]
        public string Label => $"Term {TermNumber} · {AcademicYear}";
    }
}