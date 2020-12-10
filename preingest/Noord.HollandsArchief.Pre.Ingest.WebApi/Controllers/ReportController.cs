using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> _logger;
        private AppSettings _settings = null;
        public ReportController(ILogger<ReportController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet("generateexport/{guid}", Name = "Get the total report for this current guid session.", Order = 0)]
        public IActionResult GenerateReportExport(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");            

            _logger.LogInformation("Enter TotalReport.");

            SpreadSheetHandler handler = HttpContext.RequestServices.GetService(typeof(SpreadSheetHandler)) as SpreadSheetHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SpreadSheetHandler).Name);

            handler.CreateSpreadSheet(guid);

            _logger.LogInformation("Exit TotalReport.");

            return new JsonResult(new { });
        }
    }
}
