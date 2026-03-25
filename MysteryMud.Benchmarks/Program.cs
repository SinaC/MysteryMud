// dotnet run -c Release

using BenchmarkDotNet.Running;
using MysteryMud.Benchmarks.CommandRegistry;

var summary = BenchmarkRunner.Run<TrieBenchmark>();