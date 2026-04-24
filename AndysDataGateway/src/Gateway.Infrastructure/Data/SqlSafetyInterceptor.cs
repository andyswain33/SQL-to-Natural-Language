using Gateway.Core.Interfaces;
using Gateway.Core.Mapping;
using Gateway.Infrastructure.Security;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Gateway.Infrastructure.Data;

public class SqlSafetyInterceptor : ISqlSafetyInterceptor
{
    private readonly MetadataMapper _metadataMapper;

    public SqlSafetyInterceptor(MetadataMapper metadataMapper)
    {
        _metadataMapper = metadataMapper;
    }

    public bool IsQuerySafe(string generatedSql, string userPrompt, out string errorMessage)
    {
        errorMessage = string.Empty;

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(generatedSql);
        var fragment = parser.Parse(reader, out IList<ParseError> errors);

        if (errors.Count > 0)
        {
            errorMessage = "SQL parsing failed. Invalid syntax.";
            return false;
        }

        var script = fragment as TSqlScript;
        if (script?.Batches.Count != 1 || script.Batches[0].Statements.Count != 1)
        {
            errorMessage = "Only single-statement batches are allowed.";
            return false;
        }

        if (script.Batches[0].Statements[0] is not SelectStatement)
        {
            errorMessage = "Security Violation: Only SELECT statements are permitted.";
            return false;
        }

        var authorizedViews = _metadataMapper.GetAuthorizedViewNames(userPrompt);
        var visitor = new SecureViewVisitor(authorizedViews);
        script.Accept(visitor);

        if (visitor.SecurityViolations.Count > 0)
        {
            errorMessage = string.Join(" ", visitor.SecurityViolations);
            return false;
        }

        return true;
    }
}