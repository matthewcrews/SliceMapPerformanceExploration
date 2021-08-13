module SliceMapPerformance.SliceMap


open System
open System.Collections.Generic

[<NoComparison;NoEquality>]
type SliceType<'a when 'a : comparison> =
  | All

[<NoComparison>]
type SliceSet<[<EqualityConditionalOn>]'T when 'T : comparison>(comparer:IComparer<'T>, values:Memory<'T>) =
    let comparer = comparer
    let values = values

    new(values:seq<'T>) =
        let comparer = LanguagePrimitives.FastGenericComparer<'T>
        let v = values |> Seq.distinct |> Seq.toArray |> Array.sort
        SliceSet(comparer, v.AsMemory<'T>())

    member internal _.Comparer = comparer
    member internal _.Values = values

    member _.Union (b: SliceSet<'T>) =
        let newValues = Array.zeroCreate(values.Length + b.Values.Length)

        let mutable aIdx = 0
        let mutable bIdx = 0
        let mutable outIdx = 0

        while (aIdx < values.Length && bIdx < b.Values.Length) do
    
            let c = comparer.Compare(values.Span.[aIdx], b.Values.Span.[bIdx])

            if c < 0 then
                newValues.[outIdx] <- values.Span.[aIdx]
                aIdx <- aIdx + 1
                outIdx <- outIdx + 1
            elif c = 0 then
                newValues.[outIdx] <- values.Span.[aIdx]
                aIdx <- aIdx + 1
                bIdx <- bIdx + 1
                outIdx <- outIdx + 1
            else
                newValues.[outIdx] <- b.Values.Span.[bIdx]
                bIdx <- bIdx + 1
                outIdx <- outIdx + 1

        while aIdx < values.Length do
            newValues.[outIdx] <- values.Span.[aIdx]
            aIdx <- aIdx + 1
            outIdx <- outIdx + 1

        while bIdx < b.Values.Length do
            newValues.[outIdx] <- b.Values.Span.[bIdx]
            bIdx <- bIdx + 1
            outIdx <- outIdx + 1

        SliceSet(comparer, newValues.AsMemory(0, outIdx))

    interface IEnumerable<'T> with
        member _.GetEnumerator(): IEnumerator<'T> = 
            let s = seq { for idx in 0..values.Length-1 -> values.Span.[idx] }
            s.GetEnumerator()

    interface System.Collections.IEnumerable with
        member _.GetEnumerator(): Collections.IEnumerator = 
            let s = seq { for idx in 0..values.Length-1 -> values.Span.[idx] }
            s.GetEnumerator() :> Collections.IEnumerator


    member _.Count =
        values.Length


[<RequireQualifiedAccess>]
module SliceSet =

    let toSeq (a:SliceSet<_>) =
        seq { for i in 0..a.Count - 1 -> a.Values.Span.[i] }

    let slice (f:SliceType<_>) (keys:SliceSet<_>) =
            match f with
            | All -> keys


type TryFind<'Key, 'Value> = 'Key -> 'Value option


module TryFind =

    let ofDictionary (d:Dictionary<'Key,'Value>) : TryFind<'Key, 'Value> =
        fun k -> 
          match d.TryGetValue(k) with
          | true, value -> Some value
          | false, _ -> None

    let ofSeq (s:seq<'Key * 'Value>) : TryFind<'Key, 'Value> =
        let d = Dictionary ()
        
        for (k, v) in s do
                d.[k] <- v

        ofDictionary d

    //let toSeq (keys:seq<_>) (s:TryFind<_,_>) =
    //    let lookup k = s k |> Option.map (fun v -> k, v)
        
    //    keys
    //    |> Seq.choose lookup

    //let toMap keys (s:TryFind<_,_>) =
    //    s |> (toSeq keys) |> Map.ofSeq

    //let equals keys (a:TryFind<_,_>) (b:TryFind<_,_>) =
    //    let mutable result = true

    //    for k in keys do
    //        let aValue = a k
    //        let bValue = b k
    //        if aValue <> bValue then
    //            result <- false

    //    result


    let inline sum keys (tryFind:TryFind<_,_>) =
        let mutable acc = LanguagePrimitives.GenericZero

        for k in keys do
            match tryFind k with
            | Some v -> 
                acc <- acc + v
            | None -> ()

        acc


type ISliceData<'Key, 'Value when 'Key : comparison and 'Value : equality> =
    abstract member Keys : 'Key seq
    abstract member TryFind : TryFind<'Key, 'Value>


[<NoComparison>]
type SliceMap<'Key, 'Value when 'Key : comparison and 'Value : equality> 
    (keys:SliceSet<'Key>, tryFind:TryFind<'Key, 'Value>) =

    let keys = keys
    let tryFind = tryFind

    new (s:seq<'Key * 'Value>) =
        let keys = s |> Seq.map fst |> SliceSet
        let store = TryFind.ofSeq s
        SliceMap (keys, store)

    interface ISliceData<'Key, 'Value> with
        member _.Keys = SliceSet.toSeq keys
        member _.TryFind = tryFind

    member _.Keys = keys
    member _.TryFind = tryFind

    //override this.Equals(obj) =
    //    match obj with
    //    | :? SMap<'Key, 'Value> as other -> 
    //        let mutable result = true
    //        if not (Seq.equals this.Keys other.Keys) then
    //            result <- false

    //        if result then
    //            if not (TryFind.equals this.Keys this.TryFind other.TryFind) then
    //                result <- false

    //        result
    //    | _ -> false

    //override this.GetHashCode () =
    //    hash (this.AsMap())

    //member _.ContainsKey k =
    //    if keyInRange k then
    //        match tryFind k with
    //        | Some _ -> true
    //        | None -> false
    //    else
    //        false


    static member inline (.*) (a:SliceMap<_,_>, b:SliceMap<_,_>) =
        let newKeys = a.Keys.Union b.Keys
        let newTryFind k =
            match (a.TryFind k), (b.TryFind k) with
            | Some lv, Some rv -> Some (lv * rv)
            | _,_ -> None
        SliceMap(newKeys, newTryFind)



    //static member inline Sum (m:SMap<_, _>) =
    //    TryFind.sum m.Keys m.TryFind

    //interface IEnumerable<KeyValuePair<'Key, 'Value>> with
    //    member _.GetEnumerator () : IEnumerator<KeyValuePair<'Key, 'Value>> = 
    //        let s = seq { for key in keys -> tryFind key |> Option.map (fun v -> KeyValuePair(key, v)) } |> Seq.choose id
    //        s.GetEnumerator ()

    //interface System.Collections.IEnumerable with
    //    member _.GetEnumerator () : Collections.IEnumerator = 
    //        let s = seq { for key in keys -> tryFind key |> Option.map (fun v -> KeyValuePair(key, v)) } |> Seq.choose id
    //        s.GetEnumerator () :> Collections.IEnumerator



type SliceMap2<'Key1, 'Key2, 'Value when 'Key1 : comparison and 'Key2 : comparison and 'Value : equality> 
    (keys1:SliceSet<'Key1>, keys2:SliceSet<'Key2>, tryFind:TryFind<('Key1 * 'Key2), 'Value>) =

    let keys1 = keys1
    let keys2 = keys2
    let keys = seq {for k1 in keys1 do for k2 in keys2 -> (k1, k2)}
    let tryFind = tryFind

    new (s:seq<'Key1 * 'Key2 * 'Value>) =
        let keys1 = s |> Seq.map (fun (k, _,_) -> k) |> SliceSet
        let keys2 = s |> Seq.map (fun (_, k,_) -> k) |> SliceSet
        let store = 
            s
            |> Seq.map (fun (k1, k2, v) -> (k1, k2), v)
            |> TryFind.ofSeq
        SliceMap2 (keys1, keys2, store)


    interface ISliceData<('Key1 * 'Key2), 'Value> with
      member _.Keys = keys
      member _.TryFind = tryFind

    member _.Keys1 = keys1
    member _.Keys2 = keys2
    member _.Keys = keys
    member _.TryFind = tryFind

    // Slices
    // 1D
    member this.Item
        with get (k1, k2f) =
            let keys2 = SliceSet.slice k2f this.Keys2
            let newTryFind k = tryFind (k1, k)
            SliceMap (keys2, newTryFind)

    member this.Item
        with get (k1f, k2) =
            let keys1 = SliceSet.slice k1f this.Keys1
            let newTryFind k = tryFind (k, k2)
            SliceMap (keys1, newTryFind)

    static member inline Sum (m:SliceMap2<_,_,_>) =
        TryFind.sum m.Keys m.TryFind


/// <summary>A function which sums the values contained in a SliceMap</summary>
/// <param name="x">An instance of ISliceData</param>
/// <returns>A LinearExpression</returns>
let inline sum (x:ISliceData<'Key, 'Value>) =
    TryFind.sum x.Keys x.TryFind
