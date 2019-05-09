using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition.Reporting.Reports
{
    enum ReportType
    {
        LevelBataillon,
        LevelCompany
    };

    public class BataillonOverviewReporter : DispositionListReporter
    {
        private const string CumulatedSheetDescription = "Bat";
        private const string CumulatedColumnDescription = "Total";

        private int _startRow;
        private readonly int _startColumn = 4;

        private List<string> _companyNames;
        private Dictionary<string, List<string>> _companyLocations;
        private List<string> _gradeDescriptions;
        private List<ServantReportItemBataillon> _servantReportItems;
        private List<Material.Material> _materials;
        private List<MaterialReportItemBataillon> _materialReportItems;

        private readonly IMaterialDispositionContext _context;

        public BataillonOverviewReporter(IMaterialDispositionContext context)
        {
            _context = context;               
        }

        public string GenerateBataillonOverviewReport(ExcelPackage package, string reportId)
        {
            var inventoryReport = _context.DispositionReport.Find(report => report.Id == reportId).First();
            _materialReportItems = inventoryReport.MaterialReportItems;
            _servantReportItems = inventoryReport.ServantReportItems;

            /*
             * 1. Get data out of report items 
            */

            // 1.1 Get company names out of report items
            _companyNames = new List<string>();
            _servantReportItems.ForEach(servantReportItem => servantReportItem.PerCompany.ForEach(perCompany => _companyNames.Add(perCompany.Company)));
            _materialReportItems.ForEach(materialReportItem => materialReportItem.PerCompany.ForEach(perCompany => _companyNames.Add(perCompany.Company)));
            _companyNames = SortCompanyNames(_companyNames.Distinct().ToList());
            _companyNames.Add(CumulatedSheetDescription);

            // 1.2 Get company locations out of report items
            _companyLocations = new Dictionary<string, List<string>>();
            _companyNames.Where(companyName => !string.Equals(companyName, CumulatedSheetDescription)).ToList().ForEach(companyName => _companyLocations.Add(companyName, new List<string>()));
            _companyNames.ForEach(companyName => _servantReportItems.SelectMany(servantReportItems => servantReportItems.PerCompany.Where(perCompany => string.Equals(perCompany.Company, companyName))).SelectMany(perCompany => perCompany.PerLocation).ToList().ForEach(perLocation => _companyLocations[companyName].Add(perLocation.Location)));
            _companyNames.ForEach(companyName => _materialReportItems.SelectMany(materialReportItems => materialReportItems.PerCompany.Where(perCompany => string.Equals(perCompany.Company, companyName))).SelectMany(perCompany => perCompany.PerLocation).ToList().ForEach(perLocation => _companyLocations[companyName].Add(perLocation.Location)));
            _companyLocations.Keys.ToList().ForEach(Key => _companyLocations[Key] = SortCompanyLocations(_companyLocations[Key].Distinct().ToList()));
            _companyLocations.Keys.ToList().ForEach(Key => _companyLocations[Key].Add(CumulatedColumnDescription));

            // 1.3 Get grades out of report items
            _gradeDescriptions = SortGradeList(_servantReportItems.Select(servantReportItem => servantReportItem.Grade).ToArray());

            // 1.4 Get material list out of report items
            _materials = SortMaterialList(_materialReportItems.Select(materialReportItem => materialReportItem.Material).ToList());

            /*
             * 2. Create one worksheet per company and a cumulated worksheet
            */

            var worksheets = GenerateWorkSheets(package);

            /*
             * 3. Fill cumulated worksheet
             */

            var cumulatedWorksheet = worksheets.Where(worksheet => string.Equals(worksheet.Name, CumulatedSheetDescription)).First();

            // 3.1 Reinitialize start row
            _startRow = 1;

            // 3.2 Overall title row
            InsertWorksheetTitle(cumulatedWorksheet, "Dispoliste Ristl Bat 17", 1, 18);

            // 3.3 Insert all companies
            InsertHeaderSection(cumulatedWorksheet, _companyNames, ServantSectionColumnsTotal, 1);

            // 3.4 Subtitle row
            InsertWorksheetTitle(cumulatedWorksheet, "Personal", 0, 14);

            // 3.5 Insert Grade list and according columns
            var startServantList = _startRow;
            InsertServantSectionColumns(cumulatedWorksheet, _companyNames, ServantSectionColumnsTotal);
            InsertServantSectionRows(cumulatedWorksheet);

            // 3.6 Subtitle row
            InsertWorksheetTitle(cumulatedWorksheet, "Material", 0, 14);

            // 3.7 Insert material list and according columns
            var startMaterialList = _startRow;
            InsertMaterialSectionColumns(cumulatedWorksheet, _companyNames, _startColumn + 1, 2);
            InsertMaterialSectionRows(cumulatedWorksheet);

            // 3.8 Insert servant inventory data
            InsertServantInventoryDataBataillon(cumulatedWorksheet, startServantList);

            // 3.9 Insert material inventory data
            InsertMaterialInventoryDataBataillon(cumulatedWorksheet, startMaterialList, _startColumn + 1);

            /*
             * 4. Fill each companie's worksheet
            */

            var companyWorksheets = worksheets.Where(worksheet => !string.Equals(worksheet.Name, CumulatedSheetDescription)).ToList();

            foreach (ExcelWorksheet companyWorksheet in companyWorksheets)
            {
                // 4.1 Reinitialize start row
                _startRow = 1;

                // 4.2 Overall title row
                InsertWorksheetTitle(companyWorksheet, "Dispoliste Ristl " + companyWorksheet.Name, 1, 18);

                // 4.3 Insert all locations
                InsertHeaderSection(companyWorksheet, _companyLocations[companyWorksheet.Name], ServantSectionColumns, 1);

                // 4.4 Subtitle row
                InsertWorksheetTitle(companyWorksheet, "Personal", 0, 14);

                // 4.5 Insert Grade list and according columns
                startServantList = _startRow;
                InsertServantSectionColumns(companyWorksheet, _companyLocations[companyWorksheet.Name], ServantSectionColumns);
                InsertServantSectionRows(companyWorksheet);

                // 4.6 Subtitle row
                InsertWorksheetTitle(companyWorksheet, "Material", 0, 14);

                // 4.7 Insert material list and according columns
                startMaterialList = _startRow;
                InsertMaterialSectionColumns(companyWorksheet, _companyLocations[companyWorksheet.Name], _startColumn, 1);
                InsertMaterialSectionRows(companyWorksheet);

                // 4.8 Insert servant inventory data
                InsertServantInventoryDataCompany(companyWorksheet, startServantList);

                // 4.9 Insert material inventory data
                InsertMaterialInventoryDataCompany(companyWorksheet, startMaterialList, _startColumn);
            }

            /*
             * 5. Format each worksheet
            */

            // 5.1 Set column widhts
            worksheets.ToList().ForEach(worksheet => SetColumnWidths(worksheet));

            // 5.2 Insert headers and footers
            InsertHeaderFooter(cumulatedWorksheet, "Dispoliste Ristl Bat 17", inventoryReport.ReportDate);
            companyWorksheets.ForEach(companyWorksheet => InsertHeaderFooter(companyWorksheet, "Dispoliste Ristl " + companyWorksheet.Name, inventoryReport.ReportDate));

            // 5.3 Printer settings
            worksheets.ToList().ForEach(worksheet => ApplyPrinterSettings(worksheet, eOrientation.Landscape, 1, 1));

            return ConvertUtcTimestamp(inventoryReport.ReportDate).datetime;
        }

        private ExcelWorksheets GenerateWorkSheets(ExcelPackage package)
        {
            var worksheets = package.Workbook.Worksheets;
            worksheets.Add(CumulatedSheetDescription); // Cumulated worksheet comes first
            _companyNames.Where(companyName => !string.Equals(companyName, CumulatedSheetDescription)).ToList().ForEach(companyName => worksheets.Add(companyName));
            worksheets.ToList().ForEach(worksheet => worksheet.Cells.Style.Font.Size = 10);
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

        private void InsertHeaderSection(ExcelWorksheet worksheet, List<string> headerSection, List<string> servantSectionColumns, int spaceAfter)
        {
            // Iterate over all companies or company locations and for each company resp. location insert its header
            for (int i = 0, column = _startColumn; i < headerSection.Count; i++, column += (servantSectionColumns.Count + 1))
            {
                var companyCell = ColumnIndexToColumnLetter(column) + _startRow;
                worksheet.Cells[companyCell].Value = headerSection[i];
                worksheet.Cells[companyCell + ":" + ColumnIndexToColumnLetter(column + (servantSectionColumns.Count - 1)) + _startRow].Merge = true;
                worksheet.Cells[companyCell].Style.Font.Bold = true;
                worksheet.Cells[companyCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[companyCell + ":" + ColumnIndexToColumnLetter(column + (servantSectionColumns.Count - 1)) + _startRow].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[ColumnIndexToColumnLetter(column + (servantSectionColumns.Count - 1)) + _startRow].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[companyCell + ":" + ColumnIndexToColumnLetter(column + (servantSectionColumns.Count - 1)) + _startRow].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[companyCell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            _startRow += spaceAfter + 1;
        }

        private void InsertServantSectionColumns(ExcelWorksheet worksheet, List<string> headerSection, List<string> servantSectionColumns)
        {
            for (int i = 0, column = _startColumn; i < headerSection.Count; i++, column += (servantSectionColumns.Count + 1))
            {
                worksheet.Column(column - 1).Width = 2.95;

                for (var j = 0; j < servantSectionColumns.Count; j++)
                {
                    var headerCell = ColumnIndexToColumnLetter(column + j) + _startRow;
                    worksheet.Cells[headerCell].Value = servantSectionColumns[j];
                    worksheet.Cells[headerCell].Style.Font.Bold = true;
                    worksheet.Cells[headerCell].Style.TextRotation = 90;
                    worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Column(column + j).Width = 3.8;
                }
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

        private void InsertMaterialSectionColumns(ExcelWorksheet worksheet, List<string> headerSection, int startColumn, int spaceBetween)
        {
            for (var i = 0; i < headerSection.Count; i++, startColumn += (MaterialSectionColumns.Count + spaceBetween))
            {
                for (var j = 0; j < MaterialSectionColumns.Count; j++)
                {
                    var headerCell = ColumnIndexToColumnLetter(startColumn + j) + _startRow;
                    worksheet.Cells[headerCell].Value = MaterialSectionColumns[j];
                    worksheet.Cells[headerCell].Style.Font.Bold = true;
                    worksheet.Cells[headerCell].Style.TextRotation = 90;
                    worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
        }

        private void InsertMaterialSectionRows(ExcelWorksheet worksheet)
        {
            for (var i = 0; i < _materials.Count; i++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    var categoryCell = ColumnIndexToColumnLetter(1) + (++_startRow);
                    worksheet.Cells[categoryCell].Value = _materials[i].Category;
                    worksheet.Cells[categoryCell].Style.Font.Bold = true;
                }

                var descriptionCell = ColumnIndexToColumnLetter(2) + (++_startRow);
                worksheet.Cells[descriptionCell].Value = _materials[i].ShortDescription;
                worksheet.Cells[descriptionCell].Style.Font.Bold = true;
                worksheet.Cells[descriptionCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private void InsertServantInventoryDataBataillon(ExcelWorksheet worksheet, int startRow)
        {
            for (int i = 0, row = startRow + 1; i < _gradeDescriptions.Count; i++, row++)
            {
                var currentServantReportItem = _servantReportItems.Find(servantReportItem => servantReportItem.Grade == _gradeDescriptions[i].GetValueFromDescription<Grade>());

                int totalIdeal, totalStock, totalUsed, totalDetached;
                totalIdeal = totalStock = totalUsed = totalDetached = 0;

                // Calculate and insert servant inventory data for all companies and for bataillon
                for (int j = 0, column = _startColumn; j < _companyNames.Count; j++, column += (ServantSectionColumnsTotal.Count + 1))
                {
                    var currentCompany = currentServantReportItem.PerCompany.Find(company => company.Company == _companyNames[j]);
                    var idealCell = ColumnIndexToColumnLetter(column) + row;
                    var stockCell = ColumnIndexToColumnLetter(column + 1) + row;
                    var usedCell = ColumnIndexToColumnLetter(column + 2) + row;
                    var detachedCell = ColumnIndexToColumnLetter(column + 3) + row;
                    var availableCell = ColumnIndexToColumnLetter(column + 4) + row;

                    if (currentCompany != null)
                    {
                        worksheet.Cells[idealCell].Value = currentCompany.Ideal > 0 ? currentCompany.Ideal : (int?)null; // OTF, Soll
                        totalIdeal += currentCompany.Ideal;

                        worksheet.Cells[stockCell].Value = currentCompany.Stock > 0 ? currentCompany.Stock : (int?)null; // Bestand
                        totalStock += currentCompany.Stock;

                        worksheet.Cells[usedCell].Value = currentCompany.Used > 0 ? currentCompany.Used : (int?)null; // Eingesetzt
                        totalUsed += currentCompany.Used;

                        worksheet.Cells[detachedCell].Value = currentCompany.Detached > 0 ? currentCompany.Detached : (int?)null; // Detachiert
                        totalDetached += currentCompany.Detached;

                        worksheet.Cells[availableCell].Value = currentCompany.Stock > 0 ? (currentCompany.Stock - currentCompany.Used - currentCompany.Detached) : (int?)null; // Verfügbar
                    }

                    if (j == (_companyNames.Count - 1))
                    {
                        worksheet.Cells[idealCell].Value = totalIdeal > 0 ? totalIdeal : (int?)null; // OTF, Soll
                        worksheet.Cells[stockCell].Value = totalStock > 0 ? totalStock : (int?)null; // Bestand
                        worksheet.Cells[usedCell].Value = totalUsed > 0 ? totalUsed : (int?)null; // Eingesetzt
                        worksheet.Cells[detachedCell].Value = totalDetached > 0 ? totalDetached : (int?)null; // Detachiert
                        worksheet.Cells[availableCell].Value = totalStock > 0 ? (totalStock - totalUsed - totalDetached) : (int?)null; // Verfügbar
                    }

                    worksheet.Cells[idealCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[idealCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[stockCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[stockCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[usedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[usedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[detachedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[detachedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[availableCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[availableCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[availableCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[availableCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));

                    if (i == _gradeDescriptions.Count - 1)
                    {
                        var idealTotalCell = ColumnIndexToColumnLetter(column) + (row + 1);
                        var stockTotalCell = ColumnIndexToColumnLetter(column + 1) + (row + 1);
                        var usedTotalCell = ColumnIndexToColumnLetter(column + 2) + (row + 1);
                        var detachedTotalCell = ColumnIndexToColumnLetter(column + 3) + (row + 1);
                        var availableTotalCell = ColumnIndexToColumnLetter(column + 4) + (row + 1);

                        worksheet.Cells[idealTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column) + (startRow + 1)}:{ColumnIndexToColumnLetter(column) + row})";
                        worksheet.Cells[stockTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 1) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 1) + row})";
                        worksheet.Cells[usedTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 2) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 2) + row})";
                        worksheet.Cells[detachedTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 3) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 3) + row})";
                        worksheet.Cells[availableTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 4) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 4) + row})";

                        worksheet.Cells[idealTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[idealTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[idealTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[idealTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[stockTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[stockTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[stockTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[stockTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[usedTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[usedTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[usedTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[usedTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[detachedTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[detachedTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[detachedTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[detachedTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[availableTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[availableTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[availableTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[availableTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                    }
                }
            }
        }

        private void InsertServantInventoryDataCompany(ExcelWorksheet worksheet, int startRow)
        {
            for (int i = 0, row = startRow + 1; i < _gradeDescriptions.Count; i++, row++)
            {
                var currentServantReportItem = _servantReportItems.Find(servantReportItem => servantReportItem.Grade == _gradeDescriptions[i].GetValueFromDescription<Grade>()).PerCompany.Where(perCompany => string.Equals(perCompany.Company, worksheet.Name)).First();

                int totalStock, totalUsed, totalDetached;
                totalStock = totalUsed = totalDetached = 0;

                // Calculate and insert servant inventory data for all company locations and for total column
                for (int j = 0, column = _startColumn; j < _companyLocations[worksheet.Name].Count; j++, column += (ServantSectionColumns.Count + 1))
                {
                    var currentLocation = currentServantReportItem.PerLocation.Find(perLocation => perLocation.Location == _companyLocations[worksheet.Name][j]);
                    var stockCell = ColumnIndexToColumnLetter(column) + row;
                    var usedCell = ColumnIndexToColumnLetter(column + 1) + row;
                    var detachedCell = ColumnIndexToColumnLetter(column + 2) + row;
                    var availableCell = ColumnIndexToColumnLetter(column + 3) + row;

                    if (currentLocation != null)
                    {
                        worksheet.Cells[stockCell].Value = currentLocation.Stock > 0 ? currentLocation.Stock : (int?)null; // Bestand
                        totalStock += currentLocation.Stock;

                        worksheet.Cells[usedCell].Value = currentLocation.Used > 0 ? currentLocation.Used : (int?)null; // Eingesetzt
                        totalUsed += currentLocation.Used;

                        worksheet.Cells[detachedCell].Value = currentLocation.Detached > 0 ? currentLocation.Detached : (int?)null; // Detachiert
                        totalDetached += currentLocation.Detached;

                        worksheet.Cells[availableCell].Value = currentLocation.Stock > 0 ? (currentLocation.Stock - currentLocation.Used - currentLocation.Detached) : (int?)null; // Verfügbar
                    }

                    if (j == (_companyLocations[worksheet.Name].Count - 1))
                    {
                        worksheet.Cells[stockCell].Value = totalStock > 0 ? totalStock : (int?)null; // Bestand
                        worksheet.Cells[usedCell].Value = totalUsed > 0 ? totalUsed : (int?)null; // Eingesetzt
                        worksheet.Cells[detachedCell].Value = totalDetached > 0 ? totalDetached : (int?)null; // Detachiert
                        worksheet.Cells[availableCell].Value = totalStock > 0 ? (totalStock - totalUsed - totalDetached) : (int?)null; // Verfügbar
                    }

                    worksheet.Cells[stockCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[stockCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[usedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[usedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[detachedCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[detachedCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[availableCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[availableCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[availableCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[availableCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));

                    if (i == _gradeDescriptions.Count - 1)
                    {
                        var stockTotalCell = ColumnIndexToColumnLetter(column) + (row + 1);
                        var usedTotalCell = ColumnIndexToColumnLetter(column + 1) + (row + 1);
                        var detachedTotalCell = ColumnIndexToColumnLetter(column + 2) + (row + 1);
                        var availableTotalCell = ColumnIndexToColumnLetter(column + 3) + (row + 1);

                        worksheet.Cells[stockTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column) + (startRow + 1)}:{ColumnIndexToColumnLetter(column) + row})";
                        worksheet.Cells[usedTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 1) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 1) + row})";
                        worksheet.Cells[detachedTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 2) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 2) + row})";
                        worksheet.Cells[availableTotalCell].Formula = $"SUM({ColumnIndexToColumnLetter(column + 3) + (startRow + 1)}:{ColumnIndexToColumnLetter(column + 3) + row})";

                        worksheet.Cells[stockTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[stockTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[stockTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[stockTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[usedTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[usedTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[usedTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[usedTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[detachedTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[detachedTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[detachedTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[detachedTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                        worksheet.Cells[availableTotalCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[availableTotalCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[availableTotalCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[availableTotalCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(211, 211, 211));
                    }
                }
            }



            /*
            for (int i = 0, row = startRow + 1; i < _gradeDescriptions.Count; i++, row++)
            {
                var currentServantReportItem = _servantReportItems.Find(servantReportItem => servantReportItem.Grade == _gradeDescriptions[i].GetValueFromDescription<Grade>());

                int totalIdeal, totalStock, totalUsed, totalDetached;
                totalIdeal = totalStock = totalUsed = totalDetached = 0;

                // Calculate and insert servant inventory data for all companies and for bataillon
                for (int j = 0, column = _startColumn; j < _companyNames.Count; j++, column += (ServantSectionColumnsTotal.Count + 1))
                {
                    var currentCompany = currentServantReportItem.PerCompany.Find(company => company.Company == _companyNames[j]);
                    var idealCell = ColumnIndexToColumnLetter(column) + row;
                    var stockCell = ColumnIndexToColumnLetter(column + 1) + row;

                    if (currentCompany != null)
                    {

                        
                    }
                    if (j == (_companyNames.Count - 1))
                    {
                        
                    }

                    
                }
            }
            */
        }

        private void InsertMaterialInventoryDataBataillon(ExcelWorksheet worksheet, int startRow, int startColum)
        {
            for (int i = 0, row = startRow + 1; i < _materials.Count; i++, row++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    row++;
                }

                var currentMaterialReportItem = _materialReportItems.Find(materialReportItem => materialReportItem.Material.SapNr == _materials[i].SapNr);

                int totalStock, totalUsed, totalDamaged;
                totalStock = totalUsed = totalDamaged = 0;

                // Calculate and insert material inventory data for all companies and for bataillon
                for (int j = 0, column = startColum; j < _companyNames.Count; j++, column += (MaterialSectionColumns.Count + 2))
                {
                    var currentCompany = currentMaterialReportItem.PerCompany.Find(company => company.Company == _companyNames[j]);
                    var stockCell = ColumnIndexToColumnLetter(column) + row;
                    var usedCell = ColumnIndexToColumnLetter(column + 1) + row;
                    var damagedCell = ColumnIndexToColumnLetter(column + 2) + row;
                    var availableCell = ColumnIndexToColumnLetter(column + 3) + row;

                    if (currentCompany != null)
                    {
                        worksheet.Cells[stockCell].Value = currentCompany.Stock > 0 ? currentCompany.Stock : (int?)null; // Bestand
                        totalStock += currentCompany.Stock;

                        worksheet.Cells[usedCell].Value = currentCompany.Used > 0 ? currentCompany.Used : (int?)null; // Eingesetzt
                        totalUsed += currentCompany.Used;

                        worksheet.Cells[damagedCell].Value = currentCompany.Damaged > 0 ? currentCompany.Damaged : (int?)null; // Defekt
                        totalDamaged += currentCompany.Damaged;

                        worksheet.Cells[availableCell].Value = currentCompany.Stock > 0 ? (currentCompany.Stock - currentCompany.Used - currentCompany.Damaged) : (int?)null; // Verfügbar
                    }

                    if (j == (_companyNames.Count - 1))
                    {
                        worksheet.Cells[stockCell].Value = totalStock > 0 ? totalStock : (int?)null; // Bestand
                        worksheet.Cells[usedCell].Value = totalUsed > 0 ? totalUsed : (int?)null; // Eingesetzt
                        worksheet.Cells[damagedCell].Value = totalDamaged > 0 ? totalDamaged : (int?)null; // Defekt
                        worksheet.Cells[availableCell].Value = totalStock > 0 ? (totalStock - totalUsed - totalDamaged) : (int?)null; // Verfügbar
                    }

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

                    if (worksheet.Cells[damagedCell].Value != null)
                    {
                        worksheet.Cells[damagedCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[damagedCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 50, 50));
                    }
                }
            }
        }

        private void InsertMaterialInventoryDataCompany(ExcelWorksheet worksheet, int startRow, int startColum)
        {
            for (int i = 0, row = startRow + 1; i < _materials.Count; i++, row++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    row++;
                }

                var currentMaterialReportItem = _materialReportItems.Find(materialReportItem => materialReportItem.Material.SapNr == _materials[i].SapNr).PerCompany.Where(perCompany => string.Equals(perCompany.Company, worksheet.Name)).First();

                int totalStock, totalUsed, totalDamaged;
                totalStock = totalUsed = totalDamaged = 0;

                // Calculate and insert material inventory data for all company locations and for total column
                for (int j = 0, column = startColum; j < _companyLocations[worksheet.Name].Count; j++, column += (MaterialSectionColumns.Count + 1))
                {
                    var currentLocation = currentMaterialReportItem.PerLocation.Find(perLocation => perLocation.Location == _companyLocations[worksheet.Name][j]);
                    var stockCell = ColumnIndexToColumnLetter(column) + row;
                    var usedCell = ColumnIndexToColumnLetter(column + 1) + row;
                    var damagedCell = ColumnIndexToColumnLetter(column + 2) + row;
                    var availableCell = ColumnIndexToColumnLetter(column + 3) + row;

                    if (currentLocation != null)
                    {
                        worksheet.Cells[stockCell].Value = currentLocation.Stock > 0 ? currentLocation.Stock : (int?)null; // Bestand
                        totalStock += currentLocation.Stock;

                        worksheet.Cells[usedCell].Value = currentLocation.Used > 0 ? currentLocation.Used : (int?)null; // Eingesetzt
                        totalUsed += currentLocation.Used;

                        worksheet.Cells[damagedCell].Value = currentLocation.Damaged > 0 ? currentLocation.Damaged : (int?)null; // Defekt
                        totalDamaged += currentLocation.Damaged;

                        worksheet.Cells[availableCell].Value = currentLocation.Stock > 0 ? (currentLocation.Stock - currentLocation.Used - currentLocation.Damaged) : (int?)null; // Verfügbar
                    }

                    if (j == (_companyLocations[worksheet.Name].Count - 1))
                    {
                        worksheet.Cells[stockCell].Value = totalStock > 0 ? totalStock : (int?)null; // Bestand
                        worksheet.Cells[usedCell].Value = totalUsed > 0 ? totalUsed : (int?)null; // Eingesetzt
                        worksheet.Cells[damagedCell].Value = totalDamaged > 0 ? totalDamaged : (int?)null; // Defekt
                        worksheet.Cells[availableCell].Value = totalStock > 0 ? (totalStock - totalUsed - totalDamaged) : (int?)null; // Verfügbar
                    }

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

                    if (worksheet.Cells[damagedCell].Value != null)
                    {
                        worksheet.Cells[damagedCell].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[damagedCell].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 50, 50));
                    }
                }
            }
        }
    }
}
