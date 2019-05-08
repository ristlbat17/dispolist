using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition.Reporting
{
    public class BataillonReporter : IBataillonReporter
    {
        private readonly IDispositionContext _dispositionContext;
        private readonly IMaterialDispositionContext _materialDispositionContext;
        private readonly IServantDispositionContext _servantDispositionContext;

        public BataillonReporter(IDispositionContext dispositionContext, IMaterialDispositionContext materialDispositionContext, IServantDispositionContext servantDispositionContext)
        {
            _dispositionContext = dispositionContext;
            _materialDispositionContext = materialDispositionContext;
            _servantDispositionContext = servantDispositionContext;
        }

        public async Task GenerateDispositionReport(DateTime dueDate)
        {
            var report = new DispositionReport
            {
                ReportDate = dueDate,
                Type = ReportType.MaterialLevelCompany
            };

            var companyNames = await _dispositionContext.Companies.Find(_ => true).Project(comp => comp.Name).ToListAsync();

            report.MaterialReportItems = await GenerateMaterialDispositionReport(companyNames);
            report.ServantReportItems = await GenerateServantDispositionReport(companyNames);

            await _dispositionContext.DispositionReport.InsertOneAsync(report);
        }

        private async Task<List<MaterialReportItemBataillon>> GenerateMaterialDispositionReport(IReadOnlyCollection<string> companyNames)
        {
            var materialReportItems = new List<MaterialReportItemBataillon>();
            var materialInventory = await _materialDispositionContext.MaterialInventory.Find(_ => true).ToListAsync();
            var materials = await _materialDispositionContext.Material.Find(_ => true).ToListAsync();

            foreach (var material in materials)
            {
                var materialPerCompany = materialInventory.Where(matInvItem => matInvItem.SapNr == material.SapNr)
                    .Select(matInvItem => new MaterialReportItemCompany
                    {
                        Company = matInvItem.Company,
                        Stock = matInvItem.Stock,
                        Used = matInvItem.Used,
                        Damaged = matInvItem.Damaged,
                        PerLocation = matInvItem.Distribution
                            .Select(matAllocItem => new MaterialReportItemLocation
                            {
                                Location = matAllocItem.Location,
                                Stock = matAllocItem.Stock,
                                Used = matAllocItem.Used,
                                Damaged = matAllocItem.Damaged
                            }).ToList()
                    }).ToHashSet();

                materialPerCompany.UnionWith(companyNames.Select(name => new MaterialReportItemCompany { Company = name }));

                materialReportItems.Add(new MaterialReportItemBataillon
                {
                    Material = material,
                    Stock = materialPerCompany.Sum(item => item.Stock),
                    Used = materialPerCompany.Sum(item => item.Used),
                    Damaged = materialPerCompany.Sum(item => item.Damaged),
                    PerCompany = materialPerCompany.ToList()
                });
            }

            return materialReportItems;
        }

        private async Task<List<ServantReportItemBataillon>> GenerateServantDispositionReport(IReadOnlyCollection<string> companyNames)
        {
            var servantReportItems = new List<ServantReportItemBataillon>();
            var servantInventory = await _servantDispositionContext.ServantInventory.AsQueryable().ToListAsync();
            var grades = (Grade[])Enum.GetValues(typeof(Grade));

            foreach (var grade in grades)
            {
                var gradePerCompany = servantInventory.Where(servInvItem => servInvItem.Grade == grade)
                    .Select(servInvItem => new ServantReportItemCompany
                    {
                        Company = servInvItem.Company,
                        Ideal = servInvItem.Ideal,
                        Stock = servInvItem.Stock,
                        Used = servInvItem.Used,
                        Detached = servInvItem.Detached,
                        PerLocation = servInvItem.Distribution
                            .Select(servAllocItem => new ServantReportItemLocation
                            {
                                Location = servAllocItem.Location,
                                Stock = servAllocItem.Stock,
                                Used = servAllocItem.Used,
                                Detached = servAllocItem.Detached
                            }).ToList()
                }).ToHashSet();

                gradePerCompany.UnionWith(companyNames.Select(name => new ServantReportItemCompany { Company = name }));

                servantReportItems.Add(new ServantReportItemBataillon
                {
                    Grade = grade,
                    Ideal = gradePerCompany.Sum(item => item.Ideal),
                    Stock = gradePerCompany.Sum(item => item.Stock),
                    Used = gradePerCompany.Sum(item => item.Used),
                    Detached = gradePerCompany.Sum(item => item.Detached),
                    PerCompany = gradePerCompany.ToList()
                });
            }

            return servantReportItems;
        }
    }
}