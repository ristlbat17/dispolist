using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Ristlbat17.Disposition.Servants
{
    public enum Grade
    {
        [Description("Of")]
        Offizere,
        [Description("Höh Uof")]
        HoehereUnteroffiziere,
        [Description("Uof")]
        Unteroffiziere,
        [Description("Mannschaft")]
        Mannschaft
    }
}