using Microsoft.OpenApi;

public class Link
{
    public string Key { get; set; }
    public string OperationId { get; set; }

    public string Description { get; set; }
    public Dictionary<string, RuntimeExpression> Parameters { get; set; } = new Dictionary<string, RuntimeExpression>();
}