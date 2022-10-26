

using BenchmarkDotNet.Running;
using Benchmarks;

//var summery = BenchmarkRunner.Run<DynamicBench>();
var summery = BenchmarkRunner.Run<Instantiation>();

//var summery = BenchmarkRunner.Run<DelegateCreation>();