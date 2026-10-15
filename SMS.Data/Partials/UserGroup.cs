using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SMS.Data
{
    public partial class UserGroup
    {
        [NotMapped]
        public int UserGroupSequence { get; set; }

        [NotMapped]
        public string UserGroupNumber => $"UG-{UserGroupSequence:D4}";
    }
}