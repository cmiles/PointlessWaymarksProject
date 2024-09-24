using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

try
{
    var combineArgs = string.Join("", args);

    // Decode the Base64 string to a byte array
    var decodedBytes = Convert.FromBase64String(combineArgs);

    // Convert the byte array to a regular string
    var decodedString = Encoding.UTF8.GetString(decodedBytes);

    var compiler = new ScriptCompiler(LogFactory, true);
    var runner = new ScriptRunner(compiler, LogFactory, ScriptConsole.Default);
    var sourceText = SourceText.From(decodedString);
    var context = new ScriptContext(sourceText, AppContext.BaseDirectory, [], null, OptimizationLevel.Debug,
        ScriptMode.Eval);

    var executeTask = runner.Execute<object>(context).GetAwaiter().GetResult();

    if (executeTask is int returnValue) return returnValue;
    if (executeTask is not null) Console.WriteLine(executeTask.ToString());

    return 1;
}
catch (Exception e)
{
    Console.WriteLine("Exception: ");
    Console.WriteLine(e);
    return -1;
}

Logger LogFactory(Type type)
{
    return (_, _, _) => { };
}