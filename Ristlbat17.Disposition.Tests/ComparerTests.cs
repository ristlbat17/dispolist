using FluentAssertions;
using Xunit;
using static Ristlbat17.Disposition.Reporting.Reports.DispositionListReporter;

namespace Ristlbat17.Disposition.Tests
{
    public class ComparerTests
    {
        [Theory]
        [InlineData("Stab", "Stabs Kp")]
        [InlineData("Stab", "Stabskp")]
        [InlineData("Stabs Kp", "Kp")]
        [InlineData("Stabskp", "Kp")]
        [InlineData("Kp", "Bat")]
        public void CompanyNameComparerTests(string companyName1, string companyName2)
        {
            var sut = CompanyNameComparer.Instance;
            sut.Compare(companyName1, companyName2).Should().BeLessThan(0);
        }

        [Theory]
        [InlineData("Ristl", "Funk")]
        [InlineData("Funk", "Geheim Mat")]
        [InlineData("Geheim Mat", "Mat")]
        [InlineData("Mat", "Fz")]
        public void MaterialCategoryComparerTests(string materialCategory1, string materialCategory2)
        {
            var sut = MaterialCategoryComparer.Instance;
            sut.Compare(materialCategory1, materialCategory2).Should().BeLessThan(0);
        }

        [Theory]
        [InlineData("Of", "Höh Uof")]
        [InlineData("Höh Uof", "Uof")]
        [InlineData("Uof", "Mannschaft")]
        public void GradeRankComparerTests(string gradeRank1, string gradeRank2)
        {
            var sut = GradeRankComparer.Instance;
            sut.Compare(gradeRank1, gradeRank2).Should().BeLessThan(0);
        }
    }
}
