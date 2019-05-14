using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting.Reports;
using Ristlbat17.Disposition.Servants;
using Swashbuckle.AspNetCore.Annotations;

namespace Ristlbat17.Disposition.Administration
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly string[] _acceptedFileTypes = {".xlsx"};
        private readonly IMaterialDispositionContext _context;
        private readonly IServantDispositionContext _servantDispositionContext;
        private readonly IMaterialInventoryService _materialInventoryService;
        private readonly IServantInventoryService _servantInventoryService;
        private readonly CompanyTemplateGenerator _companyTemplateGenerator;

        public CompaniesController(IMaterialDispositionContext context,
            IMaterialInventoryService materialInventoryService, IServantInventoryService servantInventoryService, IServantDispositionContext servantDispositionContext,
            CompanyTemplateGenerator companyTemplateGenerator)
        {
            _context = context;
            _materialInventoryService = materialInventoryService;
            _servantInventoryService = servantInventoryService;
            _servantDispositionContext = servantDispositionContext;
            _companyTemplateGenerator = companyTemplateGenerator;
        }

        /// <summary>
        ///     Lists all companies
        /// </summary>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetCompanies))]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies.Find(_ => true)
                .ToListAsync();
        }

        /// <summary>
        ///     Returns a company by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status404NotFound)]
        [SwaggerOperation(OperationId = nameof(GetCompanyById))]
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompanyById(string id)
        {
            var company = await _context.Companies.Find(comp => comp.Id == id).SingleOrDefaultAsync();
            if (company == null)
            {
                return NotFound();
            }

            return company;
        }

        /// <summary>
        ///     Create a new company
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status201Created)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(NewCompany))]
        [HttpPost]
        public async Task<ActionResult> NewCompany([FromBody] Company company)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _context.Companies.InsertOneAsync(company);
            return CreatedAtAction(null, null);
        }

        /// <summary>
        ///     Updates a company and if there are removed locations their stock + damaged material gets added to the default
        ///     location.
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(UpdateCompany))]
        [HttpPut]
        public async Task<ActionResult> UpdateCompany([FromBody] Company updated)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var current = await _context.Companies.Find(comp => comp.Id == updated.Id).SingleAsync();
            var removedLocations = new HashSet<string>(current.Locations.Select(_ => _.Name));
            removedLocations.ExceptWith(updated.Locations.Select(_ => _.Name));

            await _materialInventoryService.MoveStockToDefaultLocation(updated.Id, removedLocations);
            await _servantInventoryService.MoveStockToDefaultLocation(updated.Id, removedLocations);
            await _context.Companies.ReplaceOneAsync(comp => comp.Id == current.Id, updated);
            return Ok();
        }

        /// <summary>
        ///     Delete company by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerOperation(OperationId = nameof(DeleteCompanyById))]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCompanyById(string id)
        {
            var company = _context.Companies.AsQueryable().First(c => c.Id == id);
            _context.MaterialInventory.DeleteMany(inventoryItem => inventoryItem.Company == company.Name);
            _servantDispositionContext.ServantInventory.DeleteMany(inventoryItem => inventoryItem.Company == company.Name);
            await _context.Companies.DeleteOneAsync(c => c.Id == id);
            return NoContent();
        }

        /// <summary>
        ///     Generate a grade and material,servants distribution list template for the given company
        ///     1) If the company has only the default location it generates only KP Rw (initial report).
        ///     2) If there are additional locations it generates a distribution excel => to distribute all material,servants to
        ///     the
        ///     locations.
        /// </summary>
        /// <param name="companyName"></param>
        /// <returns></returns>
        [SwaggerOperation(OperationId = nameof(DownloadCompanyTemplate))]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
        [HttpGet("{companyName}/templates")]
        public ActionResult DownloadCompanyTemplate(string companyName)
        {
            byte[] data;
            using (var package = new ExcelPackage())
            {
                _companyTemplateGenerator.GenerateCompanyTemplate(package, companyName);
                data = package.GetAsByteArray();
            }

            var fileDownloadName = $"Dispoliste {companyName}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
        }

        /// <summary>
        ///     Initialize the inventory per company and distribute the company's stock to it's locations (typically at the begging
        ///     of a "WK" or after a "Micro Dispo")
        /// </summary>
        /// <param name="companyName"></param>
        /// <param name="initialReportPerCompany">filled template file (empty cells = 0)</param>
        /// <returns></returns>
        [HttpPost("{companyName}/report")]
        public async Task<IActionResult> ReportInitialDistribution([FromRoute] string companyName,
            IFormFile initialReportPerCompany)
        {
            // split readers (personal reader, material reader)
            // How to https://stackoverflow.com/questions/46895523/asp-core-webapi-test-file-upload-using-postman
            if (initialReportPerCompany == null || initialReportPerCompany.Length == 0)
            {
                return BadRequest();
            }

            if (_acceptedFileTypes.All(s => s != Path.GetExtension(initialReportPerCompany.FileName).ToLower()))
            {
                return BadRequest("Invalid file type.");
            }

            companyName = Uri.UnescapeDataString(companyName);

            var company = await _context.Companies.Find(comp => comp.Name == companyName).SingleOrDefaultAsync();
            if (company == null)
            {
                return BadRequest("Company not found");
            }

            var gradeDescriptions = (Grade[]) Enum.GetValues(typeof(Grade));
            if (gradeDescriptions == null)
            {
                return BadRequest("No grades defined");
            }

            var materials = await _context.Material.Find(_ => true).ToListAsync();
            if (materials == null)
            {
                return BadRequest("No materials defined");
            }

            using (var memoryStream = new MemoryStream())
            {
                await initialReportPerCompany.CopyToAsync(memoryStream);
                using (var package = new ExcelPackage(memoryStream))
                {
                    var companyInventory = new CompanyInventoryGenerator(materials, company.Locations);
                    var (errorMessages, servantDistribution, materialDistribution) = companyInventory.ExtractCompanyInventory(package);

                    if (errorMessages.Any())
                    {
                        foreach (var kvp in errorMessages)
                        {
                            ModelState.AddModelError(kvp.Key, kvp.Value);
                        }

                        return BadRequest(ModelState);
                    }

                    await DistributeServantInventory(gradeDescriptions, company, servantDistribution);

                    await DistributeMaterialInventory(materials, company, materialDistribution);
                }
            }

            return Ok();
        }

        private async Task DistributeMaterialInventory(IEnumerable<Material.Material> materials, Company company, List<List<MaterialAllocation>> materialDistribution)
        {
            var materialsSorted = DispositionListReporter.SortMaterialList(materials);
            for (var i = 0; i < materialsSorted.Count; i++)
            {
                await _materialInventoryService.DistributeMaterialForCompany(company.Name, materialsSorted[i].SapNr,
                    new MaterialDistribution
                    {
                        Distribution = materialDistribution[i]
                    });
            }
        }

        private async Task DistributeServantInventory(IEnumerable<Grade> gradeDescriptions, Company company, List<List<ServantAllocation>> servantDistribution)
        {
            var gradeDescriptionsSorted = DispositionListReporter.SortGradeList(gradeDescriptions);
            for (var i = 0; i < gradeDescriptionsSorted.Count; i++)
            {
                await _servantInventoryService.DistributeGradeForCompany(company.Name, gradeDescriptionsSorted[i].GetValueFromDescription<Grade>(),
                    new GradeDistribution
                    {
                        Distribution = servantDistribution[i]
                    });
            }
        }
    }
}