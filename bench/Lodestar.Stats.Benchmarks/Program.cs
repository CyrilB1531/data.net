using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Lodestar.Stats.Benchmarks.StatsBenchmarks).Assembly)
    .Run(args);
