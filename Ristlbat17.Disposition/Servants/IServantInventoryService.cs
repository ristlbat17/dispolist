using System.Collections.Generic;
using System.Threading.Tasks;
using Ristlbat17.Disposition.Servants.Events;

namespace Ristlbat17.Disposition.Servants
{
    public interface IServantInventoryService
    {
        Task DistributeGradeForCompany(string companyName, Grade grade, GradeDistribution distributionList);

        Task UpsertInventoryItem(string companyName, Grade grade, ServantInventoryItem current);

        Task<ServantInventoryItem> GetInventoryItem(Grade grade, string company);

        Task NewEventJournalEntry<T>(T journalEntry) where T : ServantEvent;

        Task<List<ServantInventoryItem>> GetInventory(string company);

        Task<List<ServantInventoryItem>> GetInventoryForAll();

        Task MoveStockToDefaultLocation(string companyId, IReadOnlyCollection<string> removedLocations);
    }
}