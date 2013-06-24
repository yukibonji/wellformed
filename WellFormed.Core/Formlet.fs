﻿namespace WellFormed.Core

open System

open System.Collections.Generic

open System.Windows
open System.Windows.Controls

type Result<'T> =
    | Success of 'T
    | Failure of string list

type Formlet<'T> = 
    {
        Rebuild : FrameworkElement -> FrameworkElement
        Collect : FrameworkElement -> Result<'T>
    }
    static member New rebuild (collect : FrameworkElement -> Result<'T>) = { Rebuild = rebuild; Collect = collect; }

module Formlet =

    let Fail (f : string) = Failure [f] 
                       
    let MapResult (m : Result<'T> -> Result<'U>) (f : Formlet<'T>) : Formlet<'U> = 
        let rebuild (ui :FrameworkElement) = f.Rebuild ui
        let collect (ui :FrameworkElement) = m (f.Collect ui)
        Formlet.New rebuild collect

    let Map (m : 'T -> 'U) (f : Formlet<'T>) : Formlet<'U> = 
        let m' r =
            match r with 
                |   Success v   -> Success (m v)
                |   Failure s   -> Failure s
        MapResult m' f

    let Join (f: Formlet<Formlet<'T>>) : Formlet<'T> = 
        let rebuild (ui :FrameworkElement) = 
            let collect = ApplyToElement ui (Fail "") (fun ui' -> f.Value.Collect(ui'))
            collect
        let collect (ui :FrameworkElement) = m (f.Collect ui)
        Formlet.New rebuild collect

    let Bind<'T1, 'T2> (f : Formlet<'T1>) (b : 'T1 -> Formlet<'T2>) : Formlet<'T2> = 
        f |> Map b |> Join


    let Return (x : 'T) : Formlet<'T> = 
        let rebuild (ui :FrameworkElement) = CreateElement ui (fun () -> new ReturnControl()) :> FrameworkElement
        let collect (ui :FrameworkElement) = Success x
        Formlet.New rebuild collect

    let Delay (f : unit -> Formlet<'T>) : Formlet<'T> = 
        let f' = lazy (f())
        let rebuild (ui :FrameworkElement) = 
            let result = CreateElement ui (fun () -> new DelayControl())
            result.Value <- f'.Value.Rebuild(result.Value)
            result :> FrameworkElement
        let collect (ui :FrameworkElement) = ApplyToElement ui (Fail "") (fun ui' -> f'.Value.Collect(ui'))
        Formlet.New rebuild collect

    let ReturnFrom (f : Formlet<'T>) = f

    type FormletBuilder() =
        member this.Return x = Return x
        member this.Bind(x, f) = Bind x f
        member this.Delay f = Delay f
        member this.ReturnFrom f = ReturnFrom f

    let Do = new FormletBuilder()


