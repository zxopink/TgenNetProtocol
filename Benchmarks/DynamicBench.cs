using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class DynamicBench
    {
        Func<int, int> Func;
        Delegate DelFunc;
        MethodInfo MethodInfo;
        dynamic dynamicFunc;

        [GlobalSetup]
        public void Setup()
        {
            Func = MethodToRun;
            DelFunc = MethodToRun;
            MethodInfo = DelFunc.Method;
            dynamicFunc = DelFunc;
        }
        public int MethodToRun(int num)
        {
            return num * num;
        }

        [Benchmark]
        public void NaiveCall()
        {
            MethodToRun(1);
        }

        [Benchmark]
        public void FuncCall()
        {
            Func(2);
        }

        [Benchmark]
        public void DelegateCall()
        {
            DelFunc.DynamicInvoke(3);
        }

        [Benchmark]
        public void MethodBaseCall()
        {
            MethodInfo.Invoke(this, new object[] {4});
        }

        [Benchmark]
        public void DynamicCall()
        {
            dynamicFunc(5);
        }

        //Benchmark results
        //|         Method |        Mean |     Error |    StdDev |      Median |   Gen0 | Allocated |
        //|--------------- |------------:|----------:|----------:|------------:|-------:|----------:|
        //|      NaiveCall |   0.0093 ns | 0.0172 ns | 0.0169 ns |   0.0000 ns |      - |         - |
        //|       FuncCall |   2.2598 ns | 0.0786 ns | 0.0874 ns |   2.2429 ns |      - |         - |
        //|   DelegateCall | 215.7803 ns | 4.3191 ns | 5.7659 ns | 215.9633 ns | 0.0253 |      80 B |
        //| MethodInfoCall | 132.9276 ns | 2.5045 ns | 2.2201 ns | 133.1037 ns | 0.0253 |      80 B |
        //|    DynamicCall |   9.4991 ns | 0.1616 ns | 0.1433 ns |   9.4909 ns | 0.0076 |      24 B |
    }
}
