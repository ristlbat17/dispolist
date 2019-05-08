using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Ristlbat17.Disposition.Reporting.Reports.DispositionListReporter;

namespace Ristlbat17.Disposition.Tests
{
    public class CompanyNameComparerTests
    {
        [Theory]
        [InlineData("Stab", "Stabs Kp")]
        [InlineData("Stab", "Stabskp")]
        [InlineData("Stabs Kp", "Kp")]
        [InlineData("Stabskp", "Kp")]
        [InlineData("Kp", "Bat")]
        public void OrderNameTests(string companyName1, string companyName2)
        {
            var sut = new CompanyNameComparer(StringComparer.CurrentCulture);
            sut.Compare(companyName1, companyName2).Should().BeLessThan(0);
        }
    }
}
