﻿using System.Diagnostics;
using System.Text;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ArchSourceGenerator;

[Generator]
public class QueryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }

        context.RegisterPostInitializationOutput(initializationContext =>
        {

            var compileTimeStatics = new StringBuilder();
            compileTimeStatics.AppendLine("using System;");
            compileTimeStatics.AppendLine("namespace Arch.Core.Utils;");
            compileTimeStatics.AppendGroups(25);
            
            var delegates = new StringBuilder();
            delegates.AppendLine("using System;");
            delegates.AppendLine("namespace Arch.Core;");
            delegates.AppendForEachDelegates(25);
            delegates.AppendForEachEntityDelegates(25);

            var interfaces = new StringBuilder();
            interfaces.AppendLine("using System;");
            interfaces.AppendLine("using System.Runtime.CompilerServices;");
            interfaces.AppendLine("namespace Arch.Core;");
            interfaces.AppendInterfaces(25);
            interfaces.AppendEntityInterfaces(25);
            
            var references = new StringBuilder();
            references.AppendLine("using System;");
            references.AppendLine("using System.Runtime.CompilerServices;");
            references.AppendLine("using CommunityToolkit.HighPerformance;");
            references.AppendLine("namespace Arch.Core;");
            references.AppendReferences(25);

            var jobs = new StringBuilder();
            jobs.AppendLine("using System;");
            jobs.AppendLine("using System.Runtime.CompilerServices;");
            jobs.AppendLine("using ArrayExtensions = CommunityToolkit.HighPerformance.ArrayExtensions;");
            jobs.AppendLine("namespace Arch.Core;");
            jobs.AppendForEachJobs(25);
            jobs.AppendEntityForEachJobs(25);
            jobs.AppendIForEachJobs(25);
            jobs.AppendIForEachWithEntityJobs(25);

            var acessors = new StringBuilder();
            acessors.AppendLine("using System;");
            acessors.AppendLine("using System.Runtime.CompilerServices;");
            acessors.AppendLine("using JobScheduler;");
            acessors.AppendLine("using Arch.Core.Utils;");
            acessors.AppendLine("using System.Diagnostics.Contracts;");
            acessors.AppendLine("using Arch.Core.Extensions;");
            acessors.AppendLine("using System.Diagnostics.CodeAnalysis;");
            acessors.AppendLine("using ArrayExtensions = CommunityToolkit.HighPerformance.ArrayExtensions;");
            acessors.AppendLine("namespace Arch.Core;");
            acessors.AppendLine($@"
               
                public partial struct Chunk{{
                    {new StringBuilder().AppendChunkHases(25)}
                    {new StringBuilder().AppendChunkIndexGets(25)}        
                    {new StringBuilder().AppendChunkIndexSets(25)}
                }}

                public partial class Archetype{{
                    {new StringBuilder().AppendArchetypeHases(25)}
                    {new StringBuilder().AppendArchetypeGets(25)}
                    {new StringBuilder().AppendArchetypeSets(25)}
                }}
            
                public partial class World{{
                    {new StringBuilder().AppendCreates(25)}
                    {new StringBuilder().AppendWorldHases(25)}
                    {new StringBuilder().AppendWorldGets(25)}
                    {new StringBuilder().AppendWorldSets(25)}
                    {new StringBuilder().AppendWorldAdds(25)}    
                    {new StringBuilder().AppendWorldRemoves(25)}

                    {new StringBuilder().AppendQueryMethods(25)}
                    {new StringBuilder().AppendEntityQueryMethods(25)}
                    {new StringBuilder().AppendParallelQuerys(25)}
                    {new StringBuilder().AppendParallelEntityQuerys(25)}

                    {new StringBuilder().AppendQueryInterfaceMethods(25)}
                    {new StringBuilder().AppendEntityQueryInterfaceMethods(25)}
                    {new StringBuilder().AppendHpParallelQuerys(25)}
                    {new StringBuilder().AppendHpeParallelQuerys(25)}
                }}

               public static partial class EntityExtensions{{
                #if !PURE_ECS
                    {new StringBuilder().AppendEntityHases(25)}
                    {new StringBuilder().AppendEntitySets(25)}
                    {new StringBuilder().AppendEntityGets(25)}
                    {new StringBuilder().AppendEntityAdds(25)}
                    {new StringBuilder().AppendEntityRemoves(25)}
                #endif
                }}

                public partial struct QueryDescription{{
                    {new StringBuilder().AppendQueryDescriptionWithAlls(25)}
                    {new StringBuilder().AppendQueryDescriptionWithAnys(25)}
                    {new StringBuilder().AppendQueryDescriptionWithNones(25)}
                    {new StringBuilder().AppendQueryDescriptionWithExclusives(25)}
                }}
            ");

            initializationContext.AddSource("CompileTimeStatics.g.cs",
                CSharpSyntaxTree.ParseText(compileTimeStatics.ToString()).GetRoot().NormalizeWhitespace().ToFullString());
            
            initializationContext.AddSource("Delegates.g.cs",
                CSharpSyntaxTree.ParseText(delegates.ToString()).GetRoot().NormalizeWhitespace().ToFullString());

            initializationContext.AddSource("Interfaces.g.cs",
                CSharpSyntaxTree.ParseText(interfaces.ToString()).GetRoot().NormalizeWhitespace().ToFullString());
            
            initializationContext.AddSource("References.g.cs",
                CSharpSyntaxTree.ParseText(references.ToString()).GetRoot().NormalizeWhitespace().ToFullString());

            initializationContext.AddSource("Jobs.g.cs",
                CSharpSyntaxTree.ParseText(jobs.ToString()).GetRoot().NormalizeWhitespace().ToFullString());
            
            initializationContext.AddSource("Acessors.g.cs",
                CSharpSyntaxTree.ParseText(acessors.ToString()).GetRoot().NormalizeWhitespace().ToFullString());
        });
    }
}