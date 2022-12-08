``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
11th Gen Intel Core i7-1195G7 2.90GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT AVX2


```
|                          Method |    Mean |    Error |   StdDev |  Median |
|-------------------------------- |--------:|---------:|---------:|--------:|
|       JsonTransformerLargeInput | 1.214 s | 0.0291 s | 0.0830 s | 1.184 s |
| JsonTransformerLargeTransformer | 1.377 s | 0.0104 s | 0.0097 s | 1.374 s |
