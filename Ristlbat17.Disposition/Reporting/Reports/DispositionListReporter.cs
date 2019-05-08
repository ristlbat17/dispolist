﻿using System;
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

            gradeDescriptions.Sort(new GradeRankComparer(StringComparer.CurrentCulture));
            return gradeDescriptions;
        }

        public static List<Material.Material> SortMaterialList(IEnumerable<Material.Material> materials)
        {
            return materials
                .OrderBy(material => material.Category, new MaterialCategoryComparer(StringComparer.CurrentCulture))
                .ThenBy(material => material.ShortDescription).ToList();
        }

        private class GradeRankComparer : IComparer<string>
        {
            private readonly IComparer<string> _gradeRankComparer;

            public GradeRankComparer(IComparer<string> gradeRankComparer)
            {
                _gradeRankComparer = gradeRankComparer;
            }

            public int Compare(string gradeRank1, string gradeRank2)
            {
                if (_gradeRankComparer.Compare(gradeRank1, gradeRank2) == 0)
                {
                    return 0;
                }

                // "Of" comes first
                if (_gradeRankComparer.Compare(gradeRank1, "Of") == 0)
                {
                    return -1;
                }

                if (_gradeRankComparer.Compare(gradeRank2, "Of") == 0)
                {
                    return 1;
                }

                // "Höh Uof" comes second
                if (_gradeRankComparer.Compare(gradeRank1, "Höh Uof") == 0)
                {
                    return -1;
                }

                if (_gradeRankComparer.Compare(gradeRank2, "Höh Uof") == 0)
                {
                    return 1;
                }

                // "Uof" comes third
                if (_gradeRankComparer.Compare(gradeRank1, "Uof") == 0)
                {
                    return -1;
                }

                if (_gradeRankComparer.Compare(gradeRank2, "Uof") == 0)
                {
                    return 1;
                }

                // "Mannschaft" comes fourth
                if (_gradeRankComparer.Compare(gradeRank1, "Mannschaft") == 0)
                {
                    return -1;
                }

                if (_gradeRankComparer.Compare(gradeRank2, "Mannschaft") == 0)
                {
                    return 1;
                }

                return _gradeRankComparer.Compare(gradeRank1, gradeRank2);
            }
        }

        private class MaterialCategoryComparer : IComparer<string>
        {
            private readonly IComparer<string> _materialCategoryComparer;

            public MaterialCategoryComparer(IComparer<string> materialCategoryComparer)
            {
                _materialCategoryComparer = materialCategoryComparer;
            }

            public int Compare(string materialCategory1, string materialCategory2)
            {
                if (_materialCategoryComparer.Compare(materialCategory1, materialCategory2) == 0)
                {
                    return 0;
                }

                // "Ristl" comes first
                if (_materialCategoryComparer.Compare(materialCategory1, "Ristl") == 0)
                {
                    return -1;
                }

                if (_materialCategoryComparer.Compare(materialCategory2, "Ristl") == 0)
                {
                    return 1;
                }

                // "Funk" comes second
                if (_materialCategoryComparer.Compare(materialCategory1, "Funk") == 0)
                {
                    return -1;
                }

                if (_materialCategoryComparer.Compare(materialCategory2, "Funk") == 0)
                {
                    return 1;
                }

                // "Geheim Mat" comes third
                if (_materialCategoryComparer.Compare(materialCategory1, "Geheim Mat") == 0)
                {
                    return -1;
                }

                if (_materialCategoryComparer.Compare(materialCategory2, "Geheim Mat") == 0)
                {
                    return 1;
                }

                // "Mat" comes fourth
                if (_materialCategoryComparer.Compare(materialCategory1, "Mat") == 0)
                {
                    return -1;
                }

                if (_materialCategoryComparer.Compare(materialCategory2, "Mat") == 0)
                {
                    return 1;
                }

                // "Fz" comes fifth
                if (_materialCategoryComparer.Compare(materialCategory1, "Fz") == 0)
                {
                    return -1;
                }

                if (_materialCategoryComparer.Compare(materialCategory2, "Fz") == 0)
                {
                    return 1;
                }

                return _materialCategoryComparer.Compare(materialCategory1, materialCategory2);
            }
        }

        public class CompanyNameComparer : IComparer<string>
        {
            public static CompanyNameComparer Instance => new CompanyNameComparer(StringComparer.CurrentCulture);

            private readonly IComparer<string> _companyComparer;

            public CompanyNameComparer(IComparer<string> companyComparer)
            {
                _companyComparer = companyComparer;
            }

            public int Compare(string companyName1, string companyName2)
            {
                if (string.Equals(companyName1, companyName2))
                {
                    return 0;
                }

                // "Stab" comes before everything else
                if (string.Equals(companyName1, "Stab"))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stab"))
                {
                    return 1;
                }

                // Followed by "Stabskp"
                if (string.Equals(companyName1, "Stabskp"))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stabskp"))
                {
                    return 1;
                }

                // .. or "Stabs Kp"
                if (string.Equals(companyName1, "Stabs Kp"))
                {
                    return -1;
                }

                if (string.Equals(companyName2, "Stabs Kp"))
                {
                    return 1;
                }

                // "Bat" is last
                if (string.Equals(companyName1, "Bat"))
                {
                    return 1;
                }

                if (string.Equals(companyName2, "Bat"))
                {
                    return -1;
                }

                return _companyComparer.Compare(companyName1, companyName2);
            }
        }
    }
}