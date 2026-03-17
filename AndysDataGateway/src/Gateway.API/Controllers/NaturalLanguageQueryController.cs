using Microsoft.AspNetCore.Mvc;
using Gateway.Core.Orchestration;
using Gateway.Infrastructure.Data;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NaturalLanguageQueryController : ControllerBase
    {
        private readonly SqlGenerationOrchestrator _orchestrator;
        private readonly SqlSafetyInterceptor _safetyInterceptor;
        private readonly SqlExecutionService _executionService;

        public NaturalLanguageQueryController(
            SqlGenerationOrchestrator orchestrator,
            SqlSafetyInterceptor safetyInterceptor,
            SqlExecutionService executionService)
        {
            _orchestrator = orchestrator;
            _safetyInterceptor = safetyInterceptor;
            _executionService = executionService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskDatabase([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserQuery))
                return BadRequest(new { Error = "Query cannot be empty." });

            // Step 1: AI Generation
            var generatedSql = await _orchestrator.GenerateSecureSqlAsync(request.UserQuery);

            // Step 2: The Security Moat
            if (!_safetyInterceptor.IsQuerySafe(generatedSql, out string securityError))
            {
                return StatusCode(403, new { Error = "Query blocked.", Reason = securityError });
            }

            try
            {
                // Step 3: Execute (Passing the demo flag down!)
                var jsonResults = await _executionService.ExecuteAndMaskAsync(generatedSql, request.EnableEnterpriseMasking);

                // Step 4: AI Summarization
                var finalAnswer = await _orchestrator.SummarizeDataAsync(request.UserQuery, jsonResults);

                return Ok(new
                {
                    MaskingEnabled = request.EnableEnterpriseMasking, // Echo the state
                    Question = request.UserQuery,
                    Answer = finalAnswer,
                    Diagnostics = new
                    {
                        ValidatedSql = generatedSql,
                        RawDataPayload = jsonResults // The UI can diff this!
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Database execution failed.", Details = ex.Message });
            }
        }
    }
}