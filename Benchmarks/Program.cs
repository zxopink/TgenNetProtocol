

using BenchmarkDotNet.Running;
using Benchmarks;
using System.Dynamic;
using TgenNetProtocol;

//var summery = BenchmarkRunner.Run<DynamicBench>();
var summery = BenchmarkRunner.Run<Instantiation>();
ServerManager s = new(6567);
s.Register(mt);

void mt()
{
    
}

//var summery = BenchmarkRunner.Run<DelegateCreation>();