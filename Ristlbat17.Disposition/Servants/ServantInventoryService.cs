using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Ristlbat17.Disposition.Servants.Events;

namespace Ristlbat17.Disposition.Servants
{
    public class ServantInventoryService : IServantInventoryService
    {
        private readonly IServantDispositionContext _context;

        public ServantInventoryService(IServantDispositionContext context)
        {
            _context = context;
        }

        public async Task DistributeGradeForCompany(string companyName, Grade grade, GradeDistribution distributionList)
        {
            // check that location stock is not exceeded.
            var current = await GetInventoryItem(grade, companyName);

            foreach (var dist in distributionList.Distribution)
            {
                if (dist.Available < 0)
                {
                    throw new Exception(
                        $"Available is below Zero: Stock {dist.Stock} - Used {dist.Used} - Damage {dist.Detached} = {dist.Available}");
                }
            }

            if (current == null)
            {
                // initialise inventory
                current = new ServantInventoryItem {Company = companyName, Grade = grade};
            }

            //Set new distribution
            current.Distribution = distributionList.Distribution;

            await UpsertInventoryItem(companyName, grade, current);

            distributionList.Distribution.ForEach(async dist => { await NewEventJournalEntry(new DistributionChanged(grade, companyName, dist.Location, dist)); });
        }

        public Task UpsertInventoryItem(string companyName, Grade grade, ServantInventoryItem current)
        {
            return _context.ServantInventory.ReplaceOneAsync(item => item.Grade == grade &&
                                                                     item.Company == companyName,
                current, new UpdateOptions {IsUpsert = true});
        }

        public Task<ServantInventoryItem> GetInventoryItem(Grade grade, string company)
        {
            return _context.ServantInventory.Find(item => item.Grade == grade && item.Company == company)
                .SingleOrDefaultAsync();
        }

        public Task NewEventJournalEntry<T>(T journalEntry) where T : ServantEvent
        {
            return _context.EventJournal.InsertOneAsync(journalEntry.ToBsonDocument());
        }

        public Task<List<ServantInventoryItem>> GetInventory(string company)
        {
            return _context.ServantInventory.Find(item => item.Company == company).ToListAsync();
        }

        public async Task<List<ServantInventoryItem>> GetInventoryForAll()
        {
            return await _context.ServantInventory.Find(FilterDefinition<ServantInventoryItem>.Empty).ToListAsync();
        }

        public async Task MoveStockToDefaultLocation(string companyId, IReadOnlyCollection<string> removedLocations)
        {
            var current = await _context.Companies.Find(comp => comp.Id == companyId).SingleAsync();
            var inventoryItems = await GetInventory(current.Name);
            var reallocLocation = current.Locations.Any(c => c.Name.Equals("KP Front", StringComparison.InvariantCultureIgnoreCase)) ?
                "KP Front" :
                current.DefaultLocation.Name;

            foreach (var inventoryItem in inventoryItems)
            {
                if (!inventoryItem.Distribution.Any(dist => removedLocations.Contains(dist.Location))) continue;
                
                var removedAllocations =
                    inventoryItem.Distribution.Where(dist => removedLocations.Contains(dist.Location)).ToList();

                var newAllocations = inventoryItem.Distribution.Where(dist => !removedLocations.Contains(dist.Location)).ToList();
                
                var defaultAllocation = newAllocations.Single(dist => dist.Location == reallocLocation);

                defaultAllocation.Stock += removedAllocations.Sum(allocation => allocation.Stock);
                defaultAllocation.Detached += removedAllocations.Sum(allocation => allocation.Detached);
                defaultAllocation.Used += removedAllocations.Sum(allocation => allocation.Used);
                inventoryItem.Distribution = newAllocations.ToList();

                await UpsertInventoryItem(current.Name, inventoryItem.Grade, inventoryItem);
            }
        }
    }
}