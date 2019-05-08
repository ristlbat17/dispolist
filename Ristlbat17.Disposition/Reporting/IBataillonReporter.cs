using System;
using System.Threading.Tasks;

namespace Ristlbat17.Disposition.Reporting
{
    public interface IBataillonReporter
    {
        Task GenerateDispositionReport(DateTime dueDate);
    }
}