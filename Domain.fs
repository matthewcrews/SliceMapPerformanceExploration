module rec SliceMapPerformance.Domain

type DecisionType =
    | Boolean
    | Integer of LowerBound:float * UpperBound:float
    | Continuous of LowerBound:float * UpperBound:float

type DecisionName = DecisionName of string

type Decision = {
    Name : DecisionName
    Type : DecisionType
} with

    static member ( + ) (l: float, r: Decision) =
        LinearExpr.Add (LinearExpr.Float l, LinearExpr.Decision r)

    static member ( + ) (l: Decision, r: Decision) =
        LinearExpr.Add (LinearExpr.Decision l, LinearExpr.Decision r)

    static member ( * ) (l: float, r: Decision) =
        LinearExpr.Scale (l, LinearExpr.Decision r)


[<RequireQualifiedAccess>]
type LinearExpr =
    | Float of float
    | Decision of Decision
    | Scale of scale: float * expr: LinearExpr
    | Add of lExpr: LinearExpr * rExpr: LinearExpr

    static member ( + ) (l: float, r: LinearExpr) =
        LinearExpr.Add (LinearExpr.Float l, r)

    static member ( + ) (l: Decision, r: LinearExpr) =
        LinearExpr.Add (LinearExpr.Decision l, r)

    static member ( + ) (l: LinearExpr, r: LinearExpr) =
        LinearExpr.Add (l, r)

    static member ( * ) (l: float, r: LinearExpr) =
        LinearExpr.Scale (l, r)

    static member ( * ) (l: LinearExpr, r: float) =
        LinearExpr.Scale (r, l)

    static member Zero = LinearExpr.Float 0.0