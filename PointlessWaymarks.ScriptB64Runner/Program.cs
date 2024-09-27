using System.Text;
using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis.Text;

try
{
    var directory =
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            @"Pointless Waymarks Project\PsrTemporaryFiles"));

    if (!directory.Exists) directory.Create();

    var combineArgs = string.Join("", args);

    // Decode the Base64 string to a byte array and then to a string
    var decodedBytes = Convert.FromBase64String(combineArgs);
    var decodedString = Encoding.UTF8.GetString(decodedBytes);

    var compiler = new ScriptCompiler(EmptyLogFactory, false) { AssemblyLoadContext = new ScriptAssemblyLoadContext() };
    var runner = new ScriptRunner(compiler, EmptyLogFactory, ScriptConsole.Default);
    var sourceText = SourceText.From(decodedString);
    var context = new ScriptContext(sourceText, directory.FullName, [], scriptMode: ScriptMode.Eval);
    var executeTask = runner.Execute<object>(context).GetAwaiter().GetResult();

    if (executeTask is int returnValue) return returnValue;
    if (executeTask is not null) Console.WriteLine(executeTask.ToString());

    return 0;
}
catch (Exception e)
{
    Console.WriteLine("Exception: ");
    Console.WriteLine(e);
    return 1;
}

Logger EmptyLogFactory(Type type)
{
    return (level, message, ex) =>
    {
        if (level <= LogLevel.Info) return;

        Console.WriteLine($"{level}: {message}");
        if (ex is not null) Console.WriteLine(ex.ToString());
    };
}