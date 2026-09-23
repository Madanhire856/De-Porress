using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class ReceiptSequence
{
    public int Year { get; set; }

    public int LastNumber { get; set; }

    public DateTime LastIssuedOn { get; set; }
}
