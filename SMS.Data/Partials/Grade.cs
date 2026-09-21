using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data
{
    public partial class Grade
    {
        [NotMapped]
        public int GradeSequence { get; set; }

        [NotMapped]
        public string GradeNumber => $"GRD-{GradeSequence:D4}";
    }
}