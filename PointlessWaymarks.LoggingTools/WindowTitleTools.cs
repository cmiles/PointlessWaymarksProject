﻿using System.Reflection;

namespace PointlessWaymarks.LoggingTools
{
    public static class WindowTitleTools
    {
        public static DateTime? GetBuildDate(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime;
        }

        public static DateTime? GetEntryAssemblyBuildDate(Assembly assembly)
        {
            try
            {
                var entryAssembly = Assembly.GetExecutingAssembly();
                if (entryAssembly == null) return null;
                var attribute = entryAssembly.GetCustomAttribute<BuildDateAttribute>();
                return attribute?.DateTime;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public static string StandardAppInformationString(Assembly assembly, string programDisplayName)
        {


            return $"{programDisplayName} - Built On {(GetEntryAssemblyBuildDate(assembly)?.ToString("g") ?? "Unknown")} - Commit {ThisAssembly.Git.Commit} {(ThisAssembly.Git.IsDirty ? "(Has Local Changes)" : string.Empty)}";
        }
    }
}