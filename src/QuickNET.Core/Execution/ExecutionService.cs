using System.Reflection;
using QuickNET.Models;

namespace QuickNET.Execution;

public class ExecutionService
{
    public ExecutionResult Execute(ExecutionInput input)
    {
        var alc = new QuickNETAssemblyLoadContext();
        string consoleOutput = "";

        try
        {
            var assembly = alc.LoadFromBytes(input.AssemblyBytes);
            var sessionType = assembly.GetType("QuickNETSession");

            if (sessionType is null)
            {
                return new ExecutionResult(false, null, "Type 'QuickNETSession' not found in assembly.", null);
            }

            var method = sessionType.GetMethod("Execute",
                BindingFlags.Public | BindingFlags.Static);

            if (method is null)
            {
                return new ExecutionResult(false, null, "Method 'Execute' not found.", null);
            }

            var originalOut = Console.Out;
            var stringWriter = new StringWriter();

            try
            {
                Console.SetOut(stringWriter);
                var result = method.Invoke(null, null);

                if (result is System.Threading.Tasks.Task task)
                {
                    task.GetAwaiter().GetResult();
                    var resultProperty = task.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }

                stringWriter.Flush();
                consoleOutput = GetConsoleOutput(sessionType, stringWriter);

                return new ExecutionResult(
                    true,
                    result?.ToString() ?? "null",
                    null,
                    consoleOutput);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException ?? ex;

            try
            {
                var assembly = alc.LoadFromBytes(input.AssemblyBytes);
                var sessionType = assembly.GetType("QuickNETSession");
                if (sessionType is not null)
                {
                    consoleOutput = GetConsoleOutput(sessionType, null);
                }
            }
            catch
            {
            }

            return new ExecutionResult(
                false,
                null,
                $"{inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}",
                consoleOutput);
        }
        finally
        {
            alc.Unload();

            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    private static string GetConsoleOutput(Type sessionType, StringWriter? hostWriter)
    {
        var field = sessionType.GetField("__ConsoleOutput",
            BindingFlags.Public | BindingFlags.Static);

        if (field is not null)
        {
            var value = field.GetValue(null)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return hostWriter?.ToString() ?? "";
    }
}
