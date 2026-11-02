namespace PDML.Generator.CSharp.Roslyn.Tests;

public class GeneratorTests
{
    [Fact]
    public void GeneratedClasses_ShouldBeInstantiatable()
    {
        Vector2i vector2i = new Vector2i
        {
            X = 20,
            Y = 30
        };
    } 
}
