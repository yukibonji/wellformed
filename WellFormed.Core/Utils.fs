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
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Threading

[<AutoOpen>]
module internal Utils =

    type Command(canExecute : unit -> bool, execute : unit -> unit) = 
        let canExecuteChanged           = new Event<EventHandler, EventArgs> ()

        interface ICommand with

            member this.CanExecute          (ctx : obj)     = canExecute ()
            member this.Execute             (ctx : obj)     = execute ()

            member this.add_CanExecuteChanged(handler)      = CommandManager.RequerySuggested.AddHandler(handler)
            member this.remove_CanExecuteChanged(handler)   = CommandManager.RequerySuggested.RemoveHandler(handler)


    let rec LastOrDefault defaultTo ls = 
        match ls with
        |   []          -> defaultTo
        |   [v]         -> v
        |   v::vs       -> LastOrDefault defaultTo vs
    let JoinFailures (l : Collect<'U>) (r : Collect<'T>) = 
        {
            Value       = r.Value
            Failures    = l.Failures @ r.Failures
        }

    let AppendFailureContext (ctx : string) (collect : Collect<'T>) = 
        {
            Value       = collect.Value
            Failures    = collect.Failures 
                            |> List.map (fun f -> {Context = ctx::f.Context; Message = f.Message})
        }

    let Coalesce x y = 
        if x <> null then x
        else y

    let (?^?) = Coalesce

    let Success v = Collect.New v []

    let HardFail msg            = failwith msg

    let HardFail_InvalidCase () = HardFail "WellFormed.ProgrammmingError: This case shouldn't be reached"

    let FailWithValue value (msg : string) = Collect.New value [{Context = []; Message = msg;}]

    let Fail<'T> (msg : string)   = Collect.New Unchecked.defaultof<'T> [{Context = []; Message = msg;}]

    let Fail_NeverBuiltUp ()= Fail "WellFormed.ProgrammmingError: Never built up"
             
    let Enumerator (e : array<'T>) = e.GetEnumerator()

    let EmptySize = new Size()
    let EmptyRect = new Rect()

    let TranslateUsingOrientation orientation (fill : bool) (sz : Size) (l : Rect) (r : Size) = 
        match fill,orientation with 
        |   false, TopToBottom  -> Rect (0.0        , l.Bottom  , sz.Width                      , r.Height                      )
        |   false, LeftToRight  -> Rect (l.Right    , 0.0       , r.Width                       , sz.Height                     )
        |   true , TopToBottom  -> Rect (0.0        , l.Bottom  , sz.Width                      , max (sz.Height - l.Bottom) 0.0)
        |   true , LeftToRight  -> Rect (l.Right    , 0.0       , max (sz.Width - l.Right) 0.0  , sz.Height                     )

    let ExceptVertically (l : Size) (r : Size) = 
        Size (max l.Width r.Width, max (l.Height - r.Height) 0.0)

    let ExceptHorizontally (l : Size) (r : Size) = 
        Size (max (l.Width - r.Width) 0.0, max l.Height r.Height)

    let ExceptUsingOrientation (o : LayoutOrientation) (l : Size) (r : Size) =
        match o with
        |   TopToBottom -> ExceptVertically    l r
        |   LeftToRight -> ExceptHorizontally  l r

    let Intersect (l : Size) (r : Size) = 
        Size (min l.Width r.Width, min l.Height r.Height)

    let UnionVertically (l : Size) (r : Size) = 
        Size (max l.Width r.Width, l.Height + r.Height)

    let UnionHorizontally (l : Size) (r : Size) = 
        Size (l.Width + r.Width, max l.Height r.Height)

    let UnionUsingOrientation (o : LayoutOrientation) (l : Size) (r : Size) =
        match o with
        |   TopToBottom -> UnionVertically    l r
        |   LeftToRight -> UnionHorizontally  l r
                       
    let DoNothing() = ()

    let RoutedEventAsDelegate (action : obj -> RoutedEventArgs -> unit) = 
        let a = RoutedEventHandler action
        let d : Delegate = upcast a
        d

    let ActionAsDelegate (action : unit -> unit) = 
        let a = Action action 
        let d : Delegate = upcast a
        d

    let Dispatch (dispatcher : Dispatcher) (action : unit -> unit) = 
        let d = ActionAsDelegate action
        ignore <| dispatcher.BeginInvoke (DispatcherPriority.ApplicationIdle, d)

    let CreateRoutedEvent<'TOwner> name = EventManager.RegisterRoutedEvent (name + "Event", RoutingStrategy.Bubble, typeof<RoutedEventHandler>, typeof<'TOwner>)
    let RaiseRoutedEvent routedEvent (sender : UIElement) = let args = new RoutedEventArgs (routedEvent, sender)
                                                            sender.RaiseEvent args
    let AddRoutedEventHandler routedEvent (receiver : UIElement) (h : obj -> RoutedEventArgs -> unit) = receiver.AddHandler (routedEvent, RoutedEventHandler h)

    let CreatePen br th = 
        let p = new Pen (br, th)
        p.Freeze ()
        p

    let DefaultBackgroundBrush  = Brushes.White
    let DefaultForegroundBrush  = Brushes.Black
    let DefaultBorderBrush      = Brushes.LightBlue
    let DefaultErrorBrush       = Brushes.Red

    let DefaultMargin           = Thickness(4.0)

    let DefaultButtonPadding    = Thickness(16.0,2.0,16.0,2.0)

    let DefaultListBoxItemPadding   = Thickness(24.0,0.0,0.0,0.0)

    let DefaultBorderMargin     = Thickness(0.0,8.0,0.0,0.0)
    let DefaultBorderPadding    = Thickness(0.0,16.0,4.0,8.0)
    let DefaultBorderThickness  = Thickness(2.0)

    type MyListBoxItem () as this =
        inherit ListBoxItem ()

        static let pen      = CreatePen DefaultBorderBrush 1.0
        static let typeFace = Typeface("Calibri")

        static let transform = 
            let transform = Matrix.Identity
            transform.Rotate 90.0
            transform.Translate (DefaultListBoxItemPadding.Left + 2.0, 4.0)
            MatrixTransform (transform)

        let mutable formattedText = Unchecked.defaultof<FormattedText>
        let mutable lastIndex = -1

        do 
            this.HorizontalContentAlignment <- HorizontalAlignment.Stretch
            this.Padding <- DefaultListBoxItemPadding

        override this.OnPropertyChanged (e) =
            base.OnPropertyChanged e
            if e.Property = ListBox.AlternationIndexProperty then
                this.InvalidateVisual ()

        override this.OnRender (drawingContext) =

            let culture = System.Threading.Thread.CurrentThread.CurrentUICulture

            let index = ListBox.GetAlternationIndex (this)
            if index <> lastIndex || formattedText = null then
                formattedText <- FormattedText (
                    index.ToString("000", culture)  ,
                    culture                         ,
                    FlowDirection.LeftToRight       ,
                    typeFace                        ,
                    24.0                            ,
                    DefaultBackgroundBrush
                    )
                lastIndex <- index

            let rs = this.RenderSize

            let rect = Rect (0.0, 0.0, this.Padding.Left, rs.Height)

            drawingContext.DrawRectangle (DefaultBorderBrush, null, rect)

            let p0 = Point (0.0, rs.Height)
            let p1 = Point (rs.Width, rs.Height)
            drawingContext.DrawLine (pen, p0, p1)

            drawingContext.PushTransform transform

            drawingContext.DrawText (formattedText, Point (0.0, 0.0))

            drawingContext.Pop ()



    type MyListBox () as this = 
        inherit ListBox ()

        do
            this.AlternationCount <- Int32.MaxValue

        override this.GetContainerForItemOverride () =
            new MyListBoxItem () :> DependencyObject


    let CreateListBox () = 
        let listBox = new MyListBox() :> ListBox
        listBox.Margin <- DefaultMargin
        listBox.SelectionMode <- SelectionMode.Extended
        listBox.MinHeight <- 24.0
        listBox.MaxHeight <- 240.0
        ScrollViewer.SetVerticalScrollBarVisibility(listBox, ScrollBarVisibility.Visible)
        ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled)
        listBox

    let CreateStackPanel orientation =
        let stackPanel = new StackPanel()
        stackPanel.Orientation <- orientation
        stackPanel

    let CreateButton t canExecute execute = 
        let button      = new Button()
        button.Content  <- t
        button.Margin   <- DefaultMargin
        button.Padding  <- DefaultButtonPadding
        let handler = ref null
        let onLoaded s e =
            button.Command  <- Command (canExecute, execute)
            button.Loaded.RemoveHandler !handler
        handler := RoutedEventHandler onLoaded
        button.Loaded.AddHandler !handler
        button

    let CreateTextBlock t = 
        let textBlock = new TextBlock()
        textBlock.Text <- t
        textBlock.Margin <- DefaultMargin
        textBlock

    let CreateTextBox t = 
        let textBox = new TextBox()
        textBox.Text <- t
        textBox.Margin <- DefaultMargin
        textBox

    let CreateLabel t = 
        let label = CreateTextBox t
        label.IsReadOnly <- true
        label.IsTabStop <- false
        label.Background <- Brushes.Transparent
        label.BorderThickness <- new Thickness (0.0)
        label.VerticalAlignment <- VerticalAlignment.Top
        label.HorizontalAlignment <- HorizontalAlignment.Left
        label

    let CreateMany canExecuteNew executeNew canExecuteDelete executeDelete : ListBox*Panel*Button*Button = 
        let buttons = CreateStackPanel Orientation.Horizontal
        let newButton = CreateButton "_New" canExecuteNew executeNew
        let deleteButton = CreateButton "_Delete" canExecuteDelete executeDelete
        ignore <| buttons.Children.Add newButton
        ignore <| buttons.Children.Add deleteButton
        let listBox = CreateListBox ()
        listBox, buttons :> Panel, newButton, deleteButton
        

    let CreateLegend t : FrameworkElement*TextBox*Decorator = 
        let label = CreateLabel t
        label.Background <- DefaultBackgroundBrush
        label.RenderTransform <- new TranslateTransform (8.0, -4.0)
        let border = new Border ()
        let outer = new Grid ()
        border.Margin <- DefaultBorderMargin
        border.Padding <- DefaultBorderPadding
        border.BorderThickness <- DefaultBorderThickness
        border.BorderBrush <- DefaultBorderBrush 
        ignore <| outer.Children.Add(border)
        ignore <| outer.Children.Add(label)
        upcast outer, label, upcast border

[<AutoOpen>]
module PublicUtils =
    let CreateElement (fe : FrameworkElement) (creator : unit -> #FrameworkElement) : #FrameworkElement = 
        match fe with
        | :? #FrameworkElement as ui -> ui
        | _                 -> creator()

    let ProcessElement (fe : FrameworkElement) (p : FrameworkElement -> unit) : FrameworkElement = 
        if fe = null then null
        else
            p fe
            fe

    let ApplyToElement defaultTo (fe : FrameworkElement) (apply : #FrameworkElement -> 'T) : 'T = 
        match fe with
        | :? #FrameworkElement as ui    -> apply ui
        | _                             -> defaultTo

    let CollectFromElement (fe : FrameworkElement) (apply : #FrameworkElement -> Collect<'T>) : Collect<'T> = 
        ApplyToElement (Fail_NeverBuiltUp ()) fe apply 

