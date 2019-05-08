using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mongo2Go;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material;
using Xunit;

namespace Ristlbat17.Disposition.Tests
{
    public class IventoryControllerTests : IDisposable
    {
        public IventoryControllerTests()
        {
            _runner = MongoDbRunner.Start();

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            _materialDispositionContext = new DispositionContext(database);
            _inventoryService = new MaterialInventoryService(_materialDispositionContext);
            _sut = new InventoryController(_inventoryService);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        private const string CompanyName = "Ristlstabskp17";
        private const string PuchSapNr = "1234";
        private static MongoDbRunner _runner;
        private readonly IMaterialDispositionContext _materialDispositionContext;
        private readonly IMaterialInventoryService _inventoryService;
        private InventoryController _sut;

        [Fact]
        public async Task GivenDefectedPuch_WhenDefectedMoreThanStock_ThenReturnsBadRequest()
        {
            GivenPuchInventory("Langnau", stock: 5, damaged: 4);

            var result = await _sut.MaterialDefect(CompanyName, "Langnau", PuchSapNr, new PatchAmount
            {
                Amount = 2
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public async Task GivenDefectedPuch_WhenPuchRepaired_ThenDefectedIsZero()
        {
            GivenPuchInventory("Langnau", stock: 1, damaged: 1);

            var result = await _sut.MaterialRepaired(CompanyName, "Langnau", PuchSapNr, new PatchAmount {Amount = 1});

            result.Should().BeOfType<OkResult>();
            var updated = _materialDispositionContext.MaterialInventory.Find(_ => true).First();
            updated.Damaged.Should().Be(0);
        }

        private void GivenPuchInventory(string location, int stock = 0, int damaged = 1, int used = 0)
        {
            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                Company = CompanyName,
                SapNr = PuchSapNr,
                Distribution = new List<MaterialAllocation>
                {
                    new MaterialAllocation
                    {
                        Stock =stock,
                        Damaged = damaged,
                        Location = location,
                        Used = used
                    }
                }
            });
        }

        [Fact]
        public async Task GivenIntial10Puchs_WhenDefectedPuch_ThenDefectedOne()
        {
            GivenPuchInventory("Langnau", stock: 5, damaged: 0);

            var result = await _sut.MaterialDefect(CompanyName, "Langnau", PuchSapNr, new PatchAmount
            {
                Amount = 1
            });

            result.Should().BeOfType<OkResult>();
            var updated = _materialDispositionContext.MaterialInventory.Find(_ => true).First();
            updated.Damaged.Should().Be(1);
        }

        [Fact]
        public async Task GivenNoCompany_WhenCallInitial_ThenReturnsBadRequest()
        {
            var sut = new InventoryController(_inventoryService);
            var result = await sut.CorrectMaterialStock("Not there", "Not there","", null);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GivenNoIventory_WhenCallCorrectStock_ThenReturnsBadRequest()
        {
            var langnau = Location.At("Langnau");
            var company = Company.With(CompanyName, new List<Location> {langnau});
            await _materialDispositionContext.Companies.InsertOneAsync(company);

            var newStock = PatchAmount.With(5);
            var result = await _sut.CorrectMaterialStock(CompanyName, PuchSapNr, langnau.Name, newStock);

            result.Should().BeOfType<BadRequestObjectResult>("you are not allowed to correct stock without initial inventory");
        }

        [Fact]
        public async Task GivenInventoryWithoutAvailablity_WhenCorrectStockToLowerCapacity_ThenReturnsBadRequest()
        {
            var langnau = Location.At("Langnau");
            GivenPuchInventory("Langnau", stock: 5, damaged: 3, used: 2);

            var newStock = PatchAmount.With(2);
            var result = await _sut.CorrectMaterialStock(CompanyName, PuchSapNr, langnau.Name, newStock);

            result.Should().BeOfType<BadRequestObjectResult>("you are not allowed to correct stock to lower number than (used + damaged) please redistribute");
        }
    }
}