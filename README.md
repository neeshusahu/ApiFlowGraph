# ApiFlowGraph

Generates an agent-facing "skill" document from an OpenAPI spec, with API operations ordered by their actual data dependencies instead of the order they happen to appear in the spec.

## What it does

Given an OpenAPI spec:

1. **Extracts** every path/operation (request body shape, response codes, and any `links` declared on a response) into a plain object model (`OpenApiGraphExtractor`).
2. **Builds a dependency graph** from those `links`: if operation A's response `links` to operation B, A must run before B. The graph is topologically sorted (`DependencyGraph`) to produce a valid call order, and detects cycles.
3. **Prompts a local LLM** (via [Ollama](https://ollama.com), model `phi4-mini`) with the resolved graph to write a `SKILL.md`-style markdown document: step-by-step, agent-facing instructions for calling the API in the correct order, including which field from an earlier response must be threaded into a later request.

There are three prompt strategies (`src/ApiFlowGraph/Prompt/`), which trade off how much they trust the pre-computed graph versus the raw spec:

| Prompt | Input given to the model |
|---|---|
| `GraphOnlyPrompt` | Only the resolved graph (operations + call sequence). |
| `GraphWithSpecPrompt` | The resolved graph (as ground truth) plus the full spec, for richer descriptions/examples. |
| `RawSpecPrompt` | The raw spec only; the model derives sequencing and field mappings itself. |

`Program.cs` currently wires up `GraphOnlyPrompt`.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) running locally on `http://localhost:11434` with the `phi4-mini` model pulled:
  ```
  ollama pull phi4-mini
  ```

## Project structure

```
src/ApiFlowGraph/
  Program.cs                  entry point: load spec, extract graph, prompt the model
  OpenApiGraphExtractor.cs    walks an OpenApiDocument into Path/Operation/Response models
  Dependency/DependencyGraph.cs  topological sort over operation links
  Models/                     Path, Operation, RequestBody, Response, Link, Properties
  Prompt/                     the three prompt strategies described above
  openapi-spec.yaml           sample spec used by Program.cs

tests/ApiFlowGraph.Tests/     xUnit tests for the extractor and dependency graph
```

## Running

```
dotnet build ApiFlowGraph.sln
dotnet run --project src/ApiFlowGraph/ApiFlowGraph.csproj
```

This loads `src/ApiFlowGraph/openapi-spec.yaml`, resolves the dependency graph, and prints the generated skill markdown to the console.

### Sample output (`GraphOnlyPrompt`)

Running against the bundled `openapi-spec.yaml` (an organization/user API with a `links` dependency between them), using `GraphOnlyPrompt` (only the resolved graph is given to the model), produces something like:

````markdown
# Skill: Create Organization and Add User

## Instructions:

1. **Create an organization**
   - Send a POST request to create a new organization.
     ```json
     {
       "name": "<organization_name>",
       "domain_data": [],
       "external_id": "",
       "metadata": {}
     }
     ```
   - The response will contain the `id` of the newly created organization.

2. **Create a user**
   - Send a POST request to create a new user and assign them to an existing organization using its ID.
     ```json
     {
       "email": "<user_email>",
       "organization_id": "<organization_id_from_step_1>",
       "first_name": "<user_first_name>",
       "last_name": "<user_last_name>"
     }
     ```
   - The response will confirm the creation of a new user.

## Example:

```markdown
# Step 1: Create Organization

- Name: `Acme Corp`
- Domain Data: []
- External ID: (optional)
- Metadata: {}

POST /organizations HTTP/1.1
Content-Type: application/json

{
  "name": "Acme Corp",
  "domain_data": [],
  "external_id": "",
  "metadata": {}
}

# Step 2: Create User and Assign to Organization

Assuming the organization ID from step 1 is `org-12345`

POST /users HTTP/1.1
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "organization_id": "org-12345",
  "first_name": "John",
  "last_name": "Doe"
}
```
````

Note the correct ordering: `createOrganization` before `createUser`, with `organization_id` threaded from the first response into the second request — exactly the dependency the `links` object in the spec declares.

### Sample output (`RawSpecPrompt`)

A separately generated skill for the same spec, this time using `RawSpecPrompt` (the raw spec file contents passed as a plain string, with no pre-computed graph):

````markdown
# Skill to Create Organization and User with Dependency Links

## Steps:

1. Call `/organizations` endpoint using `createOrganization`
   - Required fields for request body are `[name, domain_data]`.
2. Extract `id` from response of step 1.
3. Use extracted `id` as the value for field `o_id` in a new call to `/users`.

## Skill Execution:

### Step 1: Create Organization
- Call endpoint: POST /organizations with body containing:
  - name (string): A descriptive name for the organization, does not need to be unique.
  - domain_data (array of objects):
    - Each object contains `domain` and `state`.
      - Example Object in array:
        ```json
        {
          "domain": string,
          "state": oneOf ["pending", "verified"]
        }
        ```
- Required field: external_id (string): Your own identifier for this organization, used to map it back into your system.
  - Optional fields can include metadata as key/value pairs.

### Step 2: Extract Organization ID
- From the response of step 1:
  ```json
  {
    "id": string,
    ...
  }
  ```
- Store `response.body#/id` for use in subsequent steps or requests to `/users`.

### Step 3: Create User and Link with Organization ID (o_id)
- Call endpoint: POST /users with body containing at least:
  - email (string): Must be a valid format.
  - o_id (string): The organization id extracted from step 1.

Optional fields for user creation can include first_name, last_name. These are not required but may enhance the user's profile information if provided.
````

### Known inaccuracies in generated skills

Checked against `openapi-spec.yaml`'s actual `required` arrays, the model doesn't always get required-vs-optional right:

- The sample above claims required fields for `/organizations` are `[name, domain_data]`. The spec's `required` array is `[name]` only (`openapi-spec.yaml:25`) — `domain_data` is optional.
- It separately labels `external_id` as a "Required field". `external_id` isn't in the `required` list at all (`openapi-spec.yaml:41-43`) — it's optional, same as `metadata`.

## Testing

```
dotnet test tests/ApiFlowGraph.Tests/ApiFlowGraph.Tests.csproj
```


