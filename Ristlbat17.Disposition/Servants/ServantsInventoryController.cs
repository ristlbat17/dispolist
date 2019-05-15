using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ristlbat17.Disposition.Servants.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace Ristlbat17.Disposition.Servants
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServantsInventoryController : ControllerBase
    {
        private readonly IServantInventoryService _inventoryService;

        public ServantsInventoryController(IServantInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }


        /// <summary>
        ///     Get the servant inventory for one company
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetServantInventory))]
        [HttpGet("{company}")]
        public async Task<ActionResult<IEnumerable<ServantInventoryItem>>> GetServantInventory(string company)
        {
            return await _inventoryService.GetInventory(company);
        }
        /// <summary>
        ///     Get the servant inventory for all companies
        /// </summary>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetServantInventoryForAll))]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServantInventoryItem>>> GetServantInventoryForAll()
        {
            return await _inventoryService.GetInventoryForAll();
        }

        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetServantInventoryForLocation))]
        [HttpGet("{company}/servants/{grade}/locations/{location}")]
        public async Task<ActionResult<ServantAllocation>> GetServantInventoryForLocation(string company,
            string location, Grade grade)
        {
            var inventoryItem = await _inventoryService.GetInventoryItem(grade, company);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return inventoryItem.Distribution.FirstOrDefault(dist => dist.Location == location);
        }


        /// <summary>
        ///     (currently not in use)  Distribute the company stock to it's locations (typically done after a "Micro Dispo".
        /// </summary>
        /// <param name="companyName"></param>
        /// <param name="grade"></param>
        /// <param name="distributionList"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(DistributeServantsForCompany))]
        [HttpPut("{companyName}/servants/{grade}/distribution")]
        public async Task<ActionResult> DistributeServantsForCompany([FromRoute] string companyName, [FromRoute] Grade grade, GradeDistribution distributionList)
        {
            await _inventoryService.DistributeGradeForCompany(companyName, grade, distributionList);
            return Ok();
        }

        /// <summary>
        ///     Report that a certain material got damaged (typically only used during exercises)
        /// </summary>
        /// <param name="company"></param>
        /// <param name="location"></param>
        /// <param name="grade"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(ServantDetached))]
        [HttpPatch("{company}/locations/{location}/servants/{grade}/detached")]
        public async Task<ActionResult> ServantDetached([FromRoute] string company, [FromRoute] string location,
            [FromRoute] Grade grade, PatchAmount patch)
        {
            var current = await _inventoryService.GetInventoryItem(grade, company);
            if (current == null)
            {
                return BadRequest("No inventory available");
            }


            if (current.Available - patch.Amount < 0)
            {
                return BadRequest($"Can not damage more than stock {current.Available} at {company}");
            }

            current.Distribution.First(item => item.Location == location).Detached += patch.Amount;

            await _inventoryService.UpsertInventoryItem(company, grade, current);

            await _inventoryService.NewEventJournalEntry(new ServantDetached(grade, company, location, patch.Amount));
            return Ok();
        }


        /// <summary>
        ///     Report a damaged material got repaired (typically only used during exercises)
        /// </summary>
        /// <param name="company"></param>
        /// <param name="location"></param>
        /// <param name="grade"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(ServantReturned))]
        [HttpPatch("{company}/locations/{location}/servants/{grade}/returned")]
        public async Task<ActionResult> ServantReturned([FromRoute] string company, [FromRoute] string location,
            [FromRoute] Grade grade,
            PatchAmount patch)
        {
            var current = await _inventoryService.GetInventoryItem(grade, company);
            if (current == null)
            {
                return BadRequest("No inventory available");
            }

            if (current.Detached - patch.Amount < 0)
            {
                return BadRequest("Can not repair more than damaged");
            }

            current.Distribution.First(item => item.Location == location).Detached -= patch.Amount;

            await _inventoryService.UpsertInventoryItem(company, grade, current);

            await _inventoryService.NewEventJournalEntry(new ServantAvailable(grade, company, location, patch.Amount));
            return Ok();
        }

        /// <summary>
        ///     You can change (+/-) the material in use on a location.
        /// </summary>
        /// <param name="company">Company identifier</param>
        /// <param name="grade">Material used</param>
        /// <param name="location">Location of the company</param>
        /// <param name="patch">Change</param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(ServantUsed))]
        [HttpPatch("{company}/locations/{location}/servants/{grade}/used")]
        public async Task<ActionResult> ServantUsed([FromRoute] string company, [FromRoute] Grade grade,
            [FromRoute] string location,
            PatchAmount patch)
        {
            var inventoryItem = await _inventoryService.GetInventoryItem(grade, company);
            if (inventoryItem == null)
            {
                return BadRequest();
            }

            var usedAtLocation = inventoryItem.Distribution.SingleOrDefault(used => used.Location == location);
            if (usedAtLocation is null)
            {
                usedAtLocation = new ServantAllocation {Used = 0, Location = location};
                inventoryItem.Distribution.Add(usedAtLocation);
            }

            usedAtLocation.Used += patch.Amount;
            if (usedAtLocation.Available < 0 || usedAtLocation.Used < 0)
            {
                return BadRequest("Can not use more than on stock");
            }

            await _inventoryService.UpsertInventoryItem(company, grade, inventoryItem);

            await _inventoryService.NewEventJournalEntry(new ServantUsed(grade, company, location, patch.Amount));
            return Ok();
        }

        /// <summary>
        ///     Corrects the stock.
        /// </summary>
        /// <param name="company"></param>
        /// <param name="grade"></param>
        /// <param name="location"></param>
        /// <param name="newStock"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(CorrectServantStock))]
        [HttpPatch("{company}/locations/{location}/servants/{grade}/stock")]
        public async Task<ActionResult> CorrectServantStock(string company, Grade grade, string location,
            PatchAmount newStock)
        {
            var current = await _inventoryService.GetInventoryItem(grade, company);
            if (current == null)
            {
                return BadRequest("No inventory available");
            }


            if (newStock.Amount < 0)
            {
                return BadRequest("Stock must be a positive value");
            }

            // TODO check if we need to validate if new stock is exceeded (newStock - used - damaged)?
            current.Distribution.First(item => item.Location == location).Stock = newStock.Amount;

            await _inventoryService.UpsertInventoryItem(company, grade, current);

            await _inventoryService.NewEventJournalEntry(new StockCorrected(grade, company, location, newStock.Amount));
            return Ok();
        }


        /// <summary>
        ///     Corrects the ideal for company.
        /// </summary>
        /// <param name="company"></param>
        /// <param name="grade"></param>
        /// <param name="newIdeal"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(CorrectIdealForCompany))]
        [HttpPatch("{company}/servants/{grade}/stock")]
        public async Task<ActionResult> CorrectIdealForCompany(string company, Grade grade, PatchAmount newIdeal)
        {
            var current = await _inventoryService.GetInventoryItem(grade, company);
            if (current == null)
            {
                return BadRequest("No inventory available");
            }

            if (newIdeal.Amount < 0)
            {
                return BadRequest("Ideal must be a positive value");
            }

            current.Ideal = newIdeal.Amount;
            await _inventoryService.UpsertInventoryItem(company, grade, current);

            await _inventoryService.NewEventJournalEntry(new IdealCorrected(grade, company, newIdeal.Amount));
            return Ok();
        }
    }
}