using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material.Events;

namespace Ristlbat17.Disposition.Material
{
    public class MaterialInventoryService : IMaterialInventoryService
    {
        private readonly IMaterialDispositionContext _context;

        public MaterialInventoryService(IMaterialDispositionContext context)
        {
            _context = context;
        }

        public async Task DistributeMaterialForCompany(string companyName, string sapNr,
            MaterialDistribution distributionList)
        {
            // check that location stock is not exceeded.
            var current = await GetInventoryItem(sapNr, companyName);

            foreach (var dist in distributionList.Distribution)
            {
                if (dist.Available < 0)
                {
                    throw new Exception(
                        $"Available is below Zero: Stock {dist.Stock} - Used {dist.Used} - Damage {dist.Damaged} = {dist.Available}");
                }
            }

            if (current == null)
            {
                // initialise inventory
                current = new MaterialInventoryItem { Company = companyName, SapNr = sapNr };
            }
            //Set new distribution
            current.Distribution = distributionList.Distribution;

            await UpsertInventoryItem(companyName, sapNr, current);

            distributionList.Distribution.ForEach(async dist =>
            {
                await NewEventJournalEntry(new DistributionChanged(sapNr, companyName, dist.Location, dist));
            });
        }

        public async Task<List<MaterialInventoryItem>> GetInventoryForCompany(string companyName)
        {
            return await _context.MaterialInventory.Find(item => item.Company == companyName)
                .ToListAsync();
        }

        public async Task<List<MaterialInventoryItem>> GetInventoryForAll()
        {
            return await _context.MaterialInventory.Find(FilterDefinition<MaterialInventoryItem>.Empty).ToListAsync();

        }

        public Task UpsertInventoryItem(string companyName, string sapNr, MaterialInventoryItem current)
        {
            return _context.MaterialInventory.ReplaceOneAsync(item => item.SapNr == sapNr &&
                                                             item.Company == companyName,
                current, new UpdateOptions { IsUpsert = true });
        }

        public Task NewEventJournalEntry<T>(T journalEntry)
            where T : MaterialEvent
        {
            return _context.EventJournal.InsertOneAsync(journalEntry.ToBsonDocument());
        }

        public async Task MoveStockToDefaultLocation(string companyId, IEnumerable<string> locations)
        {
            var current = await _context.Companies.Find(comp => comp.Id == companyId).SingleAsync();
            var inventoryItems = await GetInventoryForCompany(current.Name);
            var reallocLocation = current.Locations.Any(c => c.Name.Equals("KP Front", StringComparison.InvariantCultureIgnoreCase)) ?
                   "KP Front" :
                   current.DefaultLocation.Name;

            foreach (var inventoryItem in inventoryItems)
            {
                if (!inventoryItem.Distribution.Any(dist => locations.Contains(dist.Location))) continue;
                
                var removedAllocations =
                    inventoryItem.Distribution.Where(dist => locations.Contains(dist.Location)).ToList();

                var newAllocations = inventoryItem.Distribution.Where(dist => !locations.Contains(dist.Location)).ToList();
                
                var defaultAllocation = newAllocations.Single(dist => dist.Location == reallocLocation);

                defaultAllocation.Stock += removedAllocations.Sum(allocation => allocation.Stock);
                defaultAllocation.Damaged += removedAllocations.Sum(allocation => allocation.Damaged);
                defaultAllocation.Used += removedAllocations.Sum(allocation => allocation.Used);
                inventoryItem.Distribution = newAllocations.ToList();

                await UpsertInventoryItem(current.Name, inventoryItem.SapNr, inventoryItem);
            }
        }

        public Task<MaterialInventoryItem> GetInventoryItem(string sapNr, string company)
        {
            return Task.FromResult(_context.MaterialInventory.AsQueryable().SingleOrDefault(item => item.SapNr == sapNr && item.Company == company));
        }
    }
}