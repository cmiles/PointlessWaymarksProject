﻿namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     The definition of the converter used to convert an EventArgs
///     in the <see cref="EventToCommand.PassEventArgsToCommand" /> class, if the
///     <see cref="EventToCommand" /> property is true.
///     Set an instance of this class to the <see cref="EventToCommand" />
///     property of the EventToCommand instance.
/// </summary>
////[ClassInfo(typeof(EventToCommand))]
public interface IEventArgsConverter
{
    /// <summary>
    ///     The method used to convert the EventArgs instance.
    /// </summary>
    /// <param name="value">
    ///     An instance of EventArgs passed by the
    ///     event that the EventToCommand instance is handling.
    /// </param>
    /// <param name="parameter">
    ///     An optional parameter used for the conversion. Use
    ///     the <see cref="EventToCommand.EventArgsConverterParameter" /> property
    ///     to set this value. This may be null.
    /// </param>
    /// <returns>The converted value.</returns>
    object Convert(object value, object parameter);
}