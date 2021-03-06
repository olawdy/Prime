﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2013-2019.

namespace Prime
open System
open Prime

/// Something with observable properties.
type Propertied = interface end

/// Handles property changes.
type PropertyChangeHandler<'w when 'w :> PropertySystem<'w>> = obj -> 'w -> 'w

/// Detaches a property change handler.
and PropertyChangeUnhandler<'w when 'w :> PropertySystem<'w>> = 'w -> 'w

/// An observable-property system.
and PropertySystem<'w when 'w :> PropertySystem<'w>> =
    abstract member GetPropertyOpt<'a> : string -> Propertied -> 'a option
    abstract member SetPropertyOpt<'a> : string -> Propertied -> 'a option -> 'w
    abstract member HandlePropertyChange : string -> Propertied -> PropertyChangeHandler<'w> -> PropertyChangeUnhandler<'w> * 'w

[<RequireQualifiedAccess>]
module PropertySystem =

    let getPropertyOpt<'w when 'w :> PropertySystem<'w>> (propertyName : string) (propertied : Propertied) (propertySystem : 'w) =
        propertySystem.GetPropertyOpt propertyName propertied

    let setPropertyOpt<'a, 'w when 'w :> PropertySystem<'w>> (propertyName : string) (propertied : Propertied) (valueOpt : 'a option) (propertySystem : 'w) =
        propertySystem.SetPropertyOpt propertyName propertied valueOpt

    let handlePropertyChange<'w when 'w :> PropertySystem<'w>> (propertyName : string) (propertied : Propertied) handler (propertySystem : 'w) =
        propertySystem.HandlePropertyChange propertyName propertied handler