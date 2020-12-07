// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq.Expressions;

/// <summary>
/// Provides extension methods for the notify property changed extensions.
/// </summary>
public static class NotifyPropertyChangedExtensions
{
    /// <summary>
    /// Notifies when the specified property changes.
    /// </summary>
    /// <param name="objectToMonitor">The object to monitor.</param>
    /// <param name="propertyExpression">The expression to the object.</param>
    /// <typeparam name="TObj">The type of initial object.</typeparam>
    /// <typeparam name="TReturn">The eventual return value.</typeparam>
    /// <returns>An observable that signals when the property specified in the expression has changed.</returns>
    /// <exception cref="ArgumentNullException">Either the property expression or the object to monitor is null.</exception>
    /// <exception cref="ArgumentException">If there is an issue with the property expression.</exception>
    public static IObservable<TReturn> WhenChanged<TObj, TReturn>(this TObj objectToMonitor, Expression<Func<TObj, TReturn>> propertyExpression)
        where TObj : INotifyPropertyChanged
    {
        throw new Exception("The impementation should have been generated.");
    }
}