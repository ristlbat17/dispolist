using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition.Reporting.Reports
{
    public abstract class DispositionListReporter
    {
        protected DispositionListReporter()
        {
            ServantSectionColumns = new List<string> {"Bestand", "Eingesetzt", "Detachiert", "Verfügbar"};
            ServantSectionColumnsTotal = new List<string> {"OTF, Soll", "Bestand", "Eingesetzt", "Detachiert", "Verfügbar"};
            MaterialSectionColumns = new List<string> {"Bestand", "Eingesetzt", "Defekt", "Verfügbar"};
        }

        protected List<string> ServantSectionColumns { get; }
        protected List<string> ServantSectionColumnsTotal { get; }
        protected List<string> MaterialSectionColumns { get; }

        protected void InsertTitleSection(ExcelWorksheet worksheet, string worksheetTitle)
        {
            worksheet.Cells["A1"].Value = worksheetTitle;
            //worksheet.Cells["A1"].Style.Locked = true;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 18;
        }

        protected void InsertRowDescriptions(ExcelWorksheet worksheet, List<Material.Material> materials, int startRow)
        {
            // Iterate over all categories and for each category insert all material short descriptions (i.e. rows)
            for (int i = 0, row = startRow; i < materials.Count; i++, row++)
            {
                if (i == 0 || materials[i].Category != materials[i - 1].Category)
                {
                    var categoryCell = "A" + row;
                    worksheet.Cells[categoryCell].Value = materials[i].Category;
                    worksheet.Cells[categoryCell].Style.Font.Bold = true;
                    row++;
                }

                var descriptionCell = "B" + row;
                worksheet.Cells[descriptionCell].Value = materials[i].ShortDescription;
                worksheet.Cells[descriptionCell].Style.Font.Bold = true;
                worksheet.Cells[descriptionCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // category column has a width of 1.43, material column is of type auto size
            worksheet.Column(1).Width = 1.43;
            worksheet.Column(2).AutoFit();
        }

        protected void InsertColumnDescriptions(ExcelWorksheet worksheet, int startRow, int startColumn)
        {
            for (var i = 0; i < MaterialSectionColumns.Count; i++)
            {
                var headerCell = ColumnIndexToColumnLetter(startColumn + i) + (startRow - 1);
                worksheet.Cells[headerCell].Value = MaterialSectionColumns[i];
                worksheet.Cells[headerCell].Style.Font.Bold = true;
                worksheet.Cells[headerCell].Style.TextRotation = 90;
                worksheet.Cells[headerCell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[headerCell].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Column(startColumn + i).Width = 3.8;
            }

            worksheet.Column(startColumn - 1).Width = 2.95;
        }

        protected static void InsertHeaderFooter(ExcelWorksheet worksheet, string worksheetTitle)
        {
            var header = worksheet.HeaderFooter.OddHeader;
            header.InsertPicture(Image.FromStream(typeof(DispositionListReporter).Assembly.GetManifestResourceStream("Ristlbat17.Disposition.Images.Schweizerische_Eidgenossenschaft.png")), PictureAlignment.Left);
            header.CenteredText = $"KP, {ExcelHeaderFooter.CurrentDate}\nStand {ExcelHeaderFooter.CurrentTime}";
            header.InsertPicture(Image.FromStream(typeof(DispositionListReporter).Assembly.GetManifestResourceStream("Ristlbat17.Disposition.Images.Badge_RistlBat17.png")), PictureAlignment.Right);

            var footer = worksheet.HeaderFooter.OddFooter;
            footer.LeftAlignedText = worksheetTitle;
            footer.CenteredText = "INTERN";
            footer.RightAlignedText = $"Seite {ExcelHeaderFooter.PageNumber} von {ExcelHeaderFooter.NumberOfPages}";
        }

        protected static void ApplyPrinterSettings(ExcelWorksheet worksheet, eOrientation pageOrientation, int startPrintTitle, int endPrintTitle)
        {
            worksheet.PrinterSettings.PaperSize = ePaperSize.A4;
            worksheet.PrinterSettings.Orientation = pageOrientation;
            worksheet.PrinterSettings.FitToPage = true;
            worksheet.PrinterSettings.FitToWidth = 1;
            worksheet.PrinterSettings.FitToHeight = 0;

            // Page borders
            worksheet.PrinterSettings.TopMargin = 2.4M / 2.54M;
            worksheet.PrinterSettings.RightMargin = 0.8M / 2.54M;
            worksheet.PrinterSettings.BottomMargin = 1.4M / 2.54M;
            worksheet.PrinterSettings.LeftMargin = 0.8M / 2.54M;

            // Print title
            worksheet.PrinterSettings.RepeatRows = new ExcelAddress($"${startPrintTitle}:${endPrintTitle}");
        }

        protected static string ColumnIndexToColumnLetter(int colIndex)
        {
            var div = colIndex;
            var colLetter = string.Empty;

            while (div > 0)
            {
                var mod = (div - 1) % 26;
                colLetter = (char) (65 + mod) + colLetter;
                div = (div - mod) / 26;
            }

            return colLetter;
        }

        public static List<string> SortGradeList(IEnumerable<Grade> grades)
        {
            var gradeDescriptions = new List<string>();

            foreach (var grade in grades)
            {
                gradeDescriptions.Add(grade
                    .GetType()
                    .GetMember(grade.ToString())
                    .FirstOrDefault()
                    ?.GetCustomAttribute<DescriptionAttribute>()
                    ?.Description);
            }

            gradeDescriptions.Sort(GradeRankComparer.Instance);
            return gradeDescriptions;
        }

        public static List<Material.Material> SortMaterialList(IEnumerable<Material.Material> materials)
        {
            return materials
                .OrderBy(material => material.Category, MaterialCategoryComparer.Instance)
                .ThenBy(material => material.ShortDescription).ToList();
        }

        public static List<string> SortCompanyNames(List<string> companyNames)
        {
            companyNames.Sort(CompanyNameComparer.Instance);
            return companyNames;
        }

        public static List<string> SortCompanyLocations(List<string> companyLocations)
        {
            companyLocations.Sort(CompanyLocationComparer.Instance);
            return companyLocations;
        }

        public class GradeRankComparer : IComparer<string>
        {
            public static GradeRankComparer Instance => new GradeRankComparer(StringComparer.CurrentCulture);

            private readonly IComparer<string> _gradeRankComparer;

            public GradeRankComparer(IComparer<string> gradeRankComparer)
            {
                _gradeRankComparer = gradeRankComparer;
            }

            public int Compare(string gradeRank1, string gradeRank2)
            {
                if (string.Equals(gradeRank1, gradeRank2, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 0;
                }

                // "Of" comes first
                if (string.Equals(gradeRank1, "Of", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(gradeRank2, "Of", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Höh Uof" comes second
                if (string.Equals(gradeRank1, "Höh Uof", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(gradeRank2, "Höh Uof", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Uof" comes third
                if (string.Equals(gradeRank1, "Uof", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(gradeRank2, "Uof", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Mannschaft" comes fourth
                if (string.Equals(gradeRank1, "Mannschaft", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(gradeRank2, "Mannschaft", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                return _gradeRankComparer.Compare(gradeRank1, gradeRank2);
            }
        }

        public class MaterialCategoryComparer : IComparer<string>
        {
            public static MaterialCategoryComparer Instance => new MaterialCategoryComparer(StringComparer.CurrentCulture);

            private readonly IComparer<string> _materialCategoryComparer;

            public MaterialCategoryComparer(IComparer<string> materialCategoryComparer)
            {
                _materialCategoryComparer = materialCategoryComparer;
            }

            public int Compare(string materialCategory1, string materialCategory2)
            {
                if (string.Equals(materialCategory1, materialCategory2, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 0;
                }

                // "Ristl" comes first
                if (string.Equals(materialCategory1, "Ristl", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(materialCategory2, "Ristl", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Funk" comes second
                if (string.Equals(materialCategory1, "Funk", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(materialCategory2, "Funk", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Geheim Mat" comes third
                if (string.Equals(materialCategory1, "Geheim Mat", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(materialCategory2, "Geheim Mat", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Mat" comes fourth
                if (string.Equals(materialCategory1, "Mat", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(materialCategory2, "Mat", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // "Fz" comes fifth
                if (string.Equals(materialCategory1, "Fz", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(materialCategory2, "Fz", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                return _materialCategoryComparer.Compare(materialCategory1, materialCategory2);
            }
        }

        public class CompanyNameComparer : IComparer<string>
        {
            public static CompanyNameComparer Instance => new CompanyNameComparer(StringComparer.CurrentCulture);

            private readonly IComparer<string> _companyNameComparer;

            public CompanyNameComparer(IComparer<string> companyNameComparer)
            {
                _companyNameComparer = companyNameComparer;
            }

            public int Compare(string companyName1, string companyName2)
            {
                if (string.Equals(companyName1, companyName2, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 0;
                }

                // "Stab" comes before everything else
                if (string.Equals(companyName1, "Stab", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stab", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // Followed by "Stabskp"
                if (string.Equals(companyName1, "Stabskp", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stabskp", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // .. or "Stabs Kp"
                if (string.Equals(companyName1, "Stabs Kp", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stabs Kp", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                return _companyNameComparer.Compare(companyName1, companyName2);
            }
        }

        public class CompanyLocationComparer : IComparer<string>
        {
            public static CompanyLocationComparer Instance => new CompanyLocationComparer(StringComparer.CurrentCulture);

            private readonly IComparer<string> _companyLocationComparer;

            public CompanyLocationComparer(IComparer<string> companyLocationComparer)
            {
                _companyLocationComparer = companyLocationComparer;
            }

            public int Compare(string companyLocation1, string companyLocation2)
            {
                if (string.Equals(companyLocation1, companyLocation2, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 0;
                }

                // "KP Rw" comes before everything else
                if (string.Equals(companyLocation1, "KP Rw", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(companyLocation2, "KP Rw", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                // Followed by "KP Front"
                if (string.Equals(companyLocation1, "KP Front", StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }

                if (string.Equals(companyLocation2, "KP Front", StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                return _companyLocationComparer.Compare(companyLocation1, companyLocation2);
            }
        }
    }
}