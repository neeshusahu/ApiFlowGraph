using System.Text.Json;

public class GraphWithSpecPrompt : Prompt<(List<Path> Paths, List<string> Sequence, string RawSpec)>
{
    public override string GetSystemPrompt() => """
        You are writing a SKILL.md-style markdown document. You are given two
        inputs: (1) a pre-computed execution graph with dependency order and
        field mappings already resolved — treat this as ground truth, never
        re-derive or alter it — and (2) the full OpenAPI source, to use only
        for descriptions, examples, and context that improves phrasing.

        Output markdown only. No JSON, no commentary outside the skill itself.
        """;

    public override string GetUserPrompt((List<Path> Paths, List<string> Sequence, string RawSpec) input)
    {
        var json = JsonSerializer.Serialize(new
        {
            operations = input.Paths.SelectMany(p => p.Operations),
            sequence = input.Sequence
        }, new JsonSerializerOptions { WriteIndented = true });

        return $"""
            Extracted execution graph (ground truth for sequence and dependencies):
            {json}

            Full OpenAPI source (for descriptions/examples only, do not treat as authoritative for sequencing):
            {input.RawSpec}

            Write the skill.
            """;
    }
}