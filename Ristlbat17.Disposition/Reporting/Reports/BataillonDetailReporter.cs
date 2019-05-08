using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using Ristlbat17.Disposition.Material;

namespace Ristlbat17.Disposition.Reporting.Reports
{
    public class BataillonDetailReporter : DispositionListReporter
    {
        private const string WorksheetTitle = "Dispoliste Ristl Bat 17 Material - pro Kompanie und Standort";

        private const int StartRow = 5;
        private const int StartColumn = 4;

        private readonly List<MaterialReportItemBataillon> _reportItems;
        private readonly List<Material.Material> _materials;
        private readonly List<Company> _companies;
        
        public BataillonDetailReporter(List<ServantReportItemBataillon> servantReportItems, List<MaterialReportItemBataillon> materialReportItems)
        {
            //_reportItems = reportItems;

            //_materials = SortMaterialList(_reportItems.Select(reportItem => reportItem.Material).ToList());
        }

        public void GenerateBataillonDetailReport(ExcelPackage package)
        {
            
        }
    }
}
