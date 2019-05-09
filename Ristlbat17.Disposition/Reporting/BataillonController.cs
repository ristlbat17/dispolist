using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;
using Ristlbat17.Disposition.Reporting.Reports;
using Swashbuckle.AspNetCore.Annotations;

namespace Ristlbat17.Disposition.Reporting
{
    [Route("api/[controller]")]
    [ApiController]
    public class BataillonController : ControllerBase
    {
        private readonly IMaterialDispositionContext _context;
        private readonly IBataillonReporter _reporter;
        private readonly BataillonOverviewReporter _bataillonOverviewReporter;

        public BataillonController(IMaterialDispositionContext context, IBataillonReporter reporter, BataillonOverviewReporter bataillonOverviewReporter)
        {
            _context = context;
            _reporter = reporter;
            _bataillonOverviewReporter = bataillonOverviewReporter;
        }

        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerOperation(OperationId = nameof(GenerateTechMatReport))]
        [HttpPost("reports/material")]
        public async Task<ActionResult> GenerateTechMatReport()
        {
            await _reporter.GenerateDispositionReport(DateTime.UtcNow);
            return Ok();
        }

        [SwaggerOperation(OperationId = nameof(GetReportList))]
        [SwaggerResponse(StatusCodes.Status200OK)]
        [HttpGet("reports/material")]
        public async Task<ActionResult<IEnumerable<Report>>> GetReportList()
        {
            return await _context.DispositionReport.Find(_ => true).Project(report => new Report
            {
                Id = report.Id,
                ReportingDate = report.ReportDate
            }).SortByDescending(report => report.ReportDate).ToListAsync();
        }

        [SwaggerOperation(OperationId = nameof(DownloadReport))]
        [SwaggerResponse(StatusCodes.Status200OK)]
        [HttpGet("reports/material/{reportId}")]
        public ActionResult DownloadReport(string reportId)
        {
            byte[] data;
            string inventoryReportDate;
            using (var package = new ExcelPackage())
            {
                inventoryReportDate = _bataillonOverviewReporter.GenerateBataillonOverviewReport(package, reportId);
                data = package.GetAsByteArray();
            }

            var fileDownloadName = $"Dispoliste_{inventoryReportDate}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
        }

        [SwaggerOperation(OperationId = nameof(DeleteReportById))]
        [SwaggerResponse(StatusCodes.Status200OK)]
        [HttpDelete("reports/material/{reportId}")]
        public async Task<ActionResult> DeleteReportById(string reportId)
        {
            await _context.DispositionReport.DeleteOneAsync(_ => _.Id == reportId);
            return NoContent();
        }

        public class Report
        {
            public string Id { get; set; }

            public DateTime ReportingDate { get; set; }

            public ReportType Type { get; set; }
        }
    }
}