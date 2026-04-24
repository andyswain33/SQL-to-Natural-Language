using Microsoft.AspNetCore.Mvc;
using Gateway.Core.Orchestration;

namespace Gateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NaturalLanguageQueryController : ControllerBase
{
    private readonly SqlGenerationOrchestrator _orchestrator;

    // Notice we are NO LONGER injecting SqlExecutionService or SqlSafetyInterceptor here.
    // The Controller is now appropriately dumb.
    public NaturalLanguageQueryController(SqlGenerationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskDatabase([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserQuery))
            return BadRequest(new { Error = "Query cannot be empty." });

        try
        {
            // The master orchestrator now handles the entire pipeline: 
            // Mapping -> Generating -> Intercepting -> Executing -> Masking -> Summarizing
            var result = await _orchestrator.ProcessUserRequestAsync(request.UserQuery, request.EnableEnterpriseMasking);

            if (!result.IsSuccess)
            {
                return StatusCode(403, new { Error = "Query blocked.", Reason = result.ErrorMessage });
            }

            return Ok(new
            {
                MaskingEnabled = request.EnableEnterpriseMasking,
                Question = request.UserQuery,
                Answer = result.FinalAnswer,
                Diagnostics = new
                {
                    ValidatedSql = result.ValidatedSql,
                    RawDataPayload = result.RawDataPayload
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Gateway processing failed.", Details = ex.Message });
        }
    }
}