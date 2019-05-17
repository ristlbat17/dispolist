using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mongo2Go;
using MongoDB.Driver;
using Ristlbat17.Disposition.Administration;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting.Reports;
using Ristlbat17.Disposition.Servants;
using Xunit;

namespace Ristlbat17.Disposition.Tests
{
    public class CompaniesControllerTests : IDisposable
    {
        public CompaniesControllerTests()
        {
            _runner = MongoDbRunner.Start();

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            var dispositionContext = new DispositionContext(database);
            _materialDispositionContext = dispositionContext;
            _servantDispositionContext = dispositionContext;
            var inventoryService = new MaterialInventoryService(_materialDispositionContext);
            var servantInventoryService = new ServantInventoryService(_servantDispositionContext);

            var companyTemplateGenerator = new CompanyTemplateGenerator(_materialDispositionContext);
            var companyInventoryGenerator = new CompanyInventoryGenerator(_materialDispositionContext);

            _sut = new CompaniesController(_materialDispositionContext, inventoryService, servantInventoryService, _servantDispositionContext, companyTemplateGenerator, companyInventoryGenerator);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        private const string CompanyName = "Ristlstabskp17";
        private static MongoDbRunner _runner;
        private readonly IMaterialDispositionContext _materialDispositionContext;
        private readonly IServantDispositionContext _servantDispositionContext;
        private readonly CompaniesController _sut;

        private async Task<string> GivenCompanyWithKpFront()
        {
            var companyId = await GivenCompany("KP Rw", "KP Front", "Somewhere");
            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                Company = CompanyName,
                SapNr = "256.165",
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("Somewhere", 5),
                    MaterialAllocation.For("KP Rw", 0),
                    MaterialAllocation.For("KP Front", 0),
                }
            });
            return companyId;
        }

        private async Task<string> GivenCompanyWithInventory()
        {
            var companyId = await GivenCompany("KP Rw", "Langnau i.E.");
            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                Company = CompanyName,
                SapNr = "256.165",
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("KP Rw", 5)
                }
            });

            return companyId;
        }

        private async Task<string> GivenCompany(params string[] locationNames)
        {
            var company = Company.With(CompanyName, locationNames.Select(Location.At).ToList());
            await _materialDispositionContext.Companies.InsertOneAsync(company);
            return _materialDispositionContext.Companies.Find(c => c.Name == CompanyName).First().Id;
        }

        [Fact]
        public async Task GivenCompany_WhenDownloadTemplate_ThenReturnsOkWithExcel()
        {
            await GivenCompany("KP Rw", "Langnau i.E.");

            var templateResult = _sut.DownloadCompanyTemplate(CompanyName);

            templateResult.Should().BeOfType<FileContentResult>();
            var contentResult = (FileContentResult) templateResult;
            contentResult.FileDownloadName.Should().Be($"Dispoliste {CompanyName}.xlsx");
            contentResult.FileContents.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GivenCompanyWithInventory_WhenDeleteCompany_ThenCompanyWasRemovedFromInventory()
        {
            var companyId = await GivenCompanyWithInventory();

            var result = await _sut.DeleteCompanyById(companyId);

            result.Should().BeOfType<NoContentResult>();
            _materialDispositionContext.MaterialInventory.AsQueryable().Should().BeEmpty();
            _servantDispositionContext.ServantInventory.AsQueryable().Should().BeEmpty();
        }

        [Fact]
        public async Task GivenCompanyWithKPFront_WhenDeleteLocation_ThenStockWasReallocatedToKPFront()
        {
            var companyId = await GivenCompanyWithKpFront();
            var comp = (await _sut.GetCompanyById(companyId)).Value;

            comp.Locations = comp.Locations.Where(l => l.Name != "Somewhere").ToList();
            await _sut.UpdateCompany(comp);

            var materialInventoryItem = await _materialDispositionContext.MaterialInventory.Find(i => i.Company == CompanyName).FirstAsync();
            materialInventoryItem.Distribution.Should().NotContain(d => d.Location == "Somewhere");
            materialInventoryItem.Distribution.Should().Contain(d => d.Location == "KP Front" && d.Stock == 5);
        }

        [Fact]
        public async Task GivenNoData_WhenInsertCompanyFull_ThenReturnsOk()
        {
            var location1 = Location.At("Langnau");
            var location2 = Location.At("Affoltern");
            var locations = new List<Location>
            {
                location1, location2
            };

            var result = await _sut.NewCompany(Company.With(CompanyName, locations));

            result.Should().BeOfType<CreatedAtActionResult>();
            var company = _materialDispositionContext.Companies.Find(comp => comp.Name == CompanyName).First();
            company.Name.Should().Be(CompanyName);
            company.Locations.Should().Contain(loc => loc.Name == location1.Name);
            company.Locations.Should().Contain(loc => loc.Name == location2.Name);
            company.DefaultLocation.Name.Should().Be(location1.Name);
        }
    }
}