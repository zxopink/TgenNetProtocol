using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class DelegateCreation
    {
        public DelegateCreation()
        {
            Setup();
            CreateMethodData();
        }
        public Delegate Delegate { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Delegate = MethodToRun;
            Prep = new MethodData(Delegate.Method, this);
        }
        public void MethodToRun(int num)
        {
            int x = num * num;
        }
        [Benchmark]
        public void CreateDelegate()
        {
            Delegate.CreateDelegate(typeof(Action<int>), this, nameof(MethodToRun));
        }

        [Benchmark]
        public void CreateMethodData()
        {
            new MethodData(Delegate.Method, this);
        }

        public MethodData Prep;
        [Benchmark]
        public void MethodDataChangeTarget()
        {
            Prep.ChangeTarget(this);
        }

        //|                 Method |     Mean |    Error |   StdDev |   Gen0 |   Gen1 | Allocated |
        //|----------------------- |---------:|---------:|---------:|-------:|-------:|----------:|
        //|         CreateDelegate | 822.1 ns | 16.14 ns | 29.92 ns | 0.0200 |      - |      64 B |
        //|       CreateMethodData | 534.1 ns |  9.66 ns | 14.46 ns | 0.0687 |      - |     216 B |
        //| MethodDataChangeTarget | 372.6 ns |  7.30 ns |  7.17 ns | 0.0353 | 0.0005 |     112 B |
    }
}
