using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ToastControl;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.WpfCommon.Status;

[NotifyPropertyChanged]
public partial class StatusControlContext
{
    private int _countOfRunningBlockingTasks;
    private int _countOfRunningNonBlockingTasks;

    public StatusControlContext()
    {
        ContextDispatcher = Application.Current?.Dispatcher ??
                            ThreadSwitcher.PinnedDispatcher ?? Dispatcher.CurrentDispatcher;

        Toast = new ToastSource(ContextDispatcher);
        StatusLog = [];
        CancellationList = [];

        UserMessageBoxResponseCommand = new RelayCommand<string>(UserMessageBoxResponse);
        UserStringEntryApprovedResponseCommand = new RelayCommand(UserStringEntryApprovedResponse);
        UserStringEntryCancelledResponseCommand = new RelayCommand(UserStringEntryCanceledResponse);
    }

    public bool BlockUi { get; set; }
    public ObservableCollection<UserCancellations> CancellationList { get; set; }
    public Dispatcher ContextDispatcher { get; set; }
    public CancellationTokenSource? CurrentFullScreenCancellationSource { get; set; }
    public List<StatusControlMessageButton>? MessageBoxButtonList { get; set; }
    public string? MessageBoxMessage { get; set; }
    public string MessageBoxTitle { get; set; } = string.Empty;
    public bool MessageBoxVisible { get; set; }
    public bool NonBlockingTaskAreRunning { get; set; }
    public bool ShowCancellations { get; set; }
    public string? ShowMessageResponse { get; set; }
    public Guid StatusControlContextId { get; } = Guid.NewGuid();
    public ObservableCollection<string> StatusLog { get; set; }
    public bool StringEntryApproved { get; set; }
    public string StringEntryMessage { get; set; } = string.Empty;
    public string StringEntryTitle { get; set; } = string.Empty;
    public string StringEntryUserText { get; set; } = string.Empty;
    public bool StringEntryVisible { get; set; }
    public ToastSource Toast { get; set; }
    public RelayCommand<string> UserMessageBoxResponseCommand { get; set; }
    public RelayCommand UserStringEntryApprovedResponseCommand { get; set; }
    public RelayCommand UserStringEntryCancelledResponseCommand { get; set; }


