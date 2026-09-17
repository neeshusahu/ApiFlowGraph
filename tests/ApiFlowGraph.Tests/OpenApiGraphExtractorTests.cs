using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

public class OpenApiGraphExtractorTests
{
    private const string SampleSpec = """
        openapi: 3.1.0
        info:
          title: Sample API
          version: "1.0"
        paths:
          /organizations:
            post:
              operationId: createOrganization
              summary: Create an organization
              requestBody:
                content:
                  application/json:
                    schema:
                      type: object
                      properties:
                        name:
                          type: string
              responses:
                "201":
                  description: Created.
                  links:
                    CreateUserInOrganization:
                      operationId: createUser
                      parameters:
                        organization_id: '$response.body#/id'
          /users:
            post:
              operationId: createUser
              summary: Create a user
              requestBody:
                content:
                  application/json:
                    schema:
                      type: object
                      properties:
                        email:
                          type: string
              responses:
                "201":
                  description: Created.
        """;

    private static async Task<OpenApiDocument> LoadSampleDocumentAsync()
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.yaml");
        try
        {
            await File.WriteAllTextAsync(tempFile, SampleSpec);
            var result = await OpenApiDocument.LoadAsync(tempFile, settings);
            return result.Document!;
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Extract_ReturnsOnePathPerSpecPath()
    {
        var document = await LoadSampleDocumentAsync();
        var extractor = new OpenApiGraphExtractor(new DependencyGraph());

        var paths = extractor.Extract(document);

        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, p => p.PathName == "/organizations");
        Assert.Contains(paths, p => p.PathName == "/users");
    }

    [Fact]
    public async Task Extract_RecordsDependency_WhenResponseDeclaresLink()
    {
        var document = await LoadSampleDocumentAsync();
        var dependencyGraph = new DependencyGraph();
        var extractor = new OpenApiGraphExtractor(dependencyGraph);

        var paths = extractor.Extract(document);

        var createOrgOperation = paths
            .SelectMany(p => p.Operations)
            .Single(o => o.OperationId == "createOrganization");
        var link = Assert.Single(createOrgOperation.Response.Links);
        Assert.Equal("createUser", link.OperationId);
    }
}
