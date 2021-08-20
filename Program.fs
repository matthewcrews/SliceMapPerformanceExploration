// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open SliceMapPerformance.Domain
open SliceMapPerformance.SliceMap


[<Measure>] type City
[<Measure>] type Truck

let rng = Random 123
let numberOfCities = 1_000
let numberOfTrucks = 10_000
let cities = [| for c in 1 .. numberOfCities -> LanguagePrimitives.Int32WithMeasure<City> c |]
let trucks = [| for t in 1 .. numberOfTrucks -> LanguagePrimitives.Int32WithMeasure<Truck> t|]

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
        SliceMap [|
            for t in trucks ->
                t, 100.0 + 10.0 * rng.NextDouble()
        |]

    let capacity =
        SliceMap [|
            for t in trucks ->
                t, 1_000.0 + 10.0 * rng.NextDouble () 
        |]

    let decisions =
        SliceMap2D [|
            for (c, t) in cityTruckPairs ->
                let decisionName = DecisionName ($"{c.ToString()}_{t.ToString()}")
                c, t, { Name = decisionName; Type = DecisionType.Boolean }
        |]

    let loop () =
        let mutable result = LanguagePrimitives.GenericZero

        for c in cities do
            let total = sum (capacity .* decisions.[c, All] .* costs)
            result <- total

        result

    

[<EntryPoint>]
let main argv =

    for _ = 1 to 1_000 do
        let _ = HighSparsity.loop ()
        ()
    
    0 // return an integer exit code