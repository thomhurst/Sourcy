#pragma warning disable RS1035

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Sourcy;

[Generator]
public class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(postInitializationContext =>
        {
            postInitializationContext.AddSource("EnableSourcyAttribute.g.cs", """
                                                            namespace Sourcy;
                                                            
                                                            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                                                            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                                                            [System.AttributeUsage(System.AttributeTargets.Assembly)]
                                                            public class EnableSourcyAttribute(string FilePath) : System.Attribute;
                                                            """);
        });
    }
}