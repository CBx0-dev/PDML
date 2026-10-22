using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
