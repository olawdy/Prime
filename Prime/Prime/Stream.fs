﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2019.

namespace Prime
open System
open System.Diagnostics
open Prime

/// A stream in the functional reactive style.
type [<ReferenceEquality>] Stream<'a, 'w when 'w :> EventSystem<'w>> =
    { Subscribe : 'w -> 'a Address * ('w -> 'w) * 'w }

// TODO: document track functions.
[<RequireQualifiedAccess>]
module Stream =

    /// The empty stream.
    let [<DebuggerHidden; DebuggerStepThrough>] makeEmpty<'a, 'w when 'w :> EventSystem<'w>> :
        Stream<'a, 'w> =
        { Subscribe = fun world -> (Address.empty, id, world) }

    /// Make a stream of an event at the given address.
    let [<DebuggerHidden; DebuggerStepThrough>] make<'a, 'w when 'w :> EventSystem<'w>>
        (eventAddress : 'a Address) : Stream<'a, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'a> (scstring subscriptionKey)
            let unsubscribe = fun world -> EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let eventTrace = EventTrace.record "Stream" "stream" evt.Trace
                let world = EventSystem.publishPlus<'a, Participant, 'w> EventSystem.sortSubscriptionsNone evt.Data subscriptionAddress eventTrace globalParticipant false world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    let [<DebuggerHidden; DebuggerStepThrough>] generalize<'a, 'w when 'w :> EventSystem<'w>>
        (stream : Stream<'a, 'w>) : Stream<obj, 'w> =
        { Subscribe = fun world -> let (address, unsub, world) = stream.Subscribe world in (atooa address, unsub, world) }

    (* Side-Effecting Combinators *)

    let [<DebuggerHidden; DebuggerStepThrough>] trackEffect4
        (tracker : 'c -> Event<'a, Participant> -> 'w -> 'c * bool * 'w)
        (transformer : 'c -> 'b)
        (state : 'c)
        (stream : Stream<'a, 'w>) :
        Stream<'b, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let stateKey = makeGuid ()
            let world = EventSystem.addEventState stateKey state world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'b> (scstring subscriptionKey)
            let (eventAddress, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = EventSystem.removeEventState stateKey world
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let state = EventSystem.getEventState stateKey world
                let (state, tracked, world) = tracker state evt world
                let world = EventSystem.addEventState stateKey state world
                let world =
                    if tracked then
                        let eventData = transformer state
                        let eventTrace = EventTrace.record "Stream" "trackEvent4" evt.Trace
                        EventSystem.publishPlus<'b, Participant, 'w> EventSystem.sortSubscriptionsNone eventData subscriptionAddress eventTrace globalParticipant false world
                    else world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    let [<DebuggerHidden; DebuggerStepThrough>] trackEffect2
        (tracker : 'a -> Event<'a, Participant> -> 'w -> 'a * bool * 'w)
        (stream : Stream<'a, 'w>) :
        Stream<'a, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let stateKey = makeGuid ()
            let world = EventSystem.addEventState stateKey None world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'a> (scstring subscriptionKey)
            let (eventAddress, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = EventSystem.removeEventState stateKey world
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let stateOpt = EventSystem.getEventState stateKey world
                let state = match stateOpt with Some state -> state | None -> evt.Data
                let (state, tracked, world) = tracker state evt world
                let world = EventSystem.addEventState stateKey state world
                let world =
                    if tracked then
                        let eventTrace = EventTrace.record "Stream" "trackEvent2" evt.Trace
                        EventSystem.publishPlus<'a, Participant, 'w> EventSystem.sortSubscriptionsNone state subscriptionAddress eventTrace globalParticipant false world
                    else world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    let [<DebuggerHidden; DebuggerStepThrough>] trackEffect
        (tracker : 'b -> 'w -> 'b * bool * 'w) (state : 'b) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let stateKey = makeGuid ()
            let world = EventSystem.addEventState stateKey state world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'a> (scstring subscriptionKey)
            let (eventAddress, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = EventSystem.removeEventState stateKey world
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let state = EventSystem.getEventState stateKey world
                let (state, tracked, world) = tracker state world
                let world = EventSystem.addEventState stateKey state world
                let world =
                    if tracked then
                        let eventTrace = EventTrace.record "Stream" "trackEvent" evt.Trace
                        EventSystem.publishPlus<'a, Participant, 'w> EventSystem.sortSubscriptionsNone evt.Data subscriptionAddress eventTrace globalParticipant false world
                    else world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    /// Fold over a stream, then map the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldMapEffect (f : 'b -> Event<'a, Participant> -> 'w -> 'b * 'w) g s (stream : Stream<'a, 'w>) : Stream<'c, 'w> =
        trackEffect4 (fun b a w -> (Triple.insert true (f b a w))) g s stream

    /// Fold over a stream, aggegating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldEffect (f : 'b -> Event<'a, Participant> -> 'w -> 'b * 'w) s (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        trackEffect4 (fun b a w -> (Triple.insert true (f b a w))) id s stream

    /// Reduce over a stream, accumulating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] reduceEffect (f : 'a -> Event<'a, Participant> -> 'w -> 'a * 'w) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackEffect2 (fun a a2 w -> (Triple.insert true (f a a2 w))) stream

    /// Filter a stream by the given 'pred' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] filterEffect
        (pred : Event<'a, Participant> -> 'w -> bool * 'w) (stream : Stream<'a, 'w>) =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'a> (scstring subscriptionKey)
            let (eventAddress, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let (passed, world) = pred evt world
                let world =
                    if passed then
                        let eventTrace = EventTrace.record "Stream" "filterEvent" evt.Trace
                        EventSystem.publishPlus<'a, Participant, 'w> EventSystem.sortSubscriptionsNone evt.Data subscriptionAddress eventTrace globalParticipant false world
                    else world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    /// Map over a stream by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] mapEffect
        (mapper : Event<'a, Participant> -> 'w -> 'b * 'w) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'b> (scstring subscriptionKey)
            let (eventAddress, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun evt world ->
                let (eventData, world) = mapper evt world
                let eventTrace = EventTrace.record "Stream" "mapEvent" evt.Trace
                let world = EventSystem.publishPlus<'b, Participant, 'w> EventSystem.sortSubscriptionsNone eventData subscriptionAddress eventTrace globalParticipant false world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription eventAddress globalParticipant world |> snd
            (subscriptionAddress, unsubscribe, world)
        { Subscribe = subscribe }

    /// Map over two streams.
    let [<DebuggerHidden; DebuggerStepThrough>] map2Effect
        (mapper : Event<'a, Participant> -> Event<'b, Participant> -> 'w -> 'c * 'w)
        (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<'c, 'w> =
        let subscribe = fun (world : 'w) ->

            // initialize event state, subscription keys and addresses
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let stateKey = makeGuid ()
            let state = (List.empty<Event<'a, Participant>>, List.empty<Event<'b, Participant>>)
            let world = EventSystem.addEventState stateKey state world
            let subscriptionKey = makeGuid ()
            let subscriptionKey' = makeGuid ()
            let subscriptionKey'' = makeGuid ()
            let (subscriptionAddress, unsubscribe, world) = stream.Subscribe world
            let (subscriptionAddress', unsubscribe', world) = stream2.Subscribe world
            let subscriptionAddress'' = ntoa<'c> (scstring subscriptionKey'')

            // unsubscribe from 'a and 'b events, and remove event state
            let unsubscribe = fun world ->
                let world = unsubscribe (unsubscribe' world)
                let world = EventSystem.unsubscribe<'w> subscriptionKey world
                let world = EventSystem.unsubscribe<'w> subscriptionKey' world
                EventSystem.removeEventState stateKey world

            // subscription for 'a events
            let subscription = fun evt world ->
                let eventTrace = EventTrace.record4 "Stream" "product" "'a" evt.Trace
                let (aList : Event<'a, Participant> list, bList : Event<'b, Participant> list) = EventSystem.getEventState stateKey world
                let aList = evt :: aList
                let (state, world) =
                    match (List.rev aList, List.rev bList) with
                    | (a :: aList, b :: bList) ->
                        let state = (aList, bList)
                        let (eventData, world) = mapper a b world
                        let world = EventSystem.publishPlus<'c, Participant, _> EventSystem.sortSubscriptionsNone eventData subscriptionAddress'' eventTrace globalParticipant false world
                        (state, world)
                    | state -> (state, world)
                let world = EventSystem.addEventState stateKey state world
                (Cascade, world)

            // subscription for 'b events
            let subscription' = fun evt world ->
                let eventTrace = EventTrace.record4 "Stream" "product" "'b" evt.Trace
                let (aList : Event<'a, Participant> list, bList : Event<'b, Participant> list) = EventSystem.getEventState stateKey world
                let bList = evt :: bList
                let (state, world) =
                    match (List.rev aList, List.rev bList) with
                    | (a :: aList, b :: bList) ->
                        let state = (aList, bList)
                        let (eventData, world) = mapper a b world
                        let world = EventSystem.publishPlus<'c, Participant, _> EventSystem.sortSubscriptionsNone eventData subscriptionAddress'' eventTrace globalParticipant false world
                        (state, world)
                    | state -> (state, world)
                let world = EventSystem.addEventState stateKey state world
                (Cascade, world)

            // subscripe 'a and 'b events
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription subscriptionAddress globalParticipant world |> snd
            let world = EventSystem.subscribePlus<'b, Participant, 'w> subscriptionKey subscription' subscriptionAddress' globalParticipant world |> snd
            (subscriptionAddress'', unsubscribe, world)

        // fin
        { Subscribe = subscribe }

    (* Event-Accessing Combinators *)

    let [<DebuggerHidden; DebuggerStepThrough>] trackEvent4
        (tracker : 'c -> Event<'a, Participant> -> 'w -> 'c * bool)
        (transformer : 'c -> 'b)
        (state : 'c)
        (stream : Stream<'a, 'w>) :
        Stream<'b, 'w> =
        trackEffect4 (fun state evt world -> Triple.append world (tracker state evt world)) transformer state stream

    let [<DebuggerHidden; DebuggerStepThrough>] trackEvent2
        (tracker : 'a -> Event<'a, Participant> -> 'w -> 'a * bool)
        (stream : Stream<'a, 'w>) :
        Stream<'a, 'w> =
        trackEffect2 (fun state evt world -> Triple.append world (tracker state evt world)) stream

    let [<DebuggerHidden; DebuggerStepThrough>] trackEvent
        (tracker : 'b -> 'w -> 'b * bool) (state : 'b) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackEffect (fun state world -> Triple.append world (tracker state world)) state stream

    /// Fold over a stream, then map the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldMapEvent (f : 'b -> Event<'a, Participant> -> 'w -> 'b) g s (stream : Stream<'a, 'w>) : Stream<'c, 'w> =
        foldMapEffect (fun state evt world -> (f state evt world, world)) g s stream

    /// Fold over a stream, aggegating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldEvent (f : 'b -> Event<'a, Participant> -> 'w -> 'b) s (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        foldEffect (fun state evt world -> (f state evt world, world)) s stream

    /// Reduce over a stream, accumulating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] reduceEvent (f : 'a -> Event<'a, Participant> -> 'w -> 'a) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        reduceEffect (fun value evt world -> (f value evt world, world)) stream

    /// Filter a stream by the given 'pred' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] filterEvent
        (pred : Event<'a, Participant> -> 'w -> bool) (stream : Stream<'a, 'w>) =
        filterEffect (fun evt world -> (pred evt world, world)) stream

    /// Map over a stream by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] mapEvent
        (mapper : Event<'a, Participant> -> 'w -> 'b) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        mapEffect (fun evt world -> (mapper evt world, world)) stream

    /// Map over two streams by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] map2Event
        (mapper : Event<'a, Participant> -> Event<'b, Participant> -> 'w -> 'c) (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<'c, 'w> =
        map2Effect (fun evtA evtB world -> (mapper evtA evtB world, world)) stream stream2

    (* World-Accessing Combinators *)

    let [<DebuggerHidden; DebuggerStepThrough>] trackWorld4
        (tracker : 'c -> 'a -> 'w -> 'c * bool) (transformer : 'c -> 'b) (state : 'c) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        trackEvent4 (fun c evt world -> tracker c evt.Data world) transformer state stream

    let [<DebuggerHidden; DebuggerStepThrough>] trackWorld2
        (tracker : 'a -> 'a -> 'w -> 'a * bool) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackEvent2 (fun a evt world -> tracker a evt.Data world) stream

    let [<DebuggerHidden; DebuggerStepThrough>] trackWorld
        (tracker : 'b -> 'w -> 'b * bool) (state : 'b) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackEvent tracker state stream

    /// Fold over a stream, then map the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldMapWorld (f : 'b -> 'a -> 'w -> 'b) g s (stream : Stream<'a, 'w>) : Stream<'c, 'w> =
        foldMapEvent (fun b evt world -> f b evt.Data world) g s stream

    /// Fold over a stream, aggegating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldWorld (f : 'b -> 'a -> 'w -> 'b) s (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        foldEvent (fun b evt world -> f b evt.Data world) s stream

    /// Reduce over a stream, accumulating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] reduceWorld (f : 'a -> 'a -> 'w -> 'a) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        reduceEvent (fun a evt world -> f a evt.Data world) stream

    /// Filter a stream by the given 'pred' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] filterWorld (pred : 'a -> 'w -> bool) (stream : Stream<'a, 'w>) =
        filterEvent (fun evt world -> pred evt.Data world) stream

    /// Map over a stream by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] mapWorld (mapper : 'a -> 'w -> 'b) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        mapEvent (fun evt world -> mapper evt.Data world) stream

    /// Map over two streams by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] map2World
        (mapper : 'a -> 'b -> 'w -> 'c) (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<'c, 'w> =
        map2Event (fun evtA evtB world -> mapper evtA.Data evtB.Data world) stream stream2

    (* Primitive Combinators *)

    let [<DebuggerHidden; DebuggerStepThrough>] track4
        (tracker : 'c -> 'a -> 'c * bool) (transformer : 'c -> 'b) (state : 'c) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        trackWorld4 (fun c a _ -> tracker c a) transformer state stream

    let [<DebuggerHidden; DebuggerStepThrough>] track2
        (tracker : 'a -> 'a -> 'a * bool) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackWorld2 (fun a a2 _ -> tracker a a2) stream

    let [<DebuggerHidden; DebuggerStepThrough>] track
        (tracker : 'b -> 'b * bool) (state : 'b) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        trackWorld (fun b _ -> tracker b) state stream

    /// Fold over a stream, then map the result.
    let [<DebuggerHidden; DebuggerStepThrough>] foldMap (f : 'b -> 'a -> 'b) g s (stream : Stream<'a, 'w>) : Stream<'c, 'w> =
        foldMapWorld (fun b a _ -> f b a) g s stream

    /// Fold over a stream, aggegating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] fold (f : 'b -> 'a -> 'b) s (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        foldWorld (fun b a _ -> f b a) s stream

    /// Reduce over a stream, accumulating the result.
    let [<DebuggerHidden; DebuggerStepThrough>] reduce (f : 'a -> 'a -> 'a) (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        reduceWorld (fun a a2 _ -> f a a2) stream

    /// Filter a stream by the given 'pred' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] filter (pred : 'a -> bool) (stream : Stream<'a, 'w>) =
        filterWorld (fun a _ -> pred a) stream

    /// Map over a stream by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] map (mapper : 'a -> 'b) (stream : Stream<'a, 'w>) : Stream<'b, 'w> =
        mapWorld (fun a _ -> mapper a) stream

    /// Map over two streams by the given 'mapper' procedure.
    let [<DebuggerHidden; DebuggerStepThrough>] map2
        (mapper : 'a -> 'b -> 'c) (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<'c, 'w> =
        map2World (fun a b _ -> mapper a b) stream stream2

    /// Combine two streams. Combination is in 'product form', which is defined as a pair of the data of the combined
    /// events. Think of it as 'zip' for event streams.
    let [<DebuggerHidden; DebuggerStepThrough>] product
        (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<'a * 'b, 'w> =
        map2 (fun a b -> (a, b)) stream stream2

    /// Combine two streams. Combination is in 'sum form', which is defined as an Either of the data of the combined
    /// events, where only data from the most recent event is available at a time.
    let [<DebuggerHidden; DebuggerStepThrough>] sum
        (stream : Stream<'a, 'w>) (stream2 : Stream<'b, 'w>) : Stream<Either<'a, 'b>, 'w> =
        let subscribe = fun world ->
            let subscriptionKey = makeGuid ()
            let subscriptionKey' = makeGuid ()
            let subscriptionKey'' = makeGuid ()
            let (subscriptionAddress, unsubscribe, world) = stream.Subscribe world
            let (subscriptionAddress', unsubscribe', world) = stream2.Subscribe world
            let subscriptionAddress'' = ntoa<Either<'a, 'b>> (scstring subscriptionKey'')
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let unsubscribe = fun world ->
                let world = unsubscribe (unsubscribe' world)
                let world = EventSystem.unsubscribe<'w> subscriptionKey world
                EventSystem.unsubscribe<'w> subscriptionKey' world
            let subscription = fun evt world ->
                let eventData = Left evt.Data
                let eventTrace = EventTrace.record "Stream" "sum" evt.Trace
                let world = EventSystem.publishPlus<Either<'a, 'b>, Participant, _> EventSystem.sortSubscriptionsNone eventData subscriptionAddress'' eventTrace globalParticipant false world
                (Cascade, world)
            let subscription' = fun evt world ->
                let eventData = Right evt.Data
                let eventTrace = EventTrace.record "Stream" "sum" evt.Trace
                let world = EventSystem.publishPlus<Either<'a, 'b>, Participant, _> EventSystem.sortSubscriptionsNone eventData subscriptionAddress'' eventTrace globalParticipant false world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'b, Participant, 'w> subscriptionKey' subscription' subscriptionAddress' globalParticipant world |> snd
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey subscription subscriptionAddress globalParticipant world |> snd
            (subscriptionAddress'', unsubscribe, world)
        { Subscribe = subscribe }

    /// Terminate a stream when a given stream receives a value.
    let [<DebuggerHidden; DebuggerStepThrough>] until
        (stream : Stream<'b, 'w>) (stream2 : Stream<'a, 'w>) : Stream<'a, 'w> =
        let subscribe = fun (world : 'w) ->
            let globalParticipant = EventSystem.getGlobalParticipantGeneralized world
            let subscriptionKey = makeGuid ()
            let subscriptionKey' = makeGuid ()
            let subscriptionKey'' = makeGuid ()
            let (subscriptionAddress, unsubscribe, world) = stream.Subscribe world
            let (subscriptionAddress', unsubscribe', world) = stream2.Subscribe world
            let subscriptionAddress'' = ntoa<'a> (scstring subscriptionKey'')
            let unsubscribe = fun world ->
                let world = unsubscribe (unsubscribe' world)
                let world = EventSystem.unsubscribe<'w> subscriptionKey' world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let subscription = fun _ world ->
                let world = unsubscribe world
                (Cascade, world)
            let subscription' = fun evt world ->
                let eventTrace = EventTrace.record "Stream" "until" evt.Trace
                let world = EventSystem.publishPlus<'a, Participant, 'w> EventSystem.sortSubscriptionsNone evt.Data subscriptionAddress'' eventTrace globalParticipant false world
                (Cascade, world)
            let world = EventSystem.subscribePlus<'a, Participant, 'w> subscriptionKey' subscription' subscriptionAddress' globalParticipant world |> snd
            let world = EventSystem.subscribePlus<'b, Participant, 'w> subscriptionKey subscription subscriptionAddress globalParticipant world |> snd
            (subscriptionAddress'', unsubscribe, world)
        { Subscribe = subscribe }

    /// Terminate a stream when the subscriber is unregistered from the world.
    let [<DebuggerHidden; DebuggerStepThrough>] lifetime<'s, 'a, 'w when 's :> Participant and 'w :> EventSystem<'w>>
        (subscriber : 's) (stream_ : Stream<'a, 'w>) : Stream<'a, 'w> =
        let unregisteringEventAddress = rtoa<unit> [|"Unregistering"; "Event"|] --> subscriber.ParticipantAddress
        let removingStream = make unregisteringEventAddress
        until removingStream stream_

    /// Subscribe to a stream, handling each event with the given subscription,
    /// returning both an unsubscription procedure as well as the world as augmented with said
    /// subscription.
    let [<DebuggerHidden; DebuggerStepThrough>] subscribeEffect subscription (subscriber : 's) stream world =
        let subscribe = fun world ->
            let subscriptionKey = makeGuid ()
            let subscriptionAddress = ntoa<'a> (scstring subscriptionKey)
            let (address, unsubscribe, world) = stream.Subscribe world
            let unsubscribe = fun world ->
                let world = unsubscribe world
                EventSystem.unsubscribe<'w> subscriptionKey world
            let world = EventSystem.subscribePlus<'a, 's, 'w> subscriptionKey subscription address subscriber world |> snd
            (subscriptionAddress, unsubscribe, world)
        let stream = { Subscribe = subscribe }
        stream.Subscribe world |> _bc

    /// Subscribe to a stream, handling each event with the given subscription.
    let [<DebuggerHidden; DebuggerStepThrough>] subscribe subscription subscriber stream world =
        subscribeEffect (fun evt world -> (Cascade, subscription evt world)) subscriber stream world |> snd

    /// Subscribe to a stream until the subscriber is removed from the world,
    /// returning both an unsubscription procedure as well as the world as augmented with said
    /// subscription.
    let [<DebuggerHidden; DebuggerStepThrough>] monitorEffect subscription subscriber stream world =
        (stream |> lifetime subscriber |> subscribeEffect subscription subscriber) world

    /// Subscribe to a stream until the subscriber is removed from the world.
    let [<DebuggerHidden; DebuggerStepThrough>] monitor subscription subscriber stream world =
        monitorEffect (fun evt world -> (Cascade, subscription evt world)) subscriber stream world |> snd

    /// Insert a persistent state value into the stream.
    let [<DebuggerHidden; DebuggerStepThrough>] insert state stream =
        stream |>
        fold (fun (stateOpt, _) a -> (Some (Option.getOrDefault state stateOpt), a)) (None, Unchecked.defaultof<_>) |>
        map (mapFst Option.get)

    (* Derived Combinators *)

    /// Append a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] inline append streamL streamR =
        map Either.amb (sum streamL streamR)

    /// Filter the left values out from the stream.
    let [<DebuggerHidden; DebuggerStepThrough>] inline filterLeft stream =
        filter Either.isLeft stream |> map Either.getLeftValue

    /// Filter the right values out from the stream.
    let [<DebuggerHidden; DebuggerStepThrough>] inline filterRight stream =
        filter Either.isRight stream |> map Either.getRightValue

    /// Transform a stream into a running average of its event's numeric data.
    let [<DebuggerHidden; DebuggerStepThrough>] inline average (stream : Stream<'a, 'w>) : Stream<'a, 'w> =
        foldMap
            (fun (avg : 'a, den : 'a) a ->
                let den' = den + one ()
                let dod' = den / den'
                let avg' = avg * dod' + a / den
                (avg', den'))
            fst
            (zero (), zero ())
            stream

    /// Transform a stream into a running map from its event's data to keys as defined by 'f'.
    let [<DebuggerHidden; DebuggerStepThrough>] organize f (stream : Stream<'a, 'w>) : Stream<('a * 'b) option * Map<'b, 'a>, 'w> =
        fold
            (fun (_, m) a ->
                let b = f a
                if Map.containsKey b m
                then (None, m)
                else (Some (a, b), Map.add b a m))
            (None, Map.empty)
            stream

    /// Transform a stream into a running set of its event's unique data as defined via 'by'.
    let [<DebuggerHidden; DebuggerStepThrough>] groupBy by (stream : Stream<'a, 'w>) : Stream<'b * bool * 'b Set, 'w> =
        fold
            (fun (_, _, set) a ->
                let b = by a
                if Set.contains b set
                then (b, false, set)
                else (b, true, Set.add b set))
            (Unchecked.defaultof<'b>, false, Set.empty)
            stream

    /// Transform a stream into a running set of its event's unique data.
    let [<DebuggerHidden; DebuggerStepThrough>] group (stream : Stream<'a, 'w>) : Stream<'a * bool * 'a Set, 'w> =
        groupBy id stream

    /// Filter a stream of options for actual values.
    let [<DebuggerHidden; DebuggerStepThrough>] definitize (stream : Stream<'a option, 'w>) =
        stream |>
        filter Option.isSome |>
        map Option.get

    /// Filter events with unchanging data.
    let [<DebuggerHidden; DebuggerStepThrough>] optimize (stream : Stream<_, 'w>) =
        fold
            (fun (_, l) a ->
                match l with
                | [] -> (Some a, [a])
                | x :: _ -> if a = x then (None, [a]) else (Some a, [a]))
            (None, [])
            stream |>
        map fst |>
        definitize

    /// Transform a stream into a running sum of its data.
    let [<DebuggerHidden; DebuggerStepThrough>] inline sumN stream = reduce (+) stream

    /// Transform a stream into a running product of its data.
    let [<DebuggerHidden; DebuggerStepThrough>] inline productN stream = reduce (*) stream

    /// Transform a stream of pairs into its fst values.
    let [<DebuggerHidden; DebuggerStepThrough>] first stream = map fst stream

    /// Transform a stream of pairs into its snd values.
    let [<DebuggerHidden; DebuggerStepThrough>] second stream = map snd stream

    /// Transform a stream's pairs by a mapping of its fst values.
    let [<DebuggerHidden; DebuggerStepThrough>] mapFirst mapper stream = map (fun a -> (mapper (fst a), snd a)) stream

    /// Transform a stream of pairs by a mapping of its snd values.
    let [<DebuggerHidden; DebuggerStepThrough>] mapSecond mapper stream = map (fun a -> (fst a, mapper (snd a))) stream

    /// Transform a stream by duplicating its data into pairs.
    let [<DebuggerHidden; DebuggerStepThrough>] duplicate stream = map (fun a -> (a, a)) stream

    /// Take only the first n events from a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] take n stream = track (fun m -> (m + 1, m < n)) 0 stream

    /// Skip the first n events in a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] skip n stream = track (fun m -> (m + 1, m >= n)) 0 stream

    /// Take only the first event from a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] head stream = take 1 stream

    /// Skip the first event of a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] tail stream = skip 1 stream

    /// Take only the nth event from a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] nth n stream = stream |> skip n |> head

    /// Take only the first event from a stream that satisfies 'p'.
    let [<DebuggerHidden; DebuggerStepThrough>] search p stream = stream |> filter p |> head

    /// Filter out the None data values from a stream and strip the Some constructor from the remaining values.
    let [<DebuggerHidden; DebuggerStepThrough>] choose (stream : Stream<'a option, 'w>) = stream |> filter Option.isSome |> map Option.get

    /// Transform a stream into a running maximum of it numeric data.
    let [<DebuggerHidden; DebuggerStepThrough>] max stream = reduce (fun n a -> if n < a then a else n) stream
    
    /// Transform a stream into a running minimum of it numeric data.
    let [<DebuggerHidden; DebuggerStepThrough>] min stream = reduce (fun n a -> if a < n then a else n) stream

    /// Filter out the events with non-unique data as defined via 'by' from a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] distinctBy by stream = stream |> organize by |> first |> choose

    /// Filter out the events with non-unique data from a stream.
    let [<DebuggerHidden; DebuggerStepThrough>] distinct stream = distinctBy id stream

    /// Identity for streams.
    let [<DebuggerHidden; DebuggerStepThrough>] id (stream : Stream<_, _>) = stream        

[<AutoOpen>]
module StreamOperators =

    /// Stream sequencing operator.
    let (---) = (|>)

    /// Make a stream of the subscriber's change events.
    let [<DebuggerHidden; DebuggerStepThrough>] (!--) (lens : Lens<'b, 'w>) =
        let changeEventAddress = rtoa<ChangeData> [|"Change"; lens.Name; "Event"|] --> lens.This.ParticipantAddress
        Stream.make changeEventAddress --- Stream.mapEvent (fun _ world -> lens.Get world)

    /// Propagate the event data of a stream to a property in the observing participant when the
    /// subscriber exists (doing nothing otherwise).
    let [<DebuggerHidden; DebuggerStepThrough>] (-|>) stream (lens : Lens<'b, 'w>) =
        Stream.subscribe (fun a world ->
            if world.ParticipantExists a.Subscriber then
                match lens.SetOpt with
                | Some set -> set a.Data world
                | None -> world // TODO: log info here about property not being set-able?
            else world)
            lens.This
            stream