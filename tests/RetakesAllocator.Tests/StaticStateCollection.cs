using Xunit;

namespace RetakesAllocator.Tests;

/// <summary>
/// xUnit collection that groups all test classes that read or write the plugin's
/// shared static state (Allocator lists, Votes settings, Utils.Prefix, …).
/// Tests in the same collection run sequentially, preventing race conditions when
/// parallel test classes mutate the same static fields.
/// </summary>
[CollectionDefinition("StaticState", DisableParallelization = true)]
public class StaticStateCollection { }
