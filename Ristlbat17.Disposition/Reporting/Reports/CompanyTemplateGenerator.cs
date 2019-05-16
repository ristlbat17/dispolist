using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
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

        private ExcelWorksheets _worksheets;
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
                InsertServantSectionColumns(worksheet);
                InsertServantSectionRows(worksheet);

                // 5. For each worksheet create the material subtitle
                InsertWorksheetTitle(worksheet, "Material", 0, 14);

                // 6. For each worksheet insert material list and according columns
                var startMaterialList = _startRow;
                InsertMaterialSectionColumns(worksheet, _startColumn);
                InsertMaterialSectionRows(worksheet);

                // 7. For each worksheet format input section, add formulas where necessary and unlock certain cells within each worksheet
                FormatServantInputSection(worksheet, startServantList);
                FormatMaterialInputSection(worksheet, startMaterialList);

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

        private ExcelWorksheets GenerateWorksheets(ExcelPackage package)
        {
            var sortedCompanyLocations = SortCompanyLocations(_company.Locations.Select(location => location.Name).ToList());
            sortedCompanyLocations.Remove(_company.DefaultLocation.Name);

            var worksheets = package.Workbook.Worksheets;
            worksheets.Add(CumulatedSheetDescription);
            worksheets.Add(_company.DefaultLocation.Name);
            sortedCompanyLocations.ForEach(location => worksheets.Add(location));

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

        private void InsertServantSectionColumns(ExcelWorksheet worksheet)
        {
            for (var i = 0; i < ServantSectionColumns.Count; i++)
            {
                var headerCell = ColumnIndexToColumnLetter(_startColumn + i) + _startRow;
                worksheet.Cells[headerCell].Value = ServantSectionColumns[i];
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

        private void InsertMaterialSectionColumns(ExcelWorksheet worksheet, int startColumn)
        {
            for (var i = 0; i < MaterialSectionColumns.Count; i++)
            {
                var headerCell = ColumnIndexToColumnLetter(startColumn + i) + _startRow;
                worksheet.Cells[headerCell].Value = MaterialSectionColumns[i];
                worksheet.Cells[headerCell].Style.Font.Bold = true;
                worksheet.Cells[headerCell].Style.TextRotation = 90;
                worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Column(startColumn + i).Width = 3.8;
            }
        }

        private void InsertMaterialSectionRows(ExcelWorksheet worksheet)
        {
            for (int i = 0, row = _startRow + 1;  i < _materials.Count; i++, row++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    var categoryCell = ColumnIndexToColumnLetter(1) + row;
                    worksheet.Cells[categoryCell].Value = _materials[i].Category;
                    worksheet.Cells[categoryCell].Style.Font.Bold = true;
                    row++;
                }

                var descriptionCell = ColumnIndexToColumnLetter(2) + row;
                worksheet.Cells[descriptionCell].Value = _materials[i].ShortDescription;
                worksheet.Cells[descriptionCell].Style.Font.Bold = true;
                worksheet.Cells[descriptionCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private void FormatServantInputSection(ExcelWorksheet worksheet, int startRow)
        {
            string stockCellTotalFormula, usedCellTotalFormula, detachedCellTotalFormula;
            stockCellTotalFormula = usedCellTotalFormula = detachedCellTotalFormula = "0";
            
            for (int i = 0, row = startRow + 1; i < _gradeDescriptions.Count + 1; i++, row++)
            {
                var startColumn = _startColumn;

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
                if (string.Equals(worksheet.Name, CumulatedSheetDescription))
                {
                    worksheet.Cells[stockCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn) + row))));
                    worksheet.Cells[usedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 1) + row))));
                    worksheet.Cells[detachedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 2) + row))));
                }

                // last row (sum up servant quantities per worksheet)
                if (i == _gradeDescriptions.Count)
                {
                    worksheet.Cells[stockCell].Formula = string.Format("sum({0})", stockCellTotalFormula);
                    worksheet.Cells[usedCell].Formula = string.Format("sum({0})", usedCellTotalFormula);
                    worksheet.Cells[detachedCell].Formula = string.Format("sum({0})", detachedCellTotalFormula);
                } else
                {
                    stockCellTotalFormula += "," + stockCell;
                    usedCellTotalFormula += "," + usedCell;
                    detachedCellTotalFormula += "," + detachedCell;
                }

                // last column (availability per grade)
                worksheet.Cells[availableCell].Formula = string.Format("sum({0},sum({1})*(-1),sum({2})*(-1))", stockCell, usedCell, detachedCell);

                // unlock stock, used and detached cell only if worksheet name is not "Total" / lock last column (availability per grade) in every case
                worksheet.Cells[stockCell].Style.Locked = worksheet.Cells[usedCell].Style.Locked = worksheet.Cells[detachedCell].Style.Locked 
                    = string.Equals(worksheet.Name, CumulatedSheetDescription) || i == _gradeDescriptions.Count;

                // data validation

                var stockCellValidation = worksheet.DataValidations.AddCustomValidation(stockCell);
                var usedCellValidation = worksheet.DataValidations.AddCustomValidation(usedCell);
                var detachedCellValidation = worksheet.DataValidations.AddCustomValidation(detachedCell);

                stockCellValidation.ErrorStyle = usedCellValidation.ErrorStyle = detachedCellValidation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
                stockCellValidation.ErrorTitle = usedCellValidation.ErrorTitle = detachedCellValidation.ErrorTitle = "an invalid value was entered";
                stockCellValidation.ShowErrorMessage = usedCellValidation.ShowErrorMessage = detachedCellValidation.ShowErrorMessage = true;
                stockCellValidation.Operator = usedCellValidation.Operator = detachedCellValidation.Operator = ExcelDataValidationOperator.equal; // seems to be a bug, you cannot use a formula as long as the operator is set to between which is default setting

                stockCellValidation.Error = "stock must be an integer greater than zero and must be greater than or equal to the sum of used and detached";                
                stockCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({0}),{0}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, detachedCell);

                usedCellValidation.Error = "used must be an integer greater than zero and must be less or equal to the difference of stock and detached";
                usedCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({1}),{1}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, detachedCell);

                detachedCellValidation.Error = "detached must be an integer greater than zero and must be less or equal to the difference of stock and used";
                detachedCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({2}),{2}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, detachedCell);
            }
        }

        private void FormatMaterialInputSection(ExcelWorksheet worksheet, int startRow)
        {
            for (int i = 0, row = startRow + 1; i < _materials.Count; i++, row++)
            {
                var startColumn = _startColumn;

                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    row++;
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
                    worksheet.Cells[stockCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn) + row))));
                    worksheet.Cells[usedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 1) + row))));
                    worksheet.Cells[damagedCell].Formula = string.Format("if(sum({0})=0,\"\",sum({0}))", string.Join(",", _worksheets.Where(ws => !string.Equals(ws.Name, CumulatedSheetDescription)).Select(ws => string.Format("'{0}'!{1}", ws.Name, ColumnIndexToColumnLetter(startColumn + 2) + row))));
                }

                // last column (availability per material)
                worksheet.Cells[availableCell].Formula = string.Format("sum({0},sum({1})*(-1),sum({2})*(-1))", stockCell, usedCell, damagedCell);

                // unlock stock, used and damaged cell only if worksheet name is not "Total"
                worksheet.Cells[stockCell].Style.Locked = worksheet.Cells[usedCell].Style.Locked = worksheet.Cells[damagedCell].Style.Locked
                    = string.Equals(worksheet.Name, CumulatedSheetDescription);

                // data validation

                var stockCellValidation = worksheet.DataValidations.AddCustomValidation(stockCell);
                var usedCellValidation = worksheet.DataValidations.AddCustomValidation(usedCell);
                var damagedCellValidation = worksheet.DataValidations.AddCustomValidation(damagedCell);

                stockCellValidation.ErrorStyle = usedCellValidation.ErrorStyle = damagedCellValidation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
                stockCellValidation.ErrorTitle = usedCellValidation.ErrorTitle = damagedCellValidation.ErrorTitle = "an invalid value was entered";
                stockCellValidation.ShowErrorMessage = usedCellValidation.ShowErrorMessage = damagedCellValidation.ShowErrorMessage = true;
                stockCellValidation.Operator = usedCellValidation.Operator = damagedCellValidation.Operator = ExcelDataValidationOperator.equal;

                stockCellValidation.Error = "stock must be an integer greater than zero and must be greater than or equal to the sum of used and damaged";
                stockCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({0}),{0}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, damagedCell);

                usedCellValidation.Error = "used must be an integer greater than zero and must be less or equal to the difference of stock and damaged";
                usedCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({1}),{1}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, damagedCell);

                damagedCellValidation.Error = "damaged must be an integer greater than zero and must be less or equal to the difference of stock and used";
                damagedCellValidation.Formula.ExcelFormula = string.Format("=and(isnumber({2}),{2}>=0,{0}>=sum({1},{2}))", stockCell, usedCell, damagedCell);
            }
        }

        private void ProtectWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.AllowSelectLockedCells = false;
        }
    }
}