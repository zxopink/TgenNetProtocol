

using BenchmarkDotNet.Running;
using Benchmarks;
using System.Dynamic;
using TgenNetProtocol;

//var summery = BenchmarkRunner.Run<DynamicBench>();
var summery = BenchmarkRunner.Run<Instantiation>();
ClientManager s = new();
s.Register<int>(mt);

void mt(int i)
{
    
}

//var summery = BenchmarkRunner.Run<DelegateCreation>();