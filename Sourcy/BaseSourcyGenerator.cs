#pragma warning disable RS1035

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy;

public abstract class BaseSourcyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var incrementalValuesProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName("Sourcy.EnableSourcyAttribute", 
                    static (_, _) => true, 
                    static (syntaxContext, _) =>
                    {
                        var attributeData = syntaxContext.Attributes.FirstOrDefault() ?? throw new ArgumentException("No EnableSourcyAttribute found");
                        
                        var typedConstant = attributeData.ConstructorArguments.FirstOrDefault();

                        if (typedConstant.IsNull)
                        {
                            throw new ArgumentException("No constructor argument found for EnableSourcyAttribute");
                        }
                        
                        return (string)typedConstant.Value!;
                    });
        
        context.RegisterSourceOutput(incrementalValuesProvider, (productionContext, path) => Initialize(productionContext, GetRootDirectory(path)));
    }

    protected abstract void Initialize(SourceProductionContext context, Root root);

    protected static Root GetRootDirectory(string path)
    {
        var location = GetLocation(path);

        while (true)
        {
            if (location == location.Root)
            {
                break;
            }
            
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                break;
            }
            
            if (File.Exists(Path.Combine(location.FullName, ".sourcyroot")))
            {
                break;
            }
            
            var parent = location.Parent;

            if (parent is null || parent == location.Root)
            {
                break;
            }

            location = parent;
        }

        return new Root(location);
    }

    protected static DirectoryInfo GetLocation(string path)
    {
        return new FileInfo(path).Directory!;
    }

    protected static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}