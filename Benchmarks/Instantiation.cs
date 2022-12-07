using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace Benchmarks
{
    internal class NetClass : NetworkBehavour
    {
        [ServerReceiver]
        public void GetStr1(string str) => Console.WriteLine(str);
        [ServerReceiver]
        public void GetStr2(string str) => Console.WriteLine(str);
        [ClientReceiver]
        public void GetStr3(string str) => Console.WriteLine(str);
        [ClientReceiver]
        public void GetStr4(string str) => Console.WriteLine(str);
        [DgramReceiver]
        public void GetStr5(string str) => Console.WriteLine(str);
        [DgramReceiver]
        public void GetStr6(string str) => Console.WriteLine(str);
    }

    [MemoryDiagnoser]
    public class Instantiation
    {
        [GlobalSetup]
        public void Setup()
        {
            new NetClass();
        }
        [Benchmark]
        public void CreateNetObj()
        {
            new NetClass();
        }

        //Old:
        //|       Method |     Mean |   Error |  StdDev |   Gen0 |   Gen1 | Allocated |
        //|------------- |---------:|--------:|--------:|-------:|-------:|----------:|
        //| CreateNetObj | 247.0 us | 4.87 us | 7.73 us | 1.7090 | 0.4883 |   11.3 KB |

        //Old without NetworkBehavour:
        //|          Method |      Mean |     Error |    StdDev | Median | Allocated |
        //|---------------- |----------:|----------:|----------:|-------:|----------:|
        //|    CreateNetObj | 0.0013 ns | 0.0037 ns | 0.0034 ns | 0.0 ns |         - |

        //new:
        //|       Method |     Mean |   Error |  StdDev |   Gen0 |   Gen1 | Allocated |
        //|------------- |---------:|--------:|--------:|-------:|-------:|----------:|
        //| CreateNetObj | 180.1 us | 3.59 us | 4.55 us | 2.6855 | 1.2207 |   9.06 KB |

        //No object cloning
        //|       Method |     Mean |   Error |  StdDev |   Gen0 |   Gen1 | Allocated |
        //|------------- |---------:|--------:|--------:|-------:|-------:|----------:|
        //| CreateNetObj | 181.5 us | 3.29 us | 3.24 us | 2.6855 | 1.2207 |   8.56 KB |

        //|       Method |     Mean |   Error |  StdDev |   Gen0 |   Gen1 | Allocated |
        //|------------- |---------:|--------:|--------:|-------:|-------:|----------:|
        //| CreateNetObj | 131.4 us | 1.71 us | 1.43 us | 1.7090 | 0.7324 |   7.11 KB |

        //|       Method |     Mean |    Error |   StdDev |   Gen0 | Allocated |
        //|------------- |---------:|---------:|---------:|-------:|----------:|
        //| CreateNetObj | 706.2 ns | 12.18 ns | 11.40 ns | 0.3500 |   1.43 KB |

        //Final:
        //|       Method |     Mean |    Error |   StdDev |   Gen0 |   Gen1 | Allocated |
        //|------------- |---------:|---------:|---------:|-------:|-------:|----------:|
        //| CreateNetObj | 875.4 ns | 12.12 ns | 10.12 ns | 0.3500 | 0.0868 |   1.43 KB |
        
    }
}
