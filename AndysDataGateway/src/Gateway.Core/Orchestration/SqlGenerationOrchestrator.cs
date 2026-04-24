using Microsoft.SemanticKernel;
using Gateway.Core.Mapping;
using Gateway.Core.Interfaces;
using Gateway.Core.Services;

namespace Gateway.Core.Orchestration;

public class OrchestrationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FinalAnswer { get; set; } = string.Empty;
    public string ValidatedSql { get; set; } = string.Empty;
    public string RawDataPayload { get; set; } = string.Empty;
}

public class SqlGenerationOrchestrator
{
    private readonly Kernel _kernel;
    private readonly MetadataMapper _metadataMapper;
    private readonly ISqlSafetyInterceptor _safetyInterceptor;
    private readonly IQueryExecutor _queryExecutor;
    private readonly DataMaskingService _maskingService;

    public SqlGenerationOrchestrator(
        Kernel kernel,
        MetadataMapper metadataMapper,
        ISqlSafetyInterceptor safetyInterceptor,
        IQueryExecutor queryExecutor,
        DataMaskingService maskingService)
    {
        _kernel = kernel;
        _metadataMapper = metadataMapper;
        _safetyInterceptor = safetyInterceptor;
        _queryExecutor = queryExecutor;
        _maskingService = maskingService;
    }

    public async Task<OrchestrationResult> ProcessUserRequestAsync(string userPrompt, bool enableMasking)
    {
        // 1. Map Intent & Generate SQL
        string schemaContext = _metadataMapper.GetSchemaContext(userPrompt);
        var generatePrompt = @"
{{$schemaContext}}
Generate a valid, read-only Microsoft T-SQL SELECT statement to answer the user's request.
Return ONLY the raw SQL code without markdown wrappers or explanations.
User Request: {{$userPrompt}}";

        var sqlResult = await _kernel.InvokePromptAsync(generatePrompt, new KernelArguments
        {
            { "schemaContext", schemaContext },
            { "userPrompt", userPrompt }
        });

        var generatedSql = sqlResult.ToString().Trim();

        // 2. Validate AST Security
        if (!_safetyInterceptor.IsQuerySafe(generatedSql, userPrompt, out string securityError))
        {
            return new OrchestrationResult { IsSuccess = false, ErrorMessage = securityError };
        }

        // 3. Execute Query
        var rawData = await _queryExecutor.ExecuteRawAsync(generatedSql);

        // 4. Apply Domain Masking
        var jsonResults = _maskingService.MaskAndSerialize(rawData, enableMasking);

        // 5. AI Summarization
        var summarizePrompt = @"
You are an enterprise data assistant. A secure database query has been executed to answer the user's question.
User's Original Question: {{$originalPrompt}}
Masked Database Results (JSON): {{$jsonResults}}
Provide a concise, human-readable summary that directly answers the user. 
CRITICAL: Only use provided data. Do not mention JSON or SQL.";

        var summaryResult = await _kernel.InvokePromptAsync(summarizePrompt, new KernelArguments
        {
            { "originalPrompt", userPrompt },
            { "jsonResults", jsonResults }
        });

        return new OrchestrationResult
        {
            IsSuccess = true,
            FinalAnswer = summaryResult.ToString().Trim(),
            ValidatedSql = generatedSql,
            RawDataPayload = jsonResults
        };
    }
}