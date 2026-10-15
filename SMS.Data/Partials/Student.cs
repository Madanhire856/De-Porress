using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data;

public partial class Student
{
    [NotMapped]
    public int StudentSequence { get; set; }

    [NotMapped]
    public string StudentNumber => $"STD-{StudentSequence:D4}";
}