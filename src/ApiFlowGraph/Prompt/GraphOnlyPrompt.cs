using System.Text.Json;

public class GraphOnlyPrompt : Prompt<(List<Path> Paths, List<string> Sequence)>
{
    public override string GetSystemPrompt() => """
        You are writing a SKILL.md-style markdown document from a pre-computed
        execution graph. The graph has already resolved every operation's
        fields and dependency order — do not re-derive, question, or modify
        any field mapping or sequence. Your only job is to phrase it as clear,
        sequential instructions for an agent to follow.

        Output markdown only. No JSON, no commentary outside the skill itself.
        """;

    public override string GetUserPrompt((List<Path> ?Paths, List<string> Sequence) input)
    {
        var json = JsonSerializer.Serialize(new
        {
            operations = input.Paths.SelectMany(p => p.Operations),
            sequence = input.Sequence
        }, new JsonSerializerOptions { WriteIndented = true });

        return $"""
            Execution graph (already resolved — sequence and dependencies are final, do not alter):
            {json}

            Write the skill.
            """;
    }
}