using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public DateTime Date { get; set; }

    public int Status { get; set; }

    public string? CorrectionReason { get; set; }

    public Guid StaffId { get; set; }
}
