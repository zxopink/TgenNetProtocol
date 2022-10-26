

using BenchmarkDotNet.Running;
using Benchmarks;
using System.Dynamic;

//var summery = BenchmarkRunner.Run<DynamicBench>();
var summery = BenchmarkRunner.Run<Instantiation>();

//var summery = BenchmarkRunner.Run<DelegateCreation>();