﻿// ReSharper disable CommentTypo

// ****************************************************************************
// <copyright file="EventToCommand.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>3.11.2009</date>
// <project>GalaSoft.MvvmLight.Extras</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

// ****************************************************************************
// <copyright file="IEventArgsConverter.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>18.05.2013</date>
// <project>GalaSoft.MvvmLight.Extras</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************
// ReSharper restore CommentTypo

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     This <see cref="T:System.Windows.Interactivity.TriggerAction`1" /> can be
///     used to bind any event on any FrameworkElement to an <see cref="ICommand" />.
///     Typically, this element is used in XAML to connect the attached element
///     to a command located in a ViewModel. This trigger can only be attached
///     to a FrameworkElement or a class deriving from FrameworkElement.
///     <para>
///         To access the EventArgs of the fired event, use a RelayCommand&lt;EventArgs&gt;
///         and leave the CommandParameter and CommandParameterValue empty!
///     </para>
/// </summary>
////[ClassInfo(typeof(EventToCommand),
////  VersionString = "5.2.8",
////  DateString = "201504252130",
////  Description = "A Trigger used to bind any event to an ICommand.",
////  UrlContacts = "http://www.galasoft.ch/contact_en.html",
////  Email = "laurent@galasoft.ch")]
public class EventToCommand : TriggerAction<DependencyObject>
{
    /// <summary>
    ///     The <see cref="AlwaysInvokeCommand" /> dependency property's name.
    /// </summary>
    public const string AlwaysInvokeCommandPropertyName = "AlwaysInvokeCommand";

    /// <summary>
    ///     The <see cref="EventArgsConverterParameter" /> dependency property's name.
    /// </summary>
    public const string EventArgsConverterParameterPropertyName = "EventArgsConverterParameter";

    /// <summary>
    ///     Identifies the <see cref="CommandParameter" /> dependency property
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(EventToCommand), new PropertyMetadata(null, (s, _) =>
        {
            var sender = s as EventToCommand;

            if (sender?.AssociatedObject == null) return;

            sender.EnableDisableElement();
        }));

    /// <summary>
    ///     Identifies the <see cref="Command" /> dependency property
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command),
        typeof(ICommand), typeof(EventToCommand),
        new PropertyMetadata(null, (s, e) => OnCommandChanged(s as EventToCommand, e)));

    /// <summary>
    ///     Identifies the <see cref="MustToggleIsEnabled" /> dependency property
    /// </summary>
    public static readonly DependencyProperty MustToggleIsEnabledProperty = DependencyProperty.Register(
        nameof(MustToggleIsEnabled), typeof(bool), typeof(EventToCommand), new PropertyMetadata(false, (s, _) =>
        {
            var sender = s as EventToCommand;

            if (sender?.AssociatedObject == null) return;

            sender.EnableDisableElement();
        }));

    /// <summary>
    ///     Identifies the <see cref="EventArgsConverterParameter" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register(
        EventArgsConverterParameterPropertyName, typeof(object), typeof(EventToCommand),
        new PropertyMetadata(null));

    /// <summary>
    ///     Identifies the <see cref="AlwaysInvokeCommand" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty AlwaysInvokeCommandProperty = DependencyProperty.Register(
        AlwaysInvokeCommandPropertyName, typeof(bool), typeof(EventToCommand), new PropertyMetadata(false));


    private object? _commandParameterValue;

    private bool? _mustToggleValue;

    /// <summary>
    ///     Gets or sets a value indicating if the command should be invoked even
    ///     if the attached control is disabled. This is a dependency property.
    /// </summary>
    public bool AlwaysInvokeCommand
    {
        get => (bool)GetValue(AlwaysInvokeCommandProperty);
        // ReSharper disable once UnusedMember.Global
        set => SetValue(AlwaysInvokeCommandProperty, value);
    }

    /// <summary>
    ///     Gets or sets the ICommand that this trigger is bound to. This
    ///     is a DependencyProperty.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand)GetValue(CommandProperty);

        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    ///     Gets or sets an object that will be passed to the <see cref="Command" />
    ///     attached to this trigger. This is a DependencyProperty.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);

        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    ///     Gets or sets an object that will be passed to the <see cref="Command" />
    ///     attached to this trigger. This property is here for compatibility
    ///     with the Silverlight version. This is NOT a DependencyProperty.
    ///     For data binding, use the <see cref="CommandParameter" /> property.
    /// </summary>
    public object? CommandParameterValue
    {
        get => _commandParameterValue ?? CommandParameter;

        // ReSharper disable once UnusedMember.Global
        set
        {
            _commandParameterValue = value;
            EnableDisableElement();
        }
    }

    /// <summary>
    ///     Gets or sets a converter used to convert the EventArgs when using
    ///     <see cref="PassEventArgsToCommand" />. If PassEventArgsToCommand is false,
    ///     this property is never used.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IEventArgsConverter? EventArgsConverter { get; set; }

    /// <summary>
    ///     Gets or sets a parameters for the converter used to convert the EventArgs when using
    ///     <see cref="PassEventArgsToCommand" />. If PassEventArgsToCommand is false,
    ///     this property is never used. This is a dependency property.
    /// </summary>
    public object EventArgsConverterParameter
    {
        get => GetValue(EventArgsConverterParameterProperty);
        // ReSharper disable once UnusedMember.Global
        set => SetValue(EventArgsConverterParameterProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the attached element must be
    ///     disabled when the <see cref="Command" /> property's CanExecuteChanged
    ///     event fires. If this property is true, and the command's CanExecute
    ///     method returns false, the element will be disabled. If this property
    ///     is false, the element will not be disabled when the command's
    ///     CanExecute method changes. This is a DependencyProperty.
    /// </summary>
    public bool MustToggleIsEnabled
    {
        get => (bool)GetValue(MustToggleIsEnabledProperty);

        set => SetValue(MustToggleIsEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the attached element must be
    ///     disabled when the <see cref="Command" /> property's CanExecuteChanged
    ///     event fires. If this property is true, and the command's CanExecute
    ///     method returns false, the element will be disabled. This property is here for
    ///     compatibility with the Silverlight version. This is NOT a DependencyProperty.
    ///     For data binding, use the <see cref="MustToggleIsEnabled" /> property.
    /// </summary>
    public bool MustToggleIsEnabledValue
    {
        get => _mustToggleValue ?? MustToggleIsEnabled;

        // ReSharper disable once UnusedMember.Global
        set
        {
            _mustToggleValue = value;
            EnableDisableElement();
        }
    }

    /// <summary>
    ///     Specifies whether the EventArgs of the event that triggered this
    ///     action should be passed to the bound RelayCommand. If this is true,
    ///     the command should accept arguments of the corresponding
    ///     type (for example RelayCommand&lt;MouseButtonEventArgs&gt;).
    /// </summary>
    public bool PassEventArgsToCommand { get; set; }

    private bool AssociatedElementIsDisabled()
    {
        var element = GetAssociatedObject();

        return AssociatedObject == null || element is { IsEnabled: false };
    }

    private void EnableDisableElement()
    {
        var element = GetAssociatedObject();

        if (element == null) return;

        var command = GetCommand();

        if (MustToggleIsEnabledValue && command != null)
            element.IsEnabled = command.CanExecute(CommandParameterValue);
    }


    /// <summary>
    ///     This method is here for compatibility
    ///     with the Silverlight version.
    /// </summary>
    /// <returns>
    ///     The FrameworkElement to which this trigger
    ///     is attached.
    /// </returns>
    private FrameworkElement? GetAssociatedObject()
    {
        return AssociatedObject as FrameworkElement;
    }

    /// <summary>
    ///     This method is here for compatibility
    ///     with the Silverlight 3 version.
    /// </summary>
    /// <returns>
    ///     The command that must be executed when
    ///     this trigger is invoked.
    /// </returns>
    private ICommand? GetCommand()
    {
        return Command;
    }


    /// <summary>
    ///     Provides a simple way to invoke this trigger programatically
    ///     without any EventArgs.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void Invoke()
    {
        Invoke(null);
    }

    /// <summary>
    ///     Executes the trigger.
    ///     <para>
    ///         To access the EventArgs of the fired event, use a RelayCommand&lt;EventArgs&gt;
    ///         and leave the CommandParameter and CommandParameterValue empty!
    ///     </para>
    /// </summary>
    /// <param name="parameter">The EventArgs of the fired event.</param>
    protected override void Invoke(object? parameter)
    {
        if (AssociatedElementIsDisabled() && !AlwaysInvokeCommand)
            return;

        var command = GetCommand();
        var commandParameter = CommandParameterValue;

        if (commandParameter == null && PassEventArgsToCommand)
            commandParameter = EventArgsConverter == null
                ? parameter
                : EventArgsConverter.Convert(parameter, EventArgsConverterParameter);

        if (command != null && command.CanExecute(commandParameter))
            command.Execute(commandParameter);
    }

    /// <summary>
    ///     Called when this trigger is attached to a FrameworkElement.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        EnableDisableElement();
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        EnableDisableElement();
    }

    private static void OnCommandChanged(EventToCommand? element, DependencyPropertyChangedEventArgs e)
    {
        if (element == null) return;

        if (e.OldValue != null) ((ICommand)e.OldValue).CanExecuteChanged -= element.OnCommandCanExecuteChanged;

        var command = (ICommand)e.NewValue;

        if (command != null) command.CanExecuteChanged += element.OnCommandCanExecuteChanged;

        element.EnableDisableElement();
    }
}