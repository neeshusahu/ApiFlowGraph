# ApiFlowGraph

Generates an agent-facing "skill" document from an OpenAPI spec, with API
calls ordered by their actual data dependencies — not the order they happen
to appear in the spec.

## The problem

Given an API with a real dependency (e.g. you must create an organization
before you can create a user under it), an LLM asked to generate agent
instructions directly from the raw spec can get the sequence right but still
invent wrong field names, incorrect required-field lists, or drop response
details — even on a two-operation spec, and inconsistently across repeated
runs on identical input. That unreliability gets worse, not better, as a
spec grows: dependency-finding complexity scales roughly with the square of
the operation count, while an LLM's ability to reliably track relationships
across a large spec doesn't scale to match.

## The approach

1. **Extract** every operation's request/response shape and any `links` it
   declares, into a plain object model (`OpenApiGraphExtractor`).
2. **Build a dependency graph** from those `links` and topologically sort it
   (`DependencyGraph`) to get a correct call order, with cycle detection.
3. **Prompt a local LLM** (Ollama, `phi4-mini`) with the *resolved* graph to
   phrase the result as a `SKILL.md`-style document — the model's job is
   wording, not discovering the dependency.

Three prompt strategies are implemented to compare this directly:

| Prompt | Given to the model |
|---|---|
| `RawSpecPrompt` | Raw spec text only — model must find dependencies itself |
| `GraphOnlyPrompt` | Only the pre-resolved graph |
| `GraphWithSpecPrompt` | Resolved graph as ground truth, plus raw spec for richer phrasing |

## Sample output

Given a spec where creating a user requires an existing organization's `id`
(declared via an OpenAPI `links` object), `GraphOnlyPrompt` produces:

````markdown
# Skill: Create Organization and Add User

1. **Create an organization** — POST /organizations
   { "name": "<organization_name>", "domain_data": [], "external_id": "", "metadata": {} }
   The response contains the new organization's `id`.

2. **Create a user** — POST /users, using the `id` from step 1
   { "email": "<user_email>", "organization_id": "<id_from_step_1>",
     "first_name": "<user_first_name>", "last_name": "<user_last_name>" }
````

Correct ordering, and `id` is correctly threaded into `organization_id` —
exactly the dependency the spec's `links` object declares.

For comparison, `RawSpecPrompt` (raw spec text only, no pre-computed graph)
on the same spec produces:

````markdown
# Skill to Create Organization and User with Dependency Links

1. Call `/organizations` using `createOrganization`
   - Required fields: `[name, domain_data]`
2. Extract `id` from the response.
3. Use extracted `id` as `organization_id` in a call to `/users`.

### Step 1: Create Organization
- name (string): required
- domain_data (array of { domain, state }): required
- Required field: external_id — your own identifier for this organization

### Step 3: Create User
- email (string), organization_id (string): from step 1
- Optional: first_name, last_name
````

Sequencing and field-threading are still correct here too — but this run
claims `domain_data` and `external_id` are required. They aren't (see below).

## What testing found

Comparing `RawSpecPrompt` against a known-correct answer (verified line-by-line
against the spec's own `required` arrays):

- Got the **call sequence and field-threading right** (organization → user,
  `id` → `organization_id`).
- **Got required-vs-optional wrong** — claimed `domain_data` and
  `external_id` were required; the spec marks both optional
  (`openapi-spec.yaml:25`, `:41-43`). This happened with the correct answer
  sitting in plain text directly in front of the model — not a missing-context
  problem, a using-the-context-correctly problem.
- **Output completeness varied between runs** on identical input — some runs
  dropped fields present in earlier runs of the same prompt.

This is the core case for the graph layer: pulling the relevant fact out and
handing it to the model in isolation is more reliable than trusting the model
to find and use it correctly inside a larger document, and it doesn't degrade
as the document grows, since extraction is a fixed, linear pass over the spec.

**Known extractor gaps** (so `GraphOnlyPrompt` isn't fully tested on the same
question yet): `required` field lists and nested array/object item shapes
aren't currently propagated by the extractor — next fix, not yet done.

## Running it

```
ollama pull phi4-mini   # once
dotnet build ApiFlowGraph.sln
dotnet run --project src/ApiFlowGraph/ApiFlowGraph.csproj
dotnet test tests/ApiFlowGraph.Tests/ApiFlowGraph.Tests.csproj
```

## Structure

```
src/ApiFlowGraph/
  OpenApiGraphExtractor.cs      spec -> Path/Operation/Response models
  Dependency/DependencyGraph.cs topological sort over links
  Prompt/                       the three strategies above
tests/ApiFlowGraph.Tests/       xUnit tests for extractor + graph
```