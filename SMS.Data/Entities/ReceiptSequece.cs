using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class ReceiptSequece
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int LastNumber { get; set; }

    public DateTime LastIssuedOn { get; set; }
}
