# SliceMapPerformanceExploration

A repo for code examples for a blog series on F# performance. If you would like to run the Benchmarks yourself, build the solution using the `Release` settings. Navigate to the output directory which should be `./SliceMapPerformanceExploration/bin/Release/net5.0` and run the following command:

```
dotnet SliceMapPerformanceExploration.dll benchmark
```

If you would like to run the loops for profiling you must build with `Release` settings and navigate to the output directory as well. This time you can choose to run the test you would like to profile: "profiledense", "profilemedium" or "profilesparse". The second argument is the number of loops you want it to repeat.

An example of running the Sparse loop 1000 times:

```cmd
dotnet SliceMapPerformanceExploration.dll profilesparse 1000
```

The `main` branch holds the best performing approach I have found so far so if you want to see if you can improve it, `main` is the branch to fork and play with. Unless of course you want to start from scratch 😊. Even the types in `Domain.fs` can be changed as long as the final goal is representable. Cheers and happy benchmarking!
