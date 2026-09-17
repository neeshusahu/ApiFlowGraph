using Microsoft.OpenApi;

public class OpenApiGraphExtractor
{
    private readonly DependencyGraph _dependencyGraph;

    public OpenApiGraphExtractor(DependencyGraph dependencyGraph)
    {
        _dependencyGraph = dependencyGraph;
    }

    public List<Path> Extract(OpenApiDocument document)
    {
        var paths = new List<Path>();

        foreach (var path in document.Paths)
        {
            var pathObj = new Path
            {
                PathName = path.Key,
                Operations = new List<Operation>()
            };

            foreach (var operation in path.Value.Operations)
            {
                var op = BuildOperation(operation.Value);
                pathObj.Operations.Add(op);
            }

            paths.Add(pathObj); // was missing before — this is what makes `paths` non-empty
        }

        return paths;
    }

    private Operation BuildOperation(OpenApiOperation operation)
    {
        var op = new Operation
        {
            OperationId = operation.OperationId,
            Summary = operation.Summary,
            Description = operation.Description
        };

        if (operation.RequestBody?.Content != null)
            op.RequestBody = BuildRequestBody(operation.RequestBody.Content);

        if (operation.Responses != null)
            op.Response = BuildResponse(operation.Responses, op.OperationId);

        return op;
    }

    private RequestBody BuildRequestBody(IDictionary<string, IOpenApiMediaType> content)
    {
        // Mirrors your original: takes the last entry in the foreach, same as before.
        // Worth confirming that's intentional — if a request body has multiple content
        // types (e.g. both application/json and application/xml), this silently keeps
        // only the last one enumerated rather than picking application/json explicitly.
        RequestBody requestBody = null;

        foreach (var request in content)
        {
            requestBody = new RequestBody
            {
                RequestContentType = request.Key,
                SchemaType = request.Value.Schema.Type.ToString()
            };

            if (request.Value.Schema.Properties != null)
            {
                requestBody.Properties = new List<Property>();
                foreach (var property in request.Value.Schema.Properties)
                {
                    requestBody.Properties.Add(new Property
                    {
                        Name = property.Key,
                        Type = property.Value.Type.ToString()
                    });
                }
            }
        }

        return requestBody;
    }

    private Response BuildResponse( OpenApiResponses responses, string currentOperationId)
    {
        Response response = null;

        foreach (var responseEntry in responses)
        {
            var statusCode = responseEntry.Key;
            var value = responseEntry.Value;

           
            response = new Response
            {
                ResponseCode = statusCode,
                Links = new List<Link>()
            };
             if (value.Links == null)
                continue;


            foreach (var link in value.Links)
            {
                var linkObj = BuildLink(link.Key, link.Value);
                response.Links.Add(linkObj);

                // currentOperationId's response produces data that linkObj.OperationId
                // consumes — so the edge runs FROM the current op TO the linked op.
                // (Previous version passed these reversed — fixed here.)
                _dependencyGraph.AddDependency(linkObj.OperationId,currentOperationId);
            }
        }

        return response;
    }

    private Link BuildLink(string key, IOpenApiLink link)
    {
        var linkObj = new Link
        {
            Key = key,
            Description = link.Description,
            OperationId = link.OperationId,
            Parameters = new Dictionary<string, RuntimeExpression>()
        };

        foreach (var parameter in link.Parameters)
            linkObj.Parameters.TryAdd(parameter.Key, parameter.Value.Expression);

        return linkObj;
    }
}