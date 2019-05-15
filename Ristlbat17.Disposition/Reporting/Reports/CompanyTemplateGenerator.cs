using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Servants;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Ristlbat17.Disposition.Reporting.Reports
{
    public class CompanyTemplateGenerator : DispositionListReporter
    {
        private const string CumulatedSheetDescription = "Total";

        private int _startRow, _startColumn;

        private List<ExcelWorksheet> _worksheets;
        private Company _company;
        private List<string> _gradeDescriptions;
        private List<Material.Material> _materials;

        private readonly IMaterialDispositionContext _context;

        public CompanyTemplateGenerator(IMaterialDispositionContext context)
        {
            _context = context;
        }

        public void GenerateCompanyTemplate(ExcelPackage package, string companyName)
        {
            _company = _context.Companies.Find(company => company.Name == companyName).First();
            _gradeDescriptions = SortGradeList((Grade[])Enum.GetValues(typeof(Grade)));
            _materials = SortMaterialList(_context.Material.Find(_ => true).ToList());

            // 1. Create excel worksheets (one foreach location and total, default location will be the second worksheet right after the cumulated sheet)
            _worksheets = GenerateWorksheets(package);

            foreach (var worksheet in _worksheets)
            {
                // 2. Define start row and start column
                _startRow = 1;
                _startColumn = 4;

                // 3. For each worksheet create a worksheet overall title and the servant subtitle
                InsertWorksheetTitle(worksheet, $"Dispoliste {_company.Name}, {(string.Equals(worksheet.Name, CumulatedSheetDescription) || string.Equals(worksheet.Name, _company.DefaultLocation.Name) ? worksheet.Name : $"Standort {worksheet.Name}")}", 1, 18);
                InsertWorksheetTitle(worksheet, "Personal", 0, 14);

                // 4. For each worksheet insert grade list and according columns
                var startServantList = _startRow;
                InsertServantSectionColumns(worksheet, string.Equals(worksheet.Name, CumulatedSheetDescription) ? ServantSectionColumnsTotal : ServantSectionColumns);
                InsertServantSectionRows(worksheet);

                // 5. For each worksheet create the material subtitle
                InsertWorksheetTitle(worksheet, "Material", 0, 14);

                // 6. For each worksheet insert material list and according columns
                var startMaterialList = _startRow;
                InsertMaterialSectionColumns(worksheet, MaterialSectionColumns, string.Equals(worksheet.Name, CumulatedSheetDescription) ? _startColumn + 1 : _startColumn);
                InsertMaterialSectionRows(worksheet, _materials);

                // 7. For each worksheet format input section, add formulas where necessary and unlock certain cells within each worksheet
                FormatServantInputSection(worksheet, false, startServantList);
                FormatMaterialInputSection(worksheet, _materials, false, startMaterialList);

                // 8. Lock the workbook totally (no password required to unlock the worksheets)
                ProtectWorksheet(worksheet);

                // 9. Insert headers and footers
                InsertHeaderFooter(worksheet, worksheet.Name, DateTime.UtcNow);

                // 10 For each worksheet set column widhts
                SetColumnWidths(worksheet);

                // 11. Printer settings
                ApplyPrinterSettings(worksheet, eOrientation.Portrait, 1, _startRow - 1);
            }
        }

        private List<ExcelWorksheet> GenerateWorksheets(ExcelPackage package)
        {
            var sortedCompanyLocations = SortCompanyLocations(_company.Locations.Select(location => location.Name).ToList());
            sortedCompanyLocations.Remove(_company.DefaultLocation.Name);

            var worksheets = new List<ExcelWorksheet>
            {
                package.Workbook.Worksheets.Add(CumulatedSheetDescription),
                package.Workbook.Worksheets.Add(_company.DefaultLocation.Name)
            };
            sortedCompanyLocations.ForEach(location => worksheets.Add(package.Workbook.Worksheets.Add(location)));

            return worksheets;
        }

        private void InsertWorksheetTitle(ExcelWorksheet worksheet, string worksheetTitle, int spaceAfter, int fontSize)
        {
            var titleCell = ColumnIndexToColumnLetter(1) + _startRow;
            worksheet.Cells[titleCell].Value = worksheetTitle;
            worksheet.Cells[titleCell].Style.Font.Bold = true;
            worksheet.Cells[titleCell].Style.Font.Size = fontSize;
            _startRow += spaceAfter + 1;
        }

        private void InsertServantSectionColumns(ExcelWorksheet worksheet, List<string> servantSectionColumnDescriptions)
        {
            for (var i = 0; i < servantSectionColumnDescriptions.Count; i++)
            {
                var headerCell = ColumnIndexToColumnLetter(_startColumn + i) + _startRow;
                worksheet.Cells[headerCell].Value = servantSectionColumnDescriptions[i];
                worksheet.Cells[headerCell].Style.Font.Bold = true;
                worksheet.Cells[headerCell].Style.TextRotation = 90;
                worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Column(_startColumn + i).Width = 3.8;
            }
        }

        private void InsertServantSectionRows(ExcelWorksheet worksheet)
        {
            foreach (var gradeDescription in _gradeDescriptions)
            {
                var gradeCell = ColumnIndexToColumnLetter(2) + (++_startRow);
                worksheet.Cells[gradeCell].Value = gradeDescription;
                worksheet.Cells[gradeCell].Style.Font.Bold = true;
                worksheet.Cells[gradeCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            var totalCell = ColumnIndexToColumnLetter(2) + (++_startRow);
            worksheet.Cells[totalCell].Value = "Total";
            worksheet.Cells[totalCell].Style.Font.Bold = true;
            worksheet.Cells[totalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            _startRow += 2;
        }

        private void InsertMaterialSectionColumns(ExcelWorksheet worksheet, List<string> materialSectionColumnDescriptions, int startColumn)
        {
            for (var i = 0; i < materialSectionColumnDescriptions.Count; i++)
            {
                var headerCell = ColumnIndexToColumnLetter(startColumn + i) + _startRow;
                worksheet.Cells[headerCell].Value = materialSectionColumnDescriptions[i];
                worksheet.Cells[headerCell].Style.Font.Bold = true;
                worksheet.Cells[headerCell].Style.TextRotation = 90;
                worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Column(startColumn + i).Width = 3.8;
            }
        }

        private void InsertMaterialSectionRows(ExcelWorksheet worksheet, IReadOnlyList<Material.Material> materials)
        {
            for (int i = 0, row = _startRow + 1;  i < materials.Count; i++, row++)
            {
                if ((i == 0) || (materials[i].Category != materials[i - 1].Category))
                {
                    var categoryCell = ColumnIndexToColumnLetter(1) + row;
                    worksheet.Cells[categoryCell].Value = materials[i].Category;
                    worksheet.Cells[categoryCell].Style.Font.Bold = true;
                    row++;
                }

                var descriptionCell = ColumnIndexToColumnLetter(2) + row;
                worksheet.Cells[descriptionCell].Value = materials[i].ShortDescription;
                worksheet.Cells[descriptionCell].Style.Font.Bold = true;
                worksheet.Cells[descriptionCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private void FormatServantInputSection(ExcelWorksheet worksheet, bool inputLocked, int startRow)
        {
            string idealCellTotalFormula, stockCellTotalFormula, usedCellTotalFormula, detachedCellTotalFormula;
            idealCellTotalFormula = stockCellTotalFormula = usedCellTotalFormula = detachedCellTotalFormula = "0";
            
            for (int i = 0, row = startRow + 1; i < _gradeDescriptions.Count + 1; i++, row++)
            {
                var startColumn = _startColumn;

                // ideal
                var idealCell = ColumnIndexToColumnLetter(startColumn) + row;
                if (worksheet.Name == CumulatedSheetDescription)
                {                    
                    worksheet.Cells[idealCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[idealCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    startColumn++;
                }

                // stock
                var stockCell = ColumnIndexToColumnLetter(startColumn) + row;
                worksheet.Cells[stockCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[stockCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                
                // used
                var usedCell = ColumnIndexToColumnLetter(startColumn + 1) + row;
                worksheet.Cells[usedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[usedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // detached
                var detachedCell = ColumnIndexToColumnLetter(startColumn + 2) + row;
                worksheet.Cells[detachedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[detachedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // available
                var availableCell = ColumnIndexToColumnLetter(startColumn + 3) + row;
                worksheet.Cells[availableCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[availableCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[availableCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[availableCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));

                // fill worksheet with name "Total" with formulas
                if (worksheet.Name == CumulatedSheetDescription)
                {
                    worksheet.Cells[stockCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn - 1) + row))));
                    worksheet.Cells[usedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn) + row))));
                    worksheet.Cells[detachedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 1) + row))));
                }

                // last row (sum up servant quantities per worksheet)
                if (i == _gradeDescriptions.Count)
                {
                    worksheet.Cells[idealCell].Formula = string.Format("sum({0})", idealCellTotalFormula);
                    worksheet.Cells[stockCell].Formula = string.Format("sum({0})", stockCellTotalFormula); // overwrites ideal cell formula as long as worksheet name is not "Total"
                    worksheet.Cells[usedCell].Formula = string.Format("sum({0})", usedCellTotalFormula);
                    worksheet.Cells[detachedCell].Formula = string.Format("sum({0})", detachedCellTotalFormula);
                } else
                {
                    idealCellTotalFormula += "," + idealCell;
                    stockCellTotalFormula += "," + stockCell;
                    usedCellTotalFormula += "," + usedCell;
                    detachedCellTotalFormula += "," + detachedCell;
                }

                // last column (availability per grade)
                worksheet.Cells[availableCell].Formula = string.Format("sum({0},sum({1})*(-1),sum({2})*(-1))", stockCell, usedCell, detachedCell);

                worksheet.Cells[stockCell].Style.Locked = inputLocked;
                worksheet.Cells[usedCell].Style.Locked = inputLocked;
                worksheet.Cells[detachedCell].Style.Locked = inputLocked;
            }
        }

        private void FormatMaterialInputSection(ExcelWorksheet worksheet, IReadOnlyList<Material.Material> materials, bool inputLocked, int startRow)
        {
            for (int i = 0, row = startRow + 1; i < materials.Count; i++, row++)
            {
                var startColumn = _startColumn;

                if ((i == 0) || (materials[i].Category != materials[i - 1].Category))
                {
                    row++;
                }

                if (worksheet.Name == CumulatedSheetDescription)
                {
                    startColumn++;
                }

                // stock
                var stockCell = ColumnIndexToColumnLetter(startColumn) + row;
                worksheet.Cells[stockCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[stockCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // used
                var usedCell = ColumnIndexToColumnLetter(startColumn + 1) + row;
                worksheet.Cells[usedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[usedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // damaged
                var damagedCell = ColumnIndexToColumnLetter(startColumn + 2) + row;
                worksheet.Cells[damagedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[damagedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // available
                var availableCell = ColumnIndexToColumnLetter(startColumn + 3) + row;
                worksheet.Cells[availableCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[availableCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[availableCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[availableCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));

                // fill worksheet with name "Total" with formulas
                if (worksheet.Name == CumulatedSheetDescription)
                {
                    worksheet.Cells[stockCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn - 1) + row))));
                    worksheet.Cells[usedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn) + row))));
                    worksheet.Cells[damagedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 1) + row))));
                }

                // last column (availability per material)
                worksheet.Cells[availableCell].Formula = string.Format("sum({0},sum({1})*(-1),sum({2})*(-1))", stockCell, usedCell, damagedCell);

                worksheet.Cells[stockCell].Style.Locked = false;
                worksheet.Cells[usedCell].Style.Locked = false;
                worksheet.Cells[damagedCell].Style.Locked = false;
            }
        }

        private static void ProtectWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.AllowSelectLockedCells = true;
        }
    }
}