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

        // TODO @Klamir refactor so that the values are passed along 
        private int _startRow, _startColumn;

        private static readonly List<string> GradeDescriptions = SortGradeList((Grade[])Enum.GetValues(typeof(Grade)));

        public void GenerateCompanyTemplate(ExcelPackage package, Company company, List<Material.Material> materials)
        {
            // 1. Create excel worksheets (one foreach location and total, default location will be the second worksheet right after the cumulated sheet)
            var worksheets = GenerateWorksheets(package, company);
            var sortedMaterialList = SortMaterialList(materials);

            foreach (var worksheet in worksheets)
            {
                // 2. Define start row and start column
                _startRow = 1;
                _startColumn = 4;

                // 2. For each worksheet create a worksheet overall title and the servant subtitle
                InsertWorksheetTitle(worksheet, $"Dispoliste {company.Name}, {(worksheet.Name == CumulatedSheetDescription || worksheet.Name == company.DefaultLocation.Name ? worksheet.Name : $"Standort {worksheet.Name}")}", 1, 18);
                InsertWorksheetTitle(worksheet, "Personal", 0, 14);

                // 3. For each worksheet insert grade list and according columns
                var startServantList = _startRow;
                InsertServantSectionColumns(worksheet, worksheet.Name == CumulatedSheetDescription ? ServantSectionColumnsTotal : ServantSectionColumns);
                InsertServantSectionRows(worksheet);

                // 4. For each worksheet create the material subtitle
                InsertWorksheetTitle(worksheet, "Material", 0, 14);

                // 5. For each worksheet insert material list and according columns
                var startMaterialList = _startRow;
                InsertMaterialSectionColumns(worksheet, MaterialSectionColumns, worksheet.Name == CumulatedSheetDescription ? _startColumn + 1 : _startColumn);
                InsertMaterialSectionRows(worksheet, sortedMaterialList);

                // 7. For each worksheet format input section, add formulas where necessary and unlock certain cells within each worksheet
                FormatServantInputSection(worksheet, false, startServantList);
                FormatMaterialInputSection(worksheet,  sortedMaterialList,false, startMaterialList);

                // 8. Lock the workbook totally (no password required to unlock the worksheets)
                ProtectWorksheet(worksheet);

                // 9. Insert headers and footers
                InsertHeaderFooter(worksheet, worksheet.Name);

                // 10 For each worksheet set column widhts
                SetColumnWidths(worksheet);

                // 11. Printer settings
                ApplyPrinterSettings(worksheet, eOrientation.Portrait, 1, _startRow - 1);
            }
        }

        private static IEnumerable<ExcelWorksheet> GenerateWorksheets(ExcelPackage package, Company company)
        {
            var locations = company.Locations.Select(location => location.Name).ToList();
            locations.Remove(company.DefaultLocation.Name);

            var worksheets = new List<ExcelWorksheet>
            {
                package.Workbook.Worksheets.Add(CumulatedSheetDescription),
                package.Workbook.Worksheets.Add(company.DefaultLocation.Name)
            };
            locations.ForEach(location => worksheets.Add(package.Workbook.Worksheets.Add(location)));

            return worksheets;
        }

        private void InsertWorksheetTitle(ExcelWorksheet worksheet, string worksheetTitle, int spaceAfter, int fontSize)
        {
            var titleCell = ColumnIndexToColumnLetter(1) + _startRow;
            worksheet.Cells[titleCell].Value = worksheetTitle;
            //worksheet.Cells[titleCell].Style.Locked = false;
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
            foreach (var gradeDescription in GradeDescriptions)
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
            string idealCellTotalFormula, stockCellTotalFormula, usedCellTotalFormula, detachedCellTotalFormula, availableCellTotalFormual;
            idealCellTotalFormula = stockCellTotalFormula = usedCellTotalFormula = detachedCellTotalFormula = availableCellTotalFormual = "0";
            
            for (int i = 0, row = startRow + 1; i < GradeDescriptions.Count + 1; i++, row++)
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

                if (i == GradeDescriptions.Count)
                {
                    worksheet.Cells[idealCell].Formula = idealCellTotalFormula;
                    worksheet.Cells[stockCell].Formula = stockCellTotalFormula; // overwrites ideal cell formula as long as worksheet name is not "Total"
                    worksheet.Cells[usedCell].Formula = usedCellTotalFormula;
                    worksheet.Cells[detachedCell].Formula = detachedCellTotalFormula;
                    worksheet.Cells[availableCell].Formula = availableCellTotalFormual;
                } else
                {
                    idealCellTotalFormula += " + " + idealCell;
                    stockCellTotalFormula += " + " + stockCell;
                    usedCellTotalFormula += " + " + usedCell;
                    detachedCellTotalFormula += " + " + detachedCell;
                    availableCellTotalFormual += " + " + availableCell;
                }

                worksheet.Cells[availableCell].Formula = string.Format("{0}-{1}-{2}", stockCell, usedCell, detachedCell);

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

                var stockCell = ColumnIndexToColumnLetter(startColumn) + row;
                var usedCell = ColumnIndexToColumnLetter(startColumn + 1) + row;
                var damagedCell = ColumnIndexToColumnLetter(startColumn + 2) + row;
                var availableCell = ColumnIndexToColumnLetter(startColumn + 3) + row;

                worksheet.Cells[availableCell].Formula = $"{stockCell}-{usedCell}-{damagedCell}";

                worksheet.Cells[stockCell].Style.Locked = false;
                worksheet.Cells[usedCell].Style.Locked = false;
                worksheet.Cells[damagedCell].Style.Locked = false;

                worksheet.Cells[stockCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[stockCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[usedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[usedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[damagedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[damagedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[availableCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[availableCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[availableCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[availableCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
            }
        }

        private static void ProtectWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.AllowSelectLockedCells = true;
        }

        private static void SetColumnWidths(ExcelWorksheet worksheet)
        {
            worksheet.Column(1).Width = 1.43; // first column has a width of 1.43
            worksheet.Column(2).AutoFit(); // grade resp. material category or material column is of type auto size
            worksheet.Column(3).Width = 2.95; // fîrst empty column has a widht of 2.95
        }
    }
}