    private void BlockTaskCompleted(Task obj)
    {
        DecrementBlockingTasks();

        if (obj.IsCanceled)
        {
            ToastWarning("Canceled Task");
            return;
        }

        if (obj.IsFaulted)
        {
            ToastError($"Error: {FirstNonSeeInnerMessage(obj.Exception ?? new Exception("Unknown Error"))}");
            Task.Run(() => Log.Error(obj.Exception, "BlockTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    private void BlockTaskCompleted(Task obj, CancellationTokenSource cancellationSource)
    {
        DecrementBlockingTasks();

        var toRemove = CancellationList.Where(x => x.CancelSource == cancellationSource).ToList();

        if (toRemove.Any())
            ContextDispatcher.InvokeAsync(() =>
            {
                toRemove.ForEach(x => CancellationList.Remove(x));
                ShowCancellations = CancellationList.Any();
            });

        cancellationSource.Dispose();

        if (obj.IsCanceled)
        {
            ToastWarning("Canceled Task");
            return;
        }

        if (obj.IsFaulted)
        {
            ToastError($"Error: {FirstNonSeeInnerMessage(obj.Exception)}");
            Task.Run(() => Log.Error(obj.Exception, "BlockTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    private void DecrementBlockingTasks()
    {
        Interlocked.Decrement(ref _countOfRunningBlockingTasks);
        BlockUi = _countOfRunningBlockingTasks > 0;
    }

    private void DecrementNonBlockingTasks()
    {
        Interlocked.Decrement(ref _countOfRunningNonBlockingTasks);
        NonBlockingTaskAreRunning = _countOfRunningNonBlockingTasks > 0;
    }

    private async void FireAndForgetBlockingTaskCompleted(Task obj)
    {
        DecrementBlockingTasks();

        if (obj.IsCanceled) return;

        if (obj.IsFaulted)
        {
            await ShowMessageWithOkButton("Error", FirstNonSeeInnerMessage(obj.Exception));

#pragma warning disable 4014
            // Intended intended as Fire and Forget
            Task.Run(() => Log.Error(obj.Exception,
#pragma warning restore 4014
                "FireAndForgetBlockingTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }


    private void FireAndForgetNonBlockingTaskCompleted(Task obj)
    {
        DecrementNonBlockingTasks();

        if (obj.IsCanceled) return;

        if (obj.IsFaulted)
        {
            ToastError($"Error: {FirstNonSeeInnerMessage(obj.Exception)}");

            Task.Run(() => Log.Error(obj.Exception,
                "FireAndForgetNonBlockingTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    private void FireAndForgetWithToastOnErrorCompleted(Task obj)
    {
        if (obj.IsCanceled) return;

        if (obj.IsFaulted)
        {
            ToastError($"Error: {FirstNonSeeInnerMessage(obj.Exception)}");

            Task.Run(() => Log.Error(obj.Exception,
                "FireAndForgetNonBlockingTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    private static string FirstNonSeeInnerMessage(Exception? exception)
    {
        if (exception == null) return string.Empty;

        if (!exception.Message.Contains("inner exception", StringComparison.OrdinalIgnoreCase))
            return exception.Message;

        if (exception.InnerException == null) return exception.Message;

        var loopCounter = 0;
        var loopException = exception.InnerException;

        while (loopCounter < 20)
        {
            if (!loopException.Message.Contains("inner exception", StringComparison.OrdinalIgnoreCase))
                return loopException.Message;
            if (exception.InnerException == null) return exception.Message;

            loopException = exception.InnerException;
            loopCounter++;
        }

        return exception.Message;
    }

    private void IncrementBlockingTasks()
    {
        Interlocked.Increment(ref _countOfRunningBlockingTasks);
        BlockUi = _countOfRunningBlockingTasks > 0;
    }

    private void IncrementNonBlockingTasks()
    {
        Interlocked.Increment(ref _countOfRunningNonBlockingTasks);
        NonBlockingTaskAreRunning = _countOfRunningNonBlockingTasks > 0;
    }

    private void NonBlockTaskCompleted(Task obj)
    {
        DecrementNonBlockingTasks();
        DecrementNonBlockingTasks();

        if (obj.IsCanceled)
        {
            ToastWarning("Canceled Task");
            return;
        }

        if (obj.IsFaulted)
        {
            ToastError($"Error: {FirstNonSeeInnerMessage(obj.Exception)}");

            Task.Run(() => Log.Error(obj.Exception, "NonBlockTaskCompleted Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    public void Progress(string e)
    {
        ContextDispatcher.InvokeAsync(() =>
        {
            StatusLog.Add(e);

            if (StatusLog.Count > 100) StatusLog.Remove(StatusLog.First());

            Task.Run(() => Log.Information("Progress: {0} - Status Context Id: {1}", e, StatusControlContextId));
        });
    }

    public IProgress<string> ProgressTracker()
    {
        var toReturn = new Progress<string>();
        toReturn.ProgressChanged += ProgressTrackerChange;
        return toReturn;
    }

    private void ProgressTrackerChange(object? sender, string e)
    {
        Progress(e);
    }

    public void RunBlockingAction(Action toRun)
    {
        RunBlockingTask(() => Task.Run(toRun));
    }

    public void RunBlockingAction<T>(Action<T> toRun, T parameter)
    {
        RunBlockingTask(() => Task.Run(() => toRun(parameter)));
    }

    public RelayCommand RunBlockingActionCommand(Action toRun)
    {
        return new RelayCommand(() => RunBlockingAction(toRun));
    }

    public void RunBlockingTask(Func<Task> toRun)
    {
        IncrementBlockingTasks();
        Task.Run(toRun).ContinueWith(BlockTaskCompleted);
    }

    public void RunBlockingTask<T>(Func<T, Task> toRun, T parameter)
    {
        IncrementBlockingTasks();
        Task.Run(async () => await toRun(parameter)).ContinueWith(BlockTaskCompleted);
    }

    public RelayCommand<T> RunBlockingTaskCommand<T>(Func<T?, Task> toRun)
    {
        return new RelayCommand<T>(x => RunBlockingTask(async () => await toRun(x)));
    }

    public RelayCommand RunBlockingTaskCommand(Func<Task> toRun)
    {
        return new RelayCommand(() => RunBlockingTask(toRun));
    }

    public void RunBlockingTaskWithCancellation(Func<CancellationToken, Task> toRun, string cancelDescription)
    {
        IncrementBlockingTasks();
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        ContextDispatcher.InvokeAsync(() =>
        {
            CancellationList.Add(new UserCancellations { CancelSource = tokenSource, Description = cancelDescription });
            ShowCancellations = CancellationList.Any();
        });


        // ReSharper disable once MethodSupportsCancellation No token for final cancellation
        Task.Run(async () => await toRun(token), token).ContinueWith(x => BlockTaskCompleted(x, tokenSource));
    }

    public RelayCommand RunBlockingTaskWithCancellationCommand(Func<CancellationToken, Task> toRun,
        string cancelDescription)
    {
        return new RelayCommand(() => RunBlockingTaskWithCancellation(toRun, cancelDescription));
    }

    public void RunFireAndForgetBlockingTask(Func<Task> toRun)
    {
        try
        {
            IncrementBlockingTasks();
            Task.Run(async () => await toRun()).ContinueWith(FireAndForgetBlockingTaskCompleted);
        }
        catch (Exception e)
        {
            ShowMessageWithOkButton("Error", e.ToString()).Wait();
            DecrementBlockingTasks();

            Task.Run(() => Log.Error(e, "RunFireAndForgetBlockingTask Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    public void RunFireAndForgetNonBlockingTask(Func<Task> toRun)
    {
        try
        {
            IncrementNonBlockingTasks();
            Task.Run(async () => await toRun()).ContinueWith(FireAndForgetNonBlockingTaskCompleted);
        }
        catch (Exception e)
        {
            DecrementNonBlockingTasks();
            ToastError($"Error: {FirstNonSeeInnerMessage(e)}");

            Task.Run(() => Log.Error(e, "RunFireAndForgetNonBlockingTask Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    public void RunFireAndForgetWithToastOnError(Func<Task> toRun)
    {
        try
        {
            Task.Run(async () => await toRun()).ContinueWith(FireAndForgetWithToastOnErrorCompleted);
        }
        catch (Exception e)
        {
            DecrementNonBlockingTasks();
            ToastError($"Error: {FirstNonSeeInnerMessage(e)}");

            Task.Run(() => Log.Error(e, "RunFireAndForgetNonBlockingTask Exception - Status Context Id: {ContextId}",
                StatusControlContextId));
        }
    }

    public void RunNonBlockingAction(Action toRun)
    {
        RunNonBlockingTask(() => Task.Run(toRun));
    }

    public RelayCommand RunNonBlockingActionCommand(Action toRun)
    {
        return new RelayCommand(() => RunNonBlockingAction(toRun));
    }

    public void RunNonBlockingTask(Func<Task> toRun)
    {
        IncrementNonBlockingTasks();
        Task.Run(toRun).ContinueWith(NonBlockTaskCompleted);
    }

    public RelayCommand<T> RunNonBlockingTaskCommand<T>(Func<T?, Task> toRun)
    {
        return new RelayCommand<T>(x => RunNonBlockingTask(async () => await toRun(x)));
    }

    public RelayCommand RunNonBlockingTaskCommand(Func<Task> toRun)
    {
        return new RelayCommand(() => RunNonBlockingTask(toRun));
    }

    public async Task<string> ShowMessage(string title, string? body, List<string> buttons)
    {
        if (buttons.Any() != true) buttons = ["Ok"];

        return await ShowMessage(title, body,
            buttons.Select(x => new StatusControlMessageButton { MessageText = x }).ToList());
    }

    public async Task<string> ShowMessage(string title, string? body, List<StatusControlMessageButton>? buttons)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (buttons == null || !buttons.Any())
            buttons = [new StatusControlMessageButton { IsDefault = true, MessageText = "Ok" }];

        if (buttons.All(x => !x.IsDefault) || buttons.Count(x => x.IsDefault) > 1)
        {
            buttons.ForEach(x => x.IsDefault = false);
            buttons.First().IsDefault = true;
        }

        MessageBoxTitle = title;
        MessageBoxMessage = body;
        MessageBoxButtonList = buttons;
        MessageBoxVisible = true;

        Log.ForContext("MessageBoxTitle", title).ForContext("MessageBoxMessage", body)
            .ForContext("MessageBoxButtonList", buttons).ForContext("StatusControlContextId", StatusControlContextId)
            .Information("StatusControlContext Showing Message Box");

        await ThreadSwitcher.ResumeBackgroundAsync();

        CurrentFullScreenCancellationSource = new CancellationTokenSource();

        try
        {
            await CurrentFullScreenCancellationSource.Token.WhenCancelled();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException) Progress($"ShowMessage Exception {e.Message}");
        }
        finally
        {
            CurrentFullScreenCancellationSource.Dispose();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = ShowMessageResponse ?? string.Empty;

        MessageBoxTitle = string.Empty;
        MessageBoxMessage = string.Empty;
        MessageBoxButtonList = [];
        ShowMessageResponse = string.Empty;
        MessageBoxVisible = false;

        Log.ForContext("MessageBoxReturn", toReturn).ForContext("StatusControlContextId", StatusControlContextId)
            .Information("StatusControlContext Returning From Message Box");

        return toReturn;
    }

    public async Task<string> ShowMessageWithOkButton(string title, string? body)
    {
        return await ShowMessage(title, body,
            new List<StatusControlMessageButton> { new() { IsDefault = true, MessageText = "Ok" } });
    }

    public async Task<string> ShowMessageWithYesNoButton(string title, string? body)
    {
        return await ShowMessage(title, body,
            new List<StatusControlMessageButton>
                { new() { IsDefault = true, MessageText = "Yes" }, new() { MessageText = "No" } });
    }

    public async Task<(bool, string)> ShowStringEntry(string title, string body, string initialUserString)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StringEntryTitle = title;
        StringEntryMessage = body;
        StringEntryUserText = initialUserString;
        StringEntryVisible = true;
        StringEntryApproved = false;

        CurrentFullScreenCancellationSource = new CancellationTokenSource();

        try
        {
            await CurrentFullScreenCancellationSource.Token.WhenCancelled();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException) Progress($"ShowStringEntry Exception {e.Message}");
#pragma warning disable 4014
            // Intended intended as Fire and Forget
            Task.Run(() => Log.Error(e, "NonBlockTaskCompleted Exception - Status Context Id: {ContextId}",
#pragma warning restore 4014
                StatusControlContextId));
        }
        finally
        {
            CurrentFullScreenCancellationSource.Dispose();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = StringEntryUserText;
        var approved = StringEntryApproved;

        StringEntryTitle = string.Empty;
        StringEntryMessage = string.Empty;

        StringEntryUserText = string.Empty;

        StringEntryVisible = false;
        StringEntryApproved = false;

        return (approved, toReturn);
    }

    public void StateForceDismissFullScreenMessage()
    {
        CurrentFullScreenCancellationSource?.Cancel();
    }

    public void ToastError(string? toastText)
    {
        ContextDispatcher.InvokeAsync(() => Toast.Show(toastText, ToastType.Error));
        Task.Run(() => Log.Error("Toast Error: {0} - Status Context Id: {1}", toastText, StatusControlContextId));
    }

    public void ToastSuccess(string? toastText)
    {
        ContextDispatcher.InvokeAsync(() => Toast.Show(toastText, ToastType.Success));
        Task.Run(() =>
            Log.Information("Toast Success: {0} - Status Context Id: {1}", toastText, StatusControlContextId));
    }

    public void ToastWarning(string? toastText)
    {
        ContextDispatcher.InvokeAsync(() => Toast.Show(toastText, ToastType.Warning));
        Task.Run(() => Log.Warning("Toast Warning: {0} - Status Context Id: {1}", toastText, StatusControlContextId));
    }

    private void UserMessageBoxResponse(string? responseString)
    {
        ShowMessageResponse = responseString;
        Progress($"Show Message Response {responseString}");
        CurrentFullScreenCancellationSource?.Cancel();
    }

    private void UserStringEntryApprovedResponse()
    {
        StringEntryApproved = true;
        CurrentFullScreenCancellationSource?.Cancel();
    }

    private void UserStringEntryCanceledResponse()
    {
        StringEntryApproved = false;
        CurrentFullScreenCancellationSource?.Cancel();
    }
}