﻿using System.Globalization;

namespace PointlessWaymarks.LoggingTools;

/// <summary>
///     This attribute can be used in a project file to write the Build Date into the Assembly info.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class BuildDateAttribute : Attribute
{
    public BuildDateAttribute(string value)
    {
        DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public DateTime DateTime { get; }

    //Approach from https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm

    // Sample usage in a .csproj file
    //
    //  <ItemGroup>
    //   <AssemblyAttribute Include = "PointlessWaymarks.CmsData.BuildDateAttribute" >
    //     < _Parameter1 >$([System.DateTime]::UtcNow.ToString("yyyyMMddHHmmss"))</_Parameter1>
    //   </AssemblyAttribute>
    // </ItemGroup>
}