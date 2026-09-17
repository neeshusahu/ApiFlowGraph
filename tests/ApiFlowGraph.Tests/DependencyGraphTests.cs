public class DependencyGraphTests
{
    [Fact]
    public void GetSequence_WithNoDependencies_ReturnsEmptyList()
    {
        var graph = new DependencyGraph();

        var sequence = graph.GetSequence();

        Assert.Empty(sequence);
    }

    [Fact]
    public void AddDependency_RegistersBothTaskAndPrerequisite_WithoutThrowing()
    {
        var graph = new DependencyGraph();

        var exception = Record.Exception(() => graph.AddDependency("createUser", "createOrganization"));

        Assert.Null(exception);
    }
}
