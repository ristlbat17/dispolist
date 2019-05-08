using System.Collections.Generic;
using System.Threading.Tasks;
using Ristlbat17.Disposition.Material.Events;

namespace Ristlbat17.Disposition.Material
{
    public interface IMaterialInventoryService
    {
        Task DistributeMaterialForCompany(string companyName, string sapNr, MaterialDistribution distributionList);

        Task<MaterialInventoryItem> GetInventoryItem(string sapNr, string company);

        Task<List<MaterialInventoryItem>> GetInventoryForCompany(string companyName);

        Task UpsertInventoryItem(string companyName, string sapNr, MaterialInventoryItem current);

        Task NewEventJournalEntry<T>(T journalEntry) where T : MaterialEvent;

        Task MoveStockToDefaultLocation(string companyId, IEnumerable<string> locations);

        Task<List<MaterialInventoryItem>> GetInventoryForAll();
    }
}