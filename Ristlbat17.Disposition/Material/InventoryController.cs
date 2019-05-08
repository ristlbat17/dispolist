using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ristlbat17.Disposition.Material.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace Ristlbat17.Disposition.Material
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IMaterialInventoryService _materialInventoryService;

        public InventoryController(IMaterialInventoryService materialInventoryService)
        {
            _materialInventoryService = materialInventoryService;
        }

        /// <summary>
        ///     Get the inventory for one company
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetMaterialInventory))]
        [HttpGet("{company}")]
        public async Task<ActionResult<IEnumerable<MaterialInventoryItem>>> GetMaterialInventory(string company)
        {
            return await _materialInventoryService.GetInventoryForCompany(company);
        }

        /// <summary>
        ///     Get the inventory for all companies
        /// </summary>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetMaterialInventoryForAll))]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaterialInventoryItem>>> GetMaterialInventoryForAll()
        {
            return await _materialInventoryService.GetInventoryForAll();
        }


        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetMaterialInventoryForLocation))]
        [HttpGet("{company}/material/{sapNr}/locations/{location}")]
        public async Task<ActionResult<MaterialAllocation>> GetMaterialInventoryForLocation(string company,
            string location, string sapNr)
        {
            var inventoryItem = await _materialInventoryService.GetInventoryItem(sapNr, company);
            if (inventoryItem == null) return NotFound();

            return inventoryItem.Distribution.FirstOrDefault(dist => dist.Location == location);
        }

        /// <summary>
        ///     (currently not in use)  Distribute the company stock to it's locations (typically done after a "Micro Dispo".
        /// </summary>
        /// <param name="companyName"></param>
        /// <param name="sapNr"></param>
        /// <param name="distributionList"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(DistributeMaterialForCompany))]
        [HttpPut("{companyName}/material/{sapNr}/distribution")]
        public async Task<ActionResult> DistributeMaterialForCompany([FromRoute] string companyName,
            [FromRoute] string sapNr,
            MaterialDistribution distributionList)
        {
            await _materialInventoryService.DistributeMaterialForCompany(companyName, sapNr, distributionList);
            return Ok();
        }


        /// <summary>
        ///     Report that a certain material got damaged (typically only used during exercises)
        /// </summary>
        /// <param name="company"></param>
        /// <param name="location"></param>
        /// <param name="sapNr"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(MaterialDefect))]
        [HttpPatch("{company}/locations/{location}/material/{sapNr}/defected")]
        public async Task<ActionResult> MaterialDefect([FromRoute] string company, [FromRoute] string location,
            [FromRoute] string sapNr, PatchAmount patch)
        {
            var current = await _materialInventoryService.GetInventoryItem(sapNr, company);
            if (current == null) return BadRequest("No inventory available");

            if (current.Available - patch.Amount < 0)
                return BadRequest($"Can not damage more than stock {current.Available} at {company}");

            current.Distribution.First(item => item.Location == location).Damaged += patch.Amount;

            await _materialInventoryService.UpsertInventoryItem(company, sapNr, current);

            await _materialInventoryService.NewEventJournalEntry(new MaterialDamaged(sapNr, company, location,
                patch.Amount));
            return Ok();
        }


        /// <summary>
        ///     Report a damaged material got repaired (typically only used during exercises)
        /// </summary>
        /// <param name="company"></param>
        /// <param name="location"></param>
        /// <param name="sapNr"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(MaterialRepaired))]
        [HttpPatch("{company}/locations/{location}/material/{sapNr}/repaired")]
        public async Task<ActionResult> MaterialRepaired([FromRoute] string company, [FromRoute] string location,
            [FromRoute] string sapNr,
            PatchAmount patch)
        {
            var current = await _materialInventoryService.GetInventoryItem(sapNr, company);
            if (current == null) return BadRequest("No inventory available");

            if (current.Damaged - patch.Amount < 0) return BadRequest("Can not repair more than damaged");

            current.Distribution.First(item => item.Location == location).Damaged -= patch.Amount;

            await _materialInventoryService.UpsertInventoryItem(company, sapNr, current);

            await _materialInventoryService.NewEventJournalEntry(new MaterialRepaired(sapNr, location, company,
                patch.Amount));
            return Ok();
        }

        /// <summary>
        ///     You can change (+/-) the material in use on a location.
        /// </summary>
        /// <param name="company">Company identifier</param>
        /// <param name="sapNr">Material used</param>
        /// <param name="location">Location of the company</param>
        /// <param name="patch">Change</param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(MaterialUsed))]
        [HttpPatch("{company}/locations/{location}/material/{sapNr}/used")]
        public async Task<ActionResult> MaterialUsed([FromRoute] string company, [FromRoute] string sapNr,
            [FromRoute] string location,
            PatchAmount patch)
        {
            var inventoryItem = await _materialInventoryService.GetInventoryItem(sapNr, company);
            if (inventoryItem == null) return BadRequest();

            var usedAtLocation = inventoryItem.Distribution.SingleOrDefault(used => used.Location == location);
            if (usedAtLocation is null)
            {
                usedAtLocation = new MaterialAllocation {Used = 0, Location = location};
                inventoryItem.Distribution.Add(usedAtLocation);
            }

            usedAtLocation.Used += patch.Amount;
            if (usedAtLocation.Available < 0 || usedAtLocation.Used < 0)
                return BadRequest("Can not use more than on stock");

            await _materialInventoryService.UpsertInventoryItem(company, sapNr, inventoryItem);

            await _materialInventoryService.NewEventJournalEntry(new MaterialUsed(sapNr, company, patch.Amount,
                location));
            return Ok();
        }

        /// <summary>
        ///     Corrects the stock.
        /// </summary>
        /// <param name="company"></param>
        /// <param name="sapNr"></param>
        /// <param name="location"></param>
        /// <param name="newStock"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(CorrectMaterialStock))]
        [HttpPatch("{company}/locations/{location}/material/{sapNr}/stock")]
        public async Task<ActionResult> CorrectMaterialStock(string company, string sapNr, string location,
            PatchAmount newStock)
        {
            var current = await _materialInventoryService.GetInventoryItem(sapNr, company);
            if (current == null) return BadRequest("No inventory available");

            if (newStock.Amount < 0) return BadRequest("Stock must be a positive value");

            if (newStock.Amount < current.Used + current.Damaged)
                return BadRequest(
                    "you are not allowed to correct stock to lower number than (used + damaged) please redistribute");
            current.Distribution.First(item => item.Location == location).Stock = newStock.Amount;

            await _materialInventoryService.UpsertInventoryItem(company, sapNr, current);

            await _materialInventoryService.NewEventJournalEntry(new StockCorrected(sapNr, company, location,
                newStock.Amount));
            return Ok();
        }
    }
}