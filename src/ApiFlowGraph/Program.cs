// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using SharpYaml.Model;



var services = new ServiceCollection();
services.AddOpenApi();

var provider = services.BuildServiceProvider();
var modelClient = provider.GetRequiredService<IOllamaModelClient>();
var dependencyGraph=provider.GetRequiredService<DependencyGraph>();
var settings = provider.GetRequiredService<OpenApiReaderSettings>();
var graphPrompt=provider.GetRequiredService<GraphOnlyPrompt>();
var rawSpecPrompt=provider.GetRequiredService<RawSpecPrompt>();
var openApiGraphExtractor=provider.GetRequiredService<OpenApiGraphExtractor>();
var specPath = System.IO.Path.Combine(AppContext.BaseDirectory, "openapi-spec.yaml");
var result = await OpenApiDocument.LoadAsync(specPath, settings);


var document = result.Document;
var path= openApiGraphExtractor.Extract(document);
var dependencySequence=dependencyGraph.GetSequence();
string spec = File.ReadAllText(specPath);
//var response=await modelClient.GenerateAsync("phi4-mini", graphPrompt.GetSystemPrompt(), graphPrompt.GetUserPrompt((path, dependencySequence)));
var rawResponse= await modelClient.GenerateAsync("phi4-mini", rawSpecPrompt.GetSystemPrompt(), rawSpecPrompt.GetUserPrompt(spec));
Console.WriteLine(rawResponse.OllamaGenerateResponse);

