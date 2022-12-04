

using BenchmarkDotNet.Running;
using Benchmarks;
using System.Dynamic;
using TgenNetProtocol;
using TgenSerializer;
using TgenSerializer.Utils;

//var summery = BenchmarkRunner.Run<DynamicBench>();
var summery = BenchmarkRunner.Run<Instantiation>();
ClientManager s = new();
s.Register<int>(mt);
Bytes.GetBytes(5, 5, 5);
void mt(int i)
{
    
}

//var summery = BenchmarkRunner.Run<DelegateCreation>();