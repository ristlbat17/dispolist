using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Mongo2Go;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting;
using Xunit;

namespace Ristlbat17.Disposition.Tests
{
    public class BatallionReporterTests : IDisposable
    {
        public BatallionReporterTests()
        {
            _runner = MongoDbRunner.Start();

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            _dispositionContext = new DispositionContext(database);
            _materialDispositionContext = new DispositionContext(database);
            _servantDispositionContext = new DispositionContext(database);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        private const string DuroSapNr = "256.156";

        private static MongoDbRunner _runner;
        private readonly IDispositionContext _dispositionContext;
        private readonly IMaterialDispositionContext _materialDispositionContext;
        private readonly IServantDispositionContext _servantDispositionContext;

        private async Task<DispositionReport> GetMaterialReport()
        {
            return await _materialDispositionContext.DispositionReport.Find(_ => true).FirstAsync();
        }

        private void GivenDuro()
        {
            _materialDispositionContext.GivenMaterial(Material.Material.With(DuroSapNr, "The ultimate Dujo"));
        }

        private void GivenCompany(string companyName, string defaultLocation = "Langnau i.E.")
        {
            _materialDispositionContext.Companies.InsertOne(Company.With(companyName, new List<Location> {Location.At(defaultLocation)}));
        }

        [Fact]
        public async Task GivenKP17Data_WhenGenerateReport_ThenBataillonIsTheSameAsCompany()
        {
            const string company = "Riststabskp 17";
            GivenCompany(company);
            GivenDuro();

            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                SapNr = DuroSapNr,
                Company = company,
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("Langnau i.E.", 12, 10, 2)
                }
            });

            var sut = new BataillonReporter(_dispositionContext, _materialDispositionContext, _servantDispositionContext);
            await sut.GenerateDispositionReport(new DateTime(2018, 10, 31, 0, 0, 0, DateTimeKind.Utc));

            var materialReport = await GetMaterialReport();
            var reportItem = materialReport.MaterialReportItems.Single(item => item.Material.SapNr == DuroSapNr);
            reportItem.Damaged.Should().Be(2);
            reportItem.Used.Should().Be(10);
            reportItem.Stock.Should().Be(12);
            reportItem.PerCompany.Should().HaveCount(1);
        }

        [Fact]
        public async Task GivenKp1AndKp2_WhenGenerateReport_ThenBattionIsSumOfBoth()
        {
            GivenDuro();

            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                SapNr = DuroSapNr,
                Company = "KP 17/1",
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("Bern", 12, 10, 2)
                }
            });

            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                SapNr = DuroSapNr,
                Company = "KP 17/2",
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("Willisau", 10, 5, 5)
                }
            });

            var sut = new BataillonReporter(_dispositionContext, _materialDispositionContext, _servantDispositionContext);
            await sut.GenerateDispositionReport(new DateTime(2018, 10, 31, 0, 0, 0, DateTimeKind.Utc));

            var materialReport = await GetMaterialReport();
            var reportItem = materialReport.MaterialReportItems.Single(item => item.Material.SapNr == DuroSapNr);
            reportItem.Damaged.Should().Be(7);
            reportItem.Used.Should().Be(15);
            reportItem.Stock.Should().Be(22);
            reportItem.PerCompany.Should().HaveCount(2);
            reportItem.PerCompany.Should().Contain(perComp => perComp.Company == "KP 17/1");
            reportItem.PerCompany.Should().Contain(perComp => perComp.Company == "KP 17/2");
        }

        [Fact]
        public async Task GivenNoData_WhenGenerateReport_ThenCreatesEmptyReport()
        {
            var sut = new BataillonReporter(_dispositionContext, _materialDispositionContext, _servantDispositionContext);
            var reportDate = new DateTime(2018, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            await sut.GenerateDispositionReport(reportDate);

            var materialReport = await _materialDispositionContext.DispositionReport.Find(_ => true).FirstAsync();
            materialReport.ReportDate.Should().Be(reportDate);
            materialReport.MaterialReportItems.Should().BeEmpty();
        }
    }
}