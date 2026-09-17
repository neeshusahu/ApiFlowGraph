public class RawSpecPrompt : Prompt<string>
{
    public override string GetSystemPrompt() => """
        You are generating an agent-facing skill (a set of tool-call instructions) from an OpenAPI spec.

        Rules:
        1. Sequence operations in the correct dependency order.
        2. When a response `links` object declares a dependency, explicitly state which field from the earlier call's response must be threaded into which field of the later call's request.
        3. Do not invent any field, endpoint, or type not present in the spec provided.
        4. Output as a SKILL.md-style markdown block only. No commentary, no explanation outside the skill itself.
        """;

    public override string GetUserPrompt(string rawSpec) => $"""
        OpenAPI spec:
        {rawSpec}

        Write the skill.
        """;
}