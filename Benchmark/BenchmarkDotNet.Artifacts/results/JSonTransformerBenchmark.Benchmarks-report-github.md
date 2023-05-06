``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1555)
11th Gen Intel Core i7-1195G7 2.90GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT AVX2


```
|                          Method |    Mean |    Error |   StdDev |
|-------------------------------- |--------:|---------:|---------:|
|       JsonTransformerLargeInput | 1.119 s | 0.0104 s | 0.0128 s |
| JsonTransformerLargeTransformer | 1.369 s | 0.0130 s | 0.0121 s |
