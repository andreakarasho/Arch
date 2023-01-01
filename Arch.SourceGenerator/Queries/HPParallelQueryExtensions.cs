using System.Text;

namespace ArchSourceGenerator;

public static class StringBuilderHpParallelQueryExtensions
{
    public static void AppendHpParallelQuerys(this StringBuilder builder, int amount)
    {
        for (var index = 0; index < amount; index++)
            builder.AppendHpParallelQuery(index);
    }

    public static void AppendHpParallelQuery(this StringBuilder builder, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);

        var whereT = new StringBuilder().GenericWhereStruct(amount);

        var template = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPParallelQuery<T,{generics}>(in QueryDescription description, ref T iForEach) where T : struct, IForEach<{generics}> {whereT}{{
        
        var innerJob = new IForEachJob<T,{generics}>();
        innerJob.ForEach = iForEach;

        var pool = JobMeta<ChunkIterationJob<IForEachJob<T,{generics}>>>.Pool;
        var query = Query(in description);
        foreach (ref var archetype in query.GetArchetypeIterator()) {{

            var archetypeSize = archetype.Size;
            var part = new RangePartitioner(Environment.ProcessorCount, archetypeSize);
            foreach (var range in part) {{
            
                var job = pool.Get();
                job.Start = range.Start;
                job.Size = range.Length;
                job.Chunks = archetype.Chunks;
                job.Instance = innerJob;
                JobsCache.Add(job);
            }}

            IJob.Schedule(JobsCache, JobHandles);
            JobScheduler.JobScheduler.Instance.Flush();
            JobHandle.Complete(JobHandles);
            JobHandle.Return(JobHandles);

            // Return jobs to pool
            for (var jobIndex = 0; jobIndex < JobsCache.Count; jobIndex++) {{

                var job = Unsafe.As<ChunkIterationJob<IForEachJob<T,{generics}>>>(JobsCache[jobIndex]);
                pool.Return(job);
            }}

            JobHandles.Clear();
            JobsCache.Clear();
        }}
    }}
}}
";

        builder.AppendLine(template);
    }

    public static void AppendHpeParallelQuerys(this StringBuilder builder, int amount)
    {
        for (var index = 0; index < amount; index++)
            builder.AppendHpeParallelQuery(index);
    }

    public static void AppendHpeParallelQuery(this StringBuilder builder, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);

        var whereT = new StringBuilder().GenericWhereStruct(amount);

        var template = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPEParallelQuery<T,{generics}>(in QueryDescription description, ref T iForEach) where T : struct, IForEachWithEntity<{generics}> {whereT} {{
        
        var innerJob = new IForEachWithEntityJob<T,{generics}>();
        innerJob.ForEach = iForEach;

        var pool = JobMeta<ChunkIterationJob<IForEachWithEntityJob<T,{generics}>>>.Pool;
        var query = Query(in description);
        foreach (ref var archetype in query.GetArchetypeIterator()) {{

            var archetypeSize = archetype.Size;
            var part = new RangePartitioner(Environment.ProcessorCount, archetypeSize);
            foreach (var range in part) {{
            
                var job = pool.Get();
                job.Start = range.Start;
                job.Size = range.Length;
                job.Chunks = archetype.Chunks;
                job.Instance = innerJob;
                JobsCache.Add(job);
            }}

            IJob.Schedule(JobsCache, JobHandles);
            JobScheduler.JobScheduler.Instance.Flush();
            JobHandle.Complete(JobHandles);
            JobHandle.Return(JobHandles);

            // Return jobs to pool
            for (var jobIndex = 0; jobIndex < JobsCache.Count; jobIndex++) {{

                var job = Unsafe.As<ChunkIterationJob<IForEachWithEntityJob<T,{generics}>>>(JobsCache[jobIndex]);
                pool.Return(job);
            }}

            JobHandles.Clear();
            JobsCache.Clear();
        }}
    }}
}}
";

        builder.AppendLine(template);
    }
}