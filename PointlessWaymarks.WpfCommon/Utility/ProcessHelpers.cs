﻿using System.Diagnostics;
using System.IO;
using System.Text;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class ProcessHelpers
{
    public static (bool success, string standardOutput, string errorOutput) ExecuteProcess(string programToExecute,
        string executionParameters, IProgress<string> progress)
    {
        if (string.IsNullOrWhiteSpace(programToExecute)) return (false, string.Empty, "Blank program to Execute?");

        var programToExecuteFile = new FileInfo(programToExecute);

        if (!programToExecuteFile.Exists)
            return (false, string.Empty, $"Program to Execute {programToExecuteFile} does not exist.");

        var standardOutput = new StringBuilder();
        var errorOutput = new StringBuilder();

        progress?.Report($"Setting up execution of {programToExecute} {executionParameters}");

        using var process = new Process
        {
            StartInfo =
            {
                FileName = programToExecute,
                Arguments = executionParameters,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            }
        };

        void OnStandardOutput(object o, DataReceivedEventArgs e)
        {
            standardOutput.AppendLine(e.Data);
            progress?.Report(e.Data);
        }

        void OnErrorOutput(object o, DataReceivedEventArgs e)
        {
            errorOutput.AppendLine(e.Data);
            progress?.Report(e.Data);
        }

        process.OutputDataReceived += OnStandardOutput;
        process.ErrorDataReceived += OnErrorOutput;

        bool result;

        try
        {
            progress?.Report("Starting Process");
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            result = process.WaitForExit(180000);
        }
        finally
        {
            process.OutputDataReceived -= OnStandardOutput;
            process.ErrorDataReceived -= OnErrorOutput;
        }

        return (result, standardOutput.ToString(), errorOutput.ToString());
    }

    public static async Task OpenExplorerWindowForFile(string fileName)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo("explorer.exe", $"/select, \"{fileName}\"")
        {
            UseShellExecute = true, Verb = "open"
        };

        Process.Start(ps);
    }

    public static void OpenUrlInExternalBrowser(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (url == "about:blank") return;

        var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
        Process.Start(ps);
    }
}