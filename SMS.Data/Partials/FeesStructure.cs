using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SMS.Data
{
    public partial class FeesStructure
    {


        [NotMapped]
        public int FeesStructureSequence { get; set; }

        [NotMapped]
        public string FeesStructureNumber => $"FEE-{FeesStructureSequence:D4}";

    }
}