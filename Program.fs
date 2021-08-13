// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open SliceMapPerformance.Domain
open SliceMapPerformance.SliceMap
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running


[<Measure>] type City
[<Measure>] type Truck

let rng = Random 123
let numberOfCities = 1_000
let numberOfTrucks = 1_000
let cities = [| for c in 1 .. numberOfCities -> LanguagePrimitives.Int32WithMeasure<City> c |]
let trucks = [| for t in 1 .. numberOfTrucks -> LanguagePrimitives.Int32WithMeasure<Truck> t|]


module Dense =
    
    let cityTruckPairs =
        [| 
            for c in cities do
            for t in trucks ->
                c, t
        |]

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
        |] |> SliceMap2D

    let loop () =
        let mutable result = LanguagePrimitives.GenericZero

        for _ = 1 to 10 do
            for c in cities do
                let total = sum (capacity .* decisions.[c, All] .* costs)
                result <- total

        result


module MediumSparsity =

    let densityPercent = 0.10

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
        |] |> SliceMap2D

    let loop () =
        let mutable result = LanguagePrimitives.GenericZero

        for _ = 1 to 10 do
            for c in cities do
                let total = sum (capacity .* decisions.[c, All] .* costs)
                result <- total

        result


module HighSparsity =
    
    let densityPercent = 0.01

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
        |] |> SliceMap2D

    let loop () =
        let mutable result = LanguagePrimitives.GenericZero

        for _ = 1 to 10 do
            for c in cities do
                let total = sum (capacity .* decisions.[c, All] .* costs)
                result <- total

        result


[<MemoryDiagnoser>]
type Benchmarks () =

    [<Benchmark>]
    member _.DenseData () =
        Dense.loop ()

    [<Benchmark>]
    member _.MediumSparsity () =
        MediumSparsity.loop ()

    [<Benchmark>]
    member _.HighSparsity () =
        HighSparsity.loop ()
    

[<EntryPoint>]
let main argv =

    match argv.[0].ToLower() with
    | "benchmark" ->
        let summary = BenchmarkRunner.Run<Benchmarks>()
        ()

    | "profiledense" ->
        let iterations = int argv.[1]
        printfn "Starting loops"
        for _ = 1 to iterations do
            let _ = Dense.loop ()
            ()

    | "profilemedium" ->
        let iterations = int argv.[1]
        printfn "Starting loops"
        for _ = 1 to iterations do
            let _ = MediumSparsity.loop ()
            ()

    | "profilesparse" ->
        let iterations = int argv.[1]
        printfn "Starting loops"
        for _ = 1 to iterations do
            let _ = HighSparsity.loop ()
            ()

    | _ -> failwith "Invalid workload"
    
    0 // return an integer exit code