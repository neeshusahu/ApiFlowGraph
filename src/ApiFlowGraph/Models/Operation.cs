public class Operation
{
    public string OperationId { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
  
    public RequestBody RequestBody { get; set; }
    public Response Response { get; set; }
}