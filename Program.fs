// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open SliceMapPerformance.Domain
open SliceMapPerformance.SliceMap
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

type City = City of int
type Truck = Truck of int

let rng = Random 123
let numberOfCities = 1_000
let numberOfTrucks = 10_000
let densityPercent = 0.01

let cities = [| for c in 1 .. numberOfCities -> City c |]
let trucks = [| for t in 1 .. numberOfTrucks -> Truck t |]
let cityTruckPairs =
    [| 
        for c in cities do
        for t in trucks ->
            if rng.NextDouble() < densityPercent then
                Some (c, t)
            else
                None
    |] |> Array.choose id

let costs =
    [|
        for t in trucks ->
            t, 100.0 + 10.0 * rng.NextDouble()
    |] |> SliceMap

let capacity =
    [|
        for t in trucks ->
            t, 1_000.0 + 10.0 * rng.NextDouble () 
    |] |> SliceMap

let decisions =
    [|
        for (c, t) in cityTruckPairs ->
            let decisionName = DecisionName ($"{c.ToString()}_{t.ToString()}")
            c, t, { Name = decisionName; Type = DecisionType.Boolean }
    |] |> SliceMap2


let profiling () =
    let mutable result = LanguagePrimitives.GenericZero

    for c in cities do
        let total = sum (capacity .* decisions.[c, All] .* costs)
        result <- total

    result


[<MemoryDiagnoser>]
type Benchmarks () =

    [<Benchmark>]
    member _.SumHadamardProduct () =
        let mutable result = LanguagePrimitives.GenericZero

        for c in cities do
            let total = sum (capacity .* decisions.[c, All] .* costs)
            result <- total

        result
    


[<EntryPoint>]
let main argv =

    let summary = BenchmarkRunner.Run<Benchmarks>()
    0 // return an integer exit code