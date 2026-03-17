namespace Gateway.API.Controllers
{
    public class QueryRequest
    {
        public required string UserQuery { get; set; }

        // Default to true so the system remains secure by default!
        public bool EnableEnterpriseMasking { get; set; } = true;
    }
}
