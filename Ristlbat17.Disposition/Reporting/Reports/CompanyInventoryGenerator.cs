using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition.Reporting.Reports
{
    public class CompanyInventoryGenerator : DispositionListReporter
    {
        private const string CumulatedSheetDescription = "Total";

        private int _startRow, _startColumn;

        private Company _company;
        private List<string> _gradeDescriptions;
        private List<List<ServantAllocation>> _servantDistribution;
        private List<Material.Material> _materials;
        private List<List<MaterialAllocation>> _materialDistribution;

        private readonly IMaterialDispositionContext _context;

        public CompanyInventoryGenerator(IMaterialDispositionContext context)
        {
            _context = context;
        }

        public (Dictionary<string,string> errorMessages, List<List<ServantAllocation>> servantDistribution, List<List<MaterialAllocation>> materialDistribution) ExtractCompanyInventory(string companyName, ExcelPackage package)
        {
            _company = _context.Companies.Find(company => string.Equals(company.Name, companyName)).SingleOrDefault();

            _gradeDescriptions = SortGradeList((Grade[])Enum.GetValues(typeof(Grade)));
            _servantDistribution = new List<List<ServantAllocation>>();
            _gradeDescriptions.ForEach(grade => _servantDistribution.Add(new List<ServantAllocation>()));

            _materials = _context.Material.Find(_ => true).ToList();
            _materials = SortMaterialList(_materials);
            _materialDistribution = new List<List<MaterialAllocation>>();
            _materials.ForEach(material => _materialDistribution.Add(new List<MaterialAllocation>()));
            
            // 1. The whole workbook (i.e. all worksheets) must be valid before an import is started
            (var workbookValid, var errorMessages) = ValidateWorkbook(package);
            if (!workbookValid)
            {
                // Excel template seems not valid, i.e. an outdated material list was found within workbook, the workbook contains invalid locations resp. worksheet names or not all inputs are valid
                return (errorMessages, new List<List<ServantAllocation>>(), new List<List<MaterialAllocation>>());
            }

            // 2. If workbook is valid extract inventory data from each worksheet
            foreach (var worksheet in package.Workbook.Worksheets.Where(worksheet => worksheet.Name != CumulatedSheetDescription))
            {
                _startRow = 5;
                _startColumn = 4;

                ExtractServantInventoryData(worksheet);
                ExtractMaterialInventoryData(worksheet);             
            }

            return (errorMessages, _servantDistribution, _materialDistribution);
        }

        private (bool workbookValid, Dictionary<string, string> errorMessages) ValidateWorkbook(ExcelPackage package)
        {
            var worksheets = package.Workbook.Worksheets;
            var workbookValid = true;
            var errorMessages = new Dictionary<string,string>();

            // 1. Check if workbook company property is equal to company name
            if (!string.Equals(package.Workbook.Properties.Company, _company.Name)) {
                workbookValid = false;
                errorMessages.Add("Workbook validation", "Workbook company property is not equal to current company (i.e. wrong template for upload selected)");
            }

            // 2. Check if number of worksheets corresponds with number of locations
            if (worksheets.Count - 1 != _company.Locations.Count) // subtract total sheet
            {
                workbookValid = false;
                errorMessages.Add("Location validation", "There is not the same number of locations within the excel file as they were found on the server");
            }

            foreach (var worksheet in worksheets.Where(worksheet => worksheet.Name != CumulatedSheetDescription))
            {
                _startRow = 5;
                _startColumn = 2;

                // 3. Validate each company location
                if (!ValidateCompanyLocation(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Location validation - {worksheet.Name}", $"Location validation: location {worksheet.Name} not found on the server");
                }

                // 4. Validate servant list within each worksheet
                if (!ValidateServantList(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Servant list validation - {worksheet.Name}", $"Worksheet with name {worksheet.Name} does not contain a valid servant list");
                }

                // 5. Validate servant input section within each worksheet
                if (!ValidateServantInputSection(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Servant input section validation - {worksheet.Name}", $"Not all servant section inputs within worksheet {worksheet.Name} are positive integers");
                }

                // 6. Validate material list within each worksheet
                if (!ValidateMaterialList(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Material list validation - {worksheet.Name}", $"Worksheet with name {worksheet.Name} does not contain a valid material list");
                }

                // 7. Validate material input section within each worksheet
                if (!ValidateMaterialInputSection(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Material input section validation - {worksheet.Name}", $"Not all material section inputs within worksheet {worksheet.Name} are positive integers");
                }
            }

            return (workbookValid, errorMessages);
        }

        private bool ValidateCompanyLocation(ExcelWorksheet worksheet)
        {
            return _company.Locations.Select(_ => _.Name).Contains(worksheet.Name);
        }

        private bool ValidateServantList(ExcelWorksheet worksheet)
        {
            var servantListValid = true;
            var startRow = _startRow;

            for (var i = 0; i < _gradeDescriptions.Count; i++, startRow++)
            {
                if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value == null) || (worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value.ToString() != _gradeDescriptions[i]))
                {
                    servantListValid = false;
                }
            }

            if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value == null) || (worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value.ToString() != "Total"))
            {
                servantListValid = false;
            }

            return servantListValid;
        }

        private bool ValidateServantInputSection(ExcelWorksheet worksheet)
        {
            var servantInputSectionValid = true;
            const int startColumn = 4;

            for (var i = 0; i < _gradeDescriptions.Count + 1; i++, _startRow++)
            {
                if (((worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + _startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + _startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + _startRow].Value.ToString()) >= 0)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + _startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + _startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + _startRow].Value.ToString()) >= 0)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + _startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + _startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + _startRow].Value.ToString()) >= 0))))
                {
                    servantInputSectionValid = false;
                }
            }

            return servantInputSectionValid;
        }

        private bool ValidateMaterialList(ExcelWorksheet worksheet)
        {
            var materialListValid = true;

            for (int i = 0, startRow = _startRow + 3; i < _materials.Count; i++, startRow++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn - 1) + startRow].Value == null) || (worksheet.Cells[ColumnIndexToColumnLetter(_startColumn - 1) + startRow].Value.ToString() != _materials[i].Category))
                    {
                        materialListValid = false;
                    }
                    startRow++;
                }

                if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value == null) || (worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value.ToString() != _materials[i].ShortDescription))
                {
                    materialListValid = false;
                }
            }

            return materialListValid;
        }

        private bool ValidateMaterialInputSection(ExcelWorksheet worksheet)
        {
            var materialInputSectionValid = true;
            const int startColumn = 4;

            for (int i = 0, startRow = _startRow + 3; i < _materials.Count; i++, startRow++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    startRow++;
                }

                if (((worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + startRow].Value.ToString()) >= 0)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + startRow].Value.ToString()) >= 0)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + startRow].Value != null) && !(int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + startRow].Value.ToString(), out _) && (int.Parse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + startRow].Value.ToString()) >= 0))))
                {
                    materialInputSectionValid = false;
                }
            }

            return materialInputSectionValid;
        }

        private void ExtractServantInventoryData(ExcelWorksheet worksheet)
        {
            for (var i = 0; i < _gradeDescriptions.Count + 1; i++, _startRow++)
            {
                if (i != _gradeDescriptions.Count)
                {
                    var location = worksheet.Name;

                    if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + _startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + _startRow].Value.ToString(), out var stock)))
                    {
                        stock = 0;
                    }
                    if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 1) + _startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 1) + _startRow].Value.ToString(), out var used)))
                    {
                        used = 0;
                    }
                    if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 2) + _startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 2) + _startRow].Value.ToString(), out var detached)))
                    {
                        detached = 0;
                    }

                    _servantDistribution[i].Add(new ServantAllocation
                    {
                        Location = location,
                        Stock = stock,
                        Used = used,
                        Detached = detached
                    });
                }
            }
        }

        private void ExtractMaterialInventoryData(ExcelWorksheet worksheet)
        {
            for (int i = 0, startRow = _startRow + 3; i < _materials.Count; i++, startRow++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    startRow++;
                }

                var location = worksheet.Name;

                if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn) + startRow].Value.ToString(), out var stock)))
                {
                    stock = 0;
                }
                if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 1) + startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 1) + startRow].Value.ToString(), out var used)))
                {
                    used = 0;
                }
                if ((worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 2) + startRow].Value == null) || (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(_startColumn + 2) + startRow].Value.ToString(), out var damaged)))
                {
                    damaged = 0;
                }

                _materialDistribution[i].Add(new MaterialAllocation
                {
                    Location = location,
                    Stock = stock,
                    Used = used,
                    Damaged = damaged
                });
            }
        }
    }
}
