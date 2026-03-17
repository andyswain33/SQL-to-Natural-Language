using Microsoft.SemanticKernel;
using Gateway.Core.Mapping;
using Polly;
using Polly.Retry;
using System.Net;

namespace Gateway.Core.Orchestration
{
    public class SqlGenerationOrchestrator
    {
        private readonly Kernel _kernel;
        private readonly MetadataMapper _metadataMapper;

        // Define a retry policy: Retry 3 times with exponential backoff
        // (Wait 2s, then 4s, then 8s) if we hit a 429 error.
        private readonly AsyncRetryPolicy _retryPolicy = Policy
            .Handle<HttpOperationException>(ex => (int)ex.StatusCode == 429)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        public SqlGenerationOrchestrator(Kernel kernel, MetadataMapper metadataMapper)
        {
            _kernel = kernel;
            _metadataMapper = metadataMapper;
        }

        public async Task<string> GenerateSecureSqlAsync(string userPrompt)
        {
            string schemaContext = _metadataMapper.GetSchemaContext(userPrompt);

            var promptTemplate = @"
{{$schemaContext}}
Generate a valid, read-only Microsoft T-SQL SELECT statement to answer the user's request.
Return ONLY the raw SQL code without markdown wrappers or explanations.
User Request: {{$userPrompt}}";

            // Wrap the call in the retry policy
            var result = await _retryPolicy.ExecuteAsync(async () =>
                await _kernel.InvokePromptAsync(promptTemplate, new KernelArguments
                {
                    { "schemaContext", schemaContext },
                    { "userPrompt", userPrompt }
                })
            );

            return result.ToString().Trim();
        }

        public async Task<string> SummarizeDataAsync(string originalPrompt, string jsonResults)
        {
            var promptTemplate = @"
You are an enterprise data assistant. A secure database query has been executed to answer the user's question.
User's Original Question: {{$originalPrompt}}
Masked Database Results (JSON): {{$jsonResults}}
Provide a concise, human-readable summary that directly answers the user. 
CRITICAL: Only use provided data. Do not mention JSON or SQL.";

            // Wrap this call too
            var result = await _retryPolicy.ExecuteAsync(async () =>
                await _kernel.InvokePromptAsync(promptTemplate, new KernelArguments
                {
                    { "originalPrompt", originalPrompt },
                    { "jsonResults", jsonResults }
                })
            );

            return result.ToString().Trim();
        }
    }
}