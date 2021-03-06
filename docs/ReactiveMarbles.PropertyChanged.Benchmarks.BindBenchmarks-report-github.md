``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8086K CPU 4.00GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]        : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  .NET Core 3.1 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Job=.NET Core 3.1  Runtime=.NET Core 3.1  

```
|           Method | Depth | Changes |           Mean |        Error |       StdDev | Ratio | RatioSD |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------- |------ |-------- |---------------:|-------------:|-------------:|------:|--------:|---------:|---------:|--------:|----------:|
|  **BindAndChangeUI** |     **1** |       **1** |    **27,620.0 ns** |    **160.17 ns** |    **133.75 ns** |  **1.00** |    **0.00** |   **2.2583** |   **0.4272** |       **-** |   **14353 B** |
| BindAndChangeOld |     1 |       1 |    16,192.4 ns |     45.31 ns |     40.17 ns |  0.59 |    0.00 |   1.8921 |   0.4578 |  0.2136 |   12287 B |
| BindAndChangeNew |     1 |       1 |    12,516.2 ns |    103.70 ns |     86.60 ns |  0.45 |    0.00 |   1.4648 |   0.3510 |  0.1678 |    9459 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     1 |       1 |       656.6 ns |      1.73 ns |      1.35 ns |  1.00 |    0.00 |   0.0887 |        - |       - |     560 B |
|        ChangeOld |     1 |       1 |     1,377.7 ns |     15.46 ns |     13.70 ns |  2.10 |    0.02 |   0.1087 |   0.0420 |  0.0210 |     729 B |
|        ChangeNew |     1 |       1 |       152.5 ns |      1.10 ns |      0.97 ns |  0.23 |    0.00 |   0.0076 |        - |       - |      48 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **1** |      **10** |    **30,671.9 ns** |    **193.29 ns** |    **171.35 ns** |  **1.00** |    **0.00** |   **2.6245** |   **0.4883** |       **-** |   **16649 B** |
| BindAndChangeOld |     1 |      10 |    29,169.0 ns |    394.93 ns |    350.09 ns |  0.95 |    0.01 |   2.8687 |   0.7324 |  0.3662 |   18506 B |
| BindAndChangeNew |     1 |      10 |    13,172.9 ns |     82.75 ns |     77.40 ns |  0.43 |    0.00 |   1.5259 |   0.3815 |       - |    9654 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     1 |      10 |     3,305.9 ns |     37.98 ns |     33.67 ns |  1.00 |    0.00 |   0.4463 |        - |       - |    2800 B |
|        ChangeOld |     1 |      10 |    13,630.0 ns |     16.72 ns |     14.82 ns |  4.12 |    0.04 |   1.0834 |   0.4272 |  0.2136 |    7294 B |
|        ChangeNew |     1 |      10 |     1,692.3 ns |     15.86 ns |     13.24 ns |  0.51 |    0.01 |   0.0763 |        - |       - |     480 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **1** |     **100** |    **61,719.4 ns** |    **143.73 ns** |    **134.44 ns** |  **1.00** |    **0.00** |   **6.5918** |   **0.6104** |       **-** |   **41927 B** |
| BindAndChangeOld |     1 |     100 |   152,214.0 ns |    809.61 ns |    717.70 ns |  2.47 |    0.01 |  12.6953 |   4.3945 |  2.1973 |   85179 B |
| BindAndChangeNew |     1 |     100 |    24,814.7 ns |    270.07 ns |    252.62 ns |  0.40 |    0.00 |   2.2278 |   0.4272 |       - |   14082 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     1 |     100 |    33,769.8 ns |    228.86 ns |    191.11 ns |  1.00 |    0.00 |   4.4556 |        - |       - |   28000 B |
|        ChangeOld |     1 |     100 |   136,715.0 ns |    572.43 ns |    507.44 ns |  4.05 |    0.03 |  10.7422 |   3.6621 |  1.7090 |   72909 B |
|        ChangeNew |     1 |     100 |    13,354.8 ns |     41.23 ns |     34.42 ns |  0.40 |    0.00 |   0.7629 |        - |       - |    4800 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **1** |    **1000** |   **373,543.0 ns** |  **1,871.60 ns** |  **1,750.70 ns** |  **1.00** |    **0.00** |  **46.3867** |   **1.4648** |       **-** |  **293786 B** |
| BindAndChangeOld |     1 |    1000 | 1,410,637.3 ns | 21,016.27 ns | 19,658.63 ns |  3.78 |    0.05 | 109.3750 |  37.1094 | 17.5781 |  741263 B |
| BindAndChangeNew |     1 |    1000 |   139,454.3 ns |    393.69 ns |    328.75 ns |  0.37 |    0.00 |   9.0332 |   0.4883 |       - |   57254 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     1 |    1000 |   335,426.6 ns |    834.84 ns |    740.06 ns |  1.00 |    0.00 |  44.4336 |        - |       - |  280001 B |
|        ChangeOld |     1 |    1000 | 1,363,816.7 ns | 12,329.92 ns | 11,533.41 ns |  4.07 |    0.04 | 107.4219 |  41.0156 | 19.5313 |  728931 B |
|        ChangeNew |     1 |    1000 |   125,979.8 ns |    191.22 ns |    149.29 ns |  0.38 |    0.00 |   7.5684 |        - |       - |   48000 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **2** |       **1** |    **35,542.7 ns** |    **384.65 ns** |    **359.80 ns** |  **1.00** |    **0.00** |   **2.9297** |   **0.5493** |       **-** |   **18470 B** |
| BindAndChangeOld |     2 |       1 |    28,377.0 ns |    301.45 ns |    281.97 ns |  0.80 |    0.01 |   3.0518 |   0.7629 |  0.3662 |   19738 B |
| BindAndChangeNew |     2 |       1 |    23,575.6 ns |    273.36 ns |    255.70 ns |  0.66 |    0.01 |   2.5940 |   0.6409 |  0.3052 |   16786 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     2 |       1 |       910.5 ns |      4.62 ns |      4.33 ns |  1.00 |    0.00 |   0.1078 |        - |       - |     680 B |
|        ChangeOld |     2 |       1 |     1,391.6 ns |      7.09 ns |      6.28 ns |  1.53 |    0.01 |   0.1087 |   0.0420 |  0.0210 |     729 B |
|        ChangeNew |     2 |       1 |       145.4 ns |      0.65 ns |      0.61 ns |  0.16 |    0.00 |   0.0076 |        - |       - |      48 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **2** |      **10** |    **52,460.5 ns** |    **600.42 ns** |    **561.63 ns** |  **1.00** |    **0.00** |   **4.4556** |   **1.0986** |       **-** |   **28248 B** |
| BindAndChangeOld |     2 |      10 |    62,184.8 ns |    665.16 ns |    589.65 ns |  1.19 |    0.02 |   5.9814 |   1.5869 |  0.7324 |   38454 B |
| BindAndChangeNew |     2 |      10 |    45,247.7 ns |    533.82 ns |    473.21 ns |  0.86 |    0.01 |   4.5776 |   1.0986 |  0.5493 |   29666 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     2 |      10 |    17,646.4 ns |     75.90 ns |     63.38 ns |  1.00 |    0.00 |   1.6479 |   0.3967 |       - |   10475 B |
|        ChangeOld |     2 |      10 |    33,328.4 ns |    311.86 ns |    291.72 ns |  1.89 |    0.02 |   2.9907 |   0.8545 |  0.4272 |   19417 B |
|        ChangeNew |     2 |      10 |    19,791.7 ns |     96.57 ns |     90.33 ns |  1.12 |    0.01 |   2.0142 |   0.5493 |  0.2747 |   12922 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **2** |     **100** |   **245,380.3 ns** |  **2,519.72 ns** |  **2,356.95 ns** |  **1.00** |    **0.00** |  **22.2168** |   **5.3711** |       **-** |  **139980 B** |
| BindAndChangeOld |     2 |     100 |   424,417.1 ns |  3,199.64 ns |  2,992.94 ns |  1.73 |    0.02 |  38.0859 |   9.7656 |  4.8828 |  244393 B |
| BindAndChangeNew |     2 |     100 |   274,482.4 ns |  1,449.71 ns |  1,356.06 ns |  1.12 |    0.01 |  27.3438 |   7.3242 |  3.4180 |  176998 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     2 |     100 |   199,289.7 ns |  2,556.87 ns |  2,391.70 ns |  1.00 |    0.00 |  19.2871 |   4.6387 |       - |  122212 B |
|        ChangeOld |     2 |     100 |   383,722.0 ns |  4,154.59 ns |  3,682.94 ns |  1.92 |    0.03 |  34.6680 |   9.7656 |  4.8828 |  225193 B |
|        ChangeNew |     2 |     100 |   244,302.5 ns |  1,305.19 ns |  1,220.87 ns |  1.23 |    0.02 |  24.9023 |   6.8359 |  3.4180 |  160270 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **2** |    **1000** | **2,151,126.1 ns** | **16,679.62 ns** | **13,928.24 ns** |  **1.00** |    **0.00** | **195.3125** |  **46.8750** |       **-** | **1240118 B** |
| BindAndChangeOld |     2 |    1000 | 3,880,528.9 ns | 27,268.92 ns | 24,173.19 ns |  1.80 |    0.02 | 351.5625 |  89.8438 | 42.9688 | 2272768 B |
| BindAndChangeNew |     2 |    1000 | 2,524,565.7 ns | 11,757.41 ns | 10,422.64 ns |  1.17 |    0.01 | 250.0000 |  66.4063 | 31.2500 | 1619866 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     2 |    1000 | 2,071,413.0 ns | 20,984.01 ns | 19,628.46 ns |  1.00 |    0.00 | 191.4063 |  46.8750 |       - | 1222119 B |
|        ChangeOld |     2 |    1000 | 3,813,487.5 ns | 39,109.93 ns | 34,669.93 ns |  1.84 |    0.02 | 347.6563 |  97.6563 | 46.8750 | 2252191 B |
|        ChangeNew |     2 |    1000 | 2,444,226.1 ns | 19,783.21 ns | 16,519.87 ns |  1.18 |    0.01 | 246.0938 |  70.3125 | 35.1563 | 1601425 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **3** |       **1** |    **40,885.2 ns** |    **444.56 ns** |    **371.22 ns** |  **1.00** |    **0.00** |   **3.5400** |   **0.8545** |       **-** |   **22443 B** |
| BindAndChangeOld |     3 |       1 |    38,272.9 ns |    257.45 ns |    240.82 ns |  0.94 |    0.01 |   4.1504 |   1.1597 |  0.5493 |   26967 B |
| BindAndChangeNew |     3 |       1 |    32,580.9 ns |    202.51 ns |    189.43 ns |  0.80 |    0.01 |   3.7231 |   0.9155 |  0.4272 |   23899 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     3 |       1 |     1,109.8 ns |      3.50 ns |      3.28 ns |  1.00 |    0.00 |   0.1183 |        - |       - |     752 B |
|        ChangeOld |     3 |       1 |     1,411.1 ns |     12.77 ns |     11.94 ns |  1.27 |    0.01 |   0.1087 |   0.0420 |  0.0210 |     729 B |
|        ChangeNew |     3 |       1 |       143.0 ns |      0.24 ns |      0.20 ns |  0.13 |    0.00 |   0.0076 |        - |       - |      48 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **3** |      **10** |    **69,756.5 ns** |    **499.33 ns** |    **389.85 ns** |  **1.00** |    **0.00** |   **6.2256** |   **0.1221** |       **-** |   **39166 B** |
| BindAndChangeOld |     3 |      10 |    92,550.3 ns |    428.57 ns |    400.88 ns |  1.33 |    0.01 |   9.0332 |   2.4414 |  1.2207 |   58189 B |
| BindAndChangeNew |     3 |      10 |    73,810.2 ns |    343.93 ns |    321.71 ns |  1.06 |    0.01 |   7.5684 |   2.0752 |  0.9766 |   49195 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     3 |      10 |    29,422.8 ns |    141.11 ns |    125.09 ns |  1.00 |    0.00 |   2.7771 |   0.6714 |  0.3052 |   17949 B |
|        ChangeOld |     3 |      10 |    54,344.4 ns |    279.23 ns |    247.53 ns |  1.85 |    0.01 |   4.9438 |   1.4038 |  0.6714 |   31908 B |
|        ChangeNew |     3 |      10 |    40,122.7 ns |    452.34 ns |    423.12 ns |  1.37 |    0.02 |   3.9063 |   1.0986 |  0.4883 |   25344 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **3** |     **100** |   **387,821.7 ns** |  **3,086.77 ns** |  **2,887.36 ns** |  **1.00** |    **0.00** |  **36.1328** |   **8.7891** |  **4.3945** |  **231889 B** |
| BindAndChangeOld |     3 |     100 |   673,668.2 ns |  6,133.71 ns |  5,737.47 ns |  1.74 |    0.02 |  62.5000 |  17.5781 |  8.7891 |  401652 B |
| BindAndChangeNew |     3 |     100 |   505,993.4 ns |  1,938.63 ns |  1,813.40 ns |  1.30 |    0.01 |  51.7578 |  13.6719 |  6.8359 |  333133 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     3 |     100 |   341,502.5 ns |  2,110.59 ns |  1,974.25 ns |  1.00 |    0.00 |  32.7148 |   7.8125 |  3.4180 |  209643 B |
|        ChangeOld |     3 |     100 |   631,230.1 ns |  1,512.58 ns |  1,340.86 ns |  1.85 |    0.01 |  57.6172 |  16.6016 |  7.8125 |  375368 B |
|        ChangeNew |     3 |     100 |   481,162.2 ns |  2,016.84 ns |  1,684.15 ns |  1.41 |    0.01 |  47.8516 |  13.6719 |  6.3477 |  309301 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|  **BindAndChangeUI** |     **3** |    **1000** | **3,566,633.8 ns** | **14,701.72 ns** | **13,032.69 ns** |  **1.00** |    **0.00** | **332.0313** |  **78.1250** | **35.1563** | **2148345 B** |
| BindAndChangeOld |     3 |    1000 | 6,534,071.4 ns | 49,922.78 ns | 46,697.80 ns |  1.83 |    0.01 | 593.7500 | 156.2500 | 78.1250 | 3837140 B |
| BindAndChangeNew |     3 |    1000 | 5,017,718.1 ns | 41,376.23 ns | 38,703.35 ns |  1.41 |    0.01 | 492.1875 | 125.0000 | 62.5000 | 3172843 B |
|                  |       |         |                |              |              |       |         |          |          |         |           |
|         ChangeUI |     3 |    1000 | 3,539,549.8 ns | 40,800.92 ns | 38,165.20 ns |  1.00 |    0.00 | 328.1250 |  82.0313 | 39.0625 | 2126607 B |
|        ChangeOld |     3 |    1000 | 6,389,873.3 ns | 29,835.57 ns | 26,448.45 ns |  1.80 |    0.02 | 593.7500 | 164.0625 | 78.1250 | 3809891 B |
|        ChangeNew |     3 |    1000 | 4,961,987.7 ns | 12,696.83 ns |  9,912.85 ns |  1.40 |    0.02 | 484.3750 | 132.8125 | 62.5000 | 3148896 B |
