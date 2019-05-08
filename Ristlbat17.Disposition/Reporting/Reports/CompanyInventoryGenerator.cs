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

        private readonly List<Location> _companyLocations;
        private readonly List<string> _gradeDescriptions;
        private readonly List<List<ServantAllocation>> _servantDistribution;
        private readonly List<Material.Material> _materials;
        private readonly List<List<MaterialAllocation>> _materialDistribution;

        public CompanyInventoryGenerator(List<Material.Material> materials, List<Location> companyLocations)
        {
            // TODO @Klamir move to method and make the method static or pass dependencies such as Rempos via DI
            _companyLocations = companyLocations;

            _gradeDescriptions = SortGradeList((Grade[])Enum.GetValues(typeof(Grade)));
            _servantDistribution = new List<List<ServantAllocation>>();
            _gradeDescriptions.ForEach(grade => _servantDistribution.Add(new List<ServantAllocation>()));

            _materials = SortMaterialList(materials);
            _materialDistribution = new List<List<MaterialAllocation>>();
            _materials.ForEach(material => _materialDistribution.Add(new List<MaterialAllocation>()));
        }

        public (Dictionary<string,string> errorMessages, List<List<ServantAllocation>> servantDistribution, List<List<MaterialAllocation>> materialDistribution) ExtractCompanyInventory(ExcelPackage package)
        {
            // 1. Get all excel worksheets within the populated template
            var worksheets = package.Workbook.Worksheets;

            // 2. The whole workbook (i.e. all worksheets) must be valid before an import is started
            (var workbookValid, var errorMessages) = ValidateWorkbook(worksheets);
            if (!workbookValid)
            {
                // Excel template seems not valid, i.e. an outdated material list was found within workbook, the workbook contains invalid locations resp. worksheet names or not all inputs are valid
                return (errorMessages, new List<List<ServantAllocation>>(), new List<List<MaterialAllocation>>());
            }

            // 3. If workbook is valid extract inventory data from each worksheet
            foreach (var worksheet in worksheets.Where(worksheet => worksheet.Name != CumulatedSheetDescription))
            {
                _startRow = 5;
                _startColumn = 4;

                ExtractServantInventoryData(worksheet);
                ExtractMaterialInventoryData(worksheet);             
            }

            return (errorMessages, _servantDistribution, _materialDistribution);
        }

        private (bool workbookValid, Dictionary<string, string> errorMessages) ValidateWorkbook(ExcelWorksheets worksheets)
        {
            var workbookValid = true;
            var errorMessages = new Dictionary<string,string>();

            // 1. Check if number of worksheets corresponds with number of locations
            if (worksheets.Count - 1 != _companyLocations.Select(_ => _.Name).Count()) // subtract total sheet
            {
                workbookValid = false;
                errorMessages.Add("Location validation", "There is not the same number of locations within the excel file as they were found on the server");
            }

            foreach (var worksheet in worksheets.Where(worksheet => worksheet.Name != CumulatedSheetDescription))
            {
                _startRow = 5;
                _startColumn = 2;

                // 2. Validate each company location
                if (!ValidateCompanyLocation(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Location validation - {worksheet.Name}", $"Location validation: location {worksheet.Name} not found on the server");
                }

                // 3. Validate servant list within each worksheet
                if (!ValidateServantList(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Servant validation - {worksheet.Name}", $"Worksheet with name {worksheet.Name} does not contain a valid servant list");
                }

                // 4. Validate servant input section within each worksheet
                if (!ValidateServantInputSection(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Input section validation - {worksheet.Name}", $"Not all servant section inputs within worksheet {worksheet.Name} are numbers");
                }

                // 5. Validate material list within each worksheet
                if (!ValidateMaterialList(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Material validation - {worksheet.Name}", $"Worksheet with name {worksheet.Name} does not contain a valid material list");
                }

                // 6. Validate material input section within each worksheet
                if (!ValidateMaterialInputSection(worksheet))
                {
                    workbookValid = false;
                    errorMessages.Add($"Input section validation - {worksheet.Name}", $"Not all material section inputs within worksheet {worksheet.Name} are numbers");
                }
            }

            return (workbookValid, errorMessages);
        }

        private bool ValidateCompanyLocation(ExcelWorksheet worksheet)
        {
            return _companyLocations.Select(_ => _.Name).Contains(worksheet.Name);
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
                if (((worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + _startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + _startRow].Value.ToString(), out _)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + _startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + _startRow].Value.ToString(), out _)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + _startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + _startRow].Value.ToString(), out _))))
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
            var inputSectionValid = true;
            const int startColumn = 4;

            for (int i = 0, startRow = _startRow + 3; i < _materials.Count; i++, startRow++)
            {
                if ((i == 0) || (_materials[i].Category != _materials[i - 1].Category))
                {
                    startRow++;
                }

                if (((worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn) + startRow].Value.ToString(), out _)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 1) + startRow].Value.ToString(), out _)))
                    || ((worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + startRow].Value != null) && (!int.TryParse(worksheet.Cells[ColumnIndexToColumnLetter(startColumn + 2) + startRow].Value.ToString(), out _))))
                {
                    inputSectionValid = false;
                }
            }

            return inputSectionValid;
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
