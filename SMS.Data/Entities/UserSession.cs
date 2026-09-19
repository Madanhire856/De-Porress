using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime? LoginDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? LogoutDate { get; set; }
}
