using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material;
using Xunit;

namespace Ristlbat17.Disposition.Tests
{
    public class MaterialControllerTests : IDisposable
    {
        private const string PuchSapNr = "MC 2003";
        private const string PuchDescription = "Puch";
        private const string CompanyName = "KP 17/1";
        private static MongoDbRunner _runner;
        private readonly IMaterialDispositionContext _materialDispositionContext;
        private readonly MaterialController _sut;

        public MaterialControllerTests()
        {
            _runner = MongoDbRunner.Start();

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            _materialDispositionContext = new DispositionContext(database);
            _sut = new MaterialController(_materialDispositionContext);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        [Fact]
        public async Task GivenNoData_WhenCreateMaterial_ThenReturnsOk()
        {
            var material = Material.Material.With(PuchSapNr, PuchDescription);

            var result = await _sut.NewMaterial(material);

            result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtResult = (CreatedAtActionResult) result;
            createdAtResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GivenMaterialWithSapNr_WhenCreatedSame_ThenReturnsBadRequest()
        {
           await _sut.NewMaterial(Material.Material.With(PuchSapNr, PuchDescription));

           var result = await _sut.NewMaterial(Material.Material.With(PuchSapNr, PuchDescription));

           result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GivenPuch_WhenGetById_ThenReturnsOK()
        {
            var materialId = await GivenPuch();

            var result =  await _sut.GetMaterialById(materialId);

            result.Value.SapNr.Should().Be(PuchSapNr);
            result.Value.Description.Should().Be(PuchDescription);
        }

        [Fact]
        public async Task GivenPuch_WhenGetList_ThenReturnsOKWithList()
        {
            var materialId = await GivenPuch();

            var result =  await _sut.GetMaterialList();

            result.Value.Should().HaveCount(1);
        }

        [Fact]
        public async Task GivenNoMaterial_WhenGetNonExisting_ThenReturnsNotFound()
        {
            var result =  await _sut.GetMaterialById(ObjectId.GenerateNewId().ToString());

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        private async Task<string> GivenPuch()
        {
            var result = (CreatedAtActionResult) await _sut.NewMaterial(Material.Material.With(PuchSapNr, PuchDescription));
            return result.Value.ToString();
        }

        [Fact]
        public async Task GivenPuchWithInventory_WhenDeletePuch_ThenEverythingGotRemoved()
        {
            var id = await GivenPuch();
            GivenPuchIventory();

            var result = await _sut.DeleteMaterialById(id);

            result.Should().BeOfType<NoContentResult>();
            _materialDispositionContext.MaterialInventory.AsQueryable().Should().BeEmpty();
        }

        private void GivenPuchIventory()
        {
            _materialDispositionContext.GivenInventoryItems(new MaterialInventoryItem
            {
                SapNr = PuchSapNr,
                Company = CompanyName,
                Distribution = new List<MaterialAllocation>
                {
                    MaterialAllocation.For("Bern", 12, 10, 2)
                }
            });
        }
    }
}