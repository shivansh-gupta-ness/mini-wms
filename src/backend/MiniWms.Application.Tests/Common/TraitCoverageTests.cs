using System.Reflection;

namespace MiniWms.Application.Tests.Common;

/// <summary>TASK-006 AC7: the Application proving tests carry their AC trait.</summary>
public class TraitCoverageTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [Trait("AC", "TASK-006.7")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac)
    {
        var expected = $"TASK-006.{ac}";

        var tagged = typeof(TraitCoverageTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

        Assert.True(tagged > 0, $"No test is tagged [Trait(\"AC\", \"{expected}\")].");
    }
}
