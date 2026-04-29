// dotnet run -c Release

using BenchmarkDotNet.Running;
using MysteryMud.Benchmarks.ECS;

//var summary = BenchmarkRunner.Run<TrieBenchmark>();
var summary = BenchmarkRunner.Run<EcsBenchmarks>();