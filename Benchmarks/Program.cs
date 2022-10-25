

using BenchmarkDotNet.Running;
using Benchmarks;

var summery = BenchmarkRunner.Run<DynamicBench>();