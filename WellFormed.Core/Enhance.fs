﻿// ----------------------------------------------------------------------------------------------
// Copyright (c) Mårten Rånge.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// If you cannot locate the  Microsoft Public License, please send an email to 
// dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
//  by the terms of the Microsoft Public License.
// ----------------------------------------------------------------------------------------------
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------------------------

namespace WellFormed.Core

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Media

module Enhance = 
    
    let Many initialCount (f : Formlet<'T>) : Formlet<'T array> = 
        let rebuild (fe : FrameworkElement) =   let many = fe |> CreateElement (fun () -> new ManyElement(initialCount))
                                                let inner = many.Inner
                                                for i in 0..inner.Count - 1 do
                                                    inner.[i] <- f.Rebuild inner.[i]
                                                many :> FrameworkElement
        let collect (fe : FrameworkElement) =   fe |> CollectFromElement (fun (ui : ManyElement) -> let inner = ui.Inner
                                                                                                    let failures = HashSet<Failure>()
                                                                                                    let result = Array.create inner.Count Unchecked.defaultof<'T>
                                                                                                    for i in 0..inner.Count - 1 do
                                                                                                        let fe = inner.[i]
                                                                                                        let collect = f.Collect fe
                                                                                                        result.[i] <- collect.Value
                                                                                                        for failure in collect.Failures do
                                                                                                            ignore <| failures.Add failure
                                                                                                    Collect.New result (failures |> Seq.toList)
                                                                                                    )

        Formlet.New rebuild collect

    let WithLegend (t : string) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   let legend = fe |> CreateElement (fun () -> new LegendElement())
                                                legend.Text <- t
                                                legend.Inner <- f.Rebuild legend.Inner
                                                legend :> FrameworkElement
        let collect (fe : FrameworkElement) =   AppendFailureContext t <| CollectFromElement (fun (ui : LegendElement) -> f.Collect ui.Inner) fe

        Formlet.New rebuild collect

    let WithStyle (style : Style) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.Style <- style)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithWidth (width : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.Width <- width)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithHeight (height : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.Height <- height)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithMaxWidth (maxWidth : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.MaxWidth <- maxWidth)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithMaxHeight (maxHeight : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.MaxHeight <- maxHeight)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithMinWidth (minWidth : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.MinWidth <- minWidth)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithMinHeight (minHeight : double) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.MinHeight <- minHeight)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithLayoutTransform (transform : Transform) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.LayoutTransform <- transform)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithRenderTransform (transform : Transform) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   fe |> f.Rebuild |> ProcessElement (fun ui' -> ui'.RenderTransform <- transform)
        let collect (fe : FrameworkElement) =   f.Collect fe

        Formlet.New rebuild collect

    let WithLabel (t : string) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   let label = fe |> CreateElement (fun () -> new LabelElement(100.0)) 
                                                label.Text <- t
                                                label.Right <- f.Rebuild label.Right
                                                label :> FrameworkElement
        let collect (fe : FrameworkElement) =   AppendFailureContext t <| CollectFromElement (fun (ui : LabelElement) -> f.Collect ui.Right) fe

        Formlet.New rebuild collect

    let WithErrorSummary (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   let summary = fe |> CreateElement (fun () -> new ErrorSummaryElement()) 
                                                summary.Right <- f.Rebuild summary.Right
                                                summary :> FrameworkElement
        let collect (fe : FrameworkElement) =   fe |> CollectFromElement (fun (ui : ErrorSummaryElement) -> let collect = f.Collect ui.Right
                                                                                                            ui.Failures <- collect.Failures
                                                                                                            collect
                                                                                                            )

        Formlet.New rebuild collect

    let WithErrorVisual (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   f.Rebuild fe 
        let collect (fe : FrameworkElement) =   let collect = f.Collect fe

                                                let layer = AdornerLayer.GetAdornerLayer fe
                                                if layer <> null then
                                                    let adorners = layer.GetAdorners fe ?^? [||]
                                                    let adornerOption = 
                                                        adorners
                                                        |>  Array.tryFind (fun a -> 
                                                            match a with 
                                                            | :? ErrorVisualAdorner -> true
                                                            | _ -> false
                                                            )

                                                    if collect.Failures.Length > 0 then
                                                        match adornerOption with
                                                        |   Some adorner    -> ()
                                                        |   _               -> layer.Add(new ErrorVisualAdorner (fe))
                                                    else
                                                        match adornerOption with
                                                        |   Some adorner    -> layer.Remove (adorner)
                                                        |   _               -> ()
                                                collect
        Formlet.New rebuild collect

 
    let WithValidation (validator : 'T -> string option) (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   f.Rebuild fe 
        let collect (fe : FrameworkElement) =   let result = f.Collect fe
                                                let v = result.Value
                                                let fs = validator v
                                                match fs with
                                                |   Some fs'    ->  { 
                                                                        Value = v  
                                                                        Failures = {Context = []; Message = fs';}::result.Failures
                                                                    }
                                                |   _           -> result
                                                

        Formlet.New rebuild collect

 
    let WithValidation_NonEmpty (f : Formlet<string>) : Formlet<string> = 
        WithValidation (fun s -> if String.IsNullOrWhiteSpace s then Some "Value must not be empty" else None) f

    let WithValidation_Regex (r : Regex) (msg : string) (f : Formlet<string>) : Formlet<string> = 
        WithValidation (fun s -> if r.IsMatch s then None else Some msg) f

    let WithSubmitAndReset (f : Formlet<'T>) : Formlet<'T> = 
        let rebuild (fe : FrameworkElement) =   let submitReset = fe |> CreateElement (fun () -> new SubmitResetElement()) 
                                                submitReset.Right <- f.Rebuild submitReset.Right
                                                submitReset :> FrameworkElement
        let collect (fe : FrameworkElement) =   fe |> CollectFromElement (fun (ui : SubmitResetElement) -> 
                                                    let collect = f.Collect ui.Right
                                                    ui.SubmitAllowed <- collect.Failures.Length = 0
                                                    collect
                                                    )
                                                
                                                

        Formlet.New rebuild collect
 