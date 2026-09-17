public class RequestBody
{
    public string RequestContentType {get;set;}
    public string SchemaType {get;set;}
    public List<Property> Properties { get; set; }=new List<Property>();
}