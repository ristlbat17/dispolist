using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Swashbuckle.AspNetCore.Annotations;

namespace Ristlbat17.Disposition.Material
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialController : ControllerBase
    {
        private readonly IMaterialDispositionContext _context;

        public MaterialController(IMaterialDispositionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all defined material.
        /// </summary>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GetMaterialList))]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialList()
        {
            return await _context.Material.Find(_ => true).ToListAsync();
        }

        /// <summary>
        /// Returns a material by its number (unique).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status404NotFound)]
        [SwaggerOperation(OperationId = nameof(GetMaterialById))]
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterialById(string id)
        {
            var current = await _context.Material.Find(mat => mat.Id == id).SingleOrDefaultAsync();
            if (current == null)
            {
                return NotFound();
            }

            return current;
        }

        /// <summary>
        /// Creates a new material 
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status201Created)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(NewMaterial))]
        [HttpPost]
        public async Task<ActionResult> NewMaterial(Material material)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _context.Material.InsertOneAsync(material);
                return CreatedAtAction("GetMaterialById", material.Id);
            }
            catch (MongoWriteException)
            {
                return BadRequest("SapNr is already in use");
            }
        }

        /// <summary>
        /// Updates an existing material 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = nameof(UpdateMaterial))]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMaterial(string id, Material material)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _context.Material.ReplaceOneAsync(item => item.Id == id, material, new UpdateOptions{ IsUpsert = true});
            return Ok();
        }

        /// <summary>
        /// Delete material by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerOperation(OperationId = nameof(DeleteMaterialById))]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMaterialById(string id)
        {
            var material = (await GetMaterialById(id)).Value;
            await _context.MaterialInventory.DeleteManyAsync(inv => inv.SapNr == material.SapNr);
            await _context.Material.DeleteOneAsync(mat => mat.Id == id);
            return NoContent();
        }

    }
}