using System;
using System.Collections.Generic;
using System.Text;

namespace SMS.Lib
{
    [Flags]
    public enum UserRole
    {
        CLASS_TEACHER = 1 << 0,
        HEAD = 1<<1,
        DEPUTY_HEAD = 1 << 2,
        BURSER = 1 << 3,
        PARENT = 1<<4,
        STAFF = 1<<5,
        ADMIN = 1 << 6,

    }
}
