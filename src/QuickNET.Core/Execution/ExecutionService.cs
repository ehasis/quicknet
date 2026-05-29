using System.Reflection;
using QuickNET.Models;

namespace QuickNET.Execution;

public class ExecutionService
{
    public ExecutionResult Execute(ExecutionInput input, int timeoutSeconds = 0)
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

                object? result;
                Exception? executionException = null;

                if (timeoutSeconds > 0)
                {
                    var timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    var task = System.Threading.Tasks.Task.Run(() => method.Invoke(null, null));

                    try
                    {
                        if (!task.Wait(timeout))
                        {
                            stringWriter.Flush();
                            consoleOutput = GetConsoleOutput(sessionType, stringWriter);
                            return new ExecutionResult(
                                false, null,
                                $"Execution timed out after {timeoutSeconds} seconds.",
                                consoleOutput);
                        }
                    }
                    catch (AggregateException)
                    {
                        // Task faulted — handled below
                    }

                    if (task.IsFaulted)
                    {
                        executionException = UnwrapException(task.Exception);
                        result = null;
                    }
                    else
                        result = task.Result;
                }
                else
                {
                    try
                    {
                        result = method.Invoke(null, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        executionException = ex.InnerException ?? ex;
                        result = null;
                    }
                }

                if (executionException != null)
                {
                    return new ExecutionResult(
                        false, null,
                        $"{executionException.GetType().Name}: {executionException.Message}\n{executionException.StackTrace}",
                        consoleOutput);
                }

                if (result is System.Threading.Tasks.Task taskResult)
                {
                    taskResult.GetAwaiter().GetResult();
                    var resultProperty = taskResult.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(taskResult);
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

    private static Exception UnwrapException(Exception? exception)
    {
        if (exception is null)
            return new Exception("Unknown error");

        var unwrapped = exception;
        while (unwrapped.InnerException is not null
            && (unwrapped is TargetInvocationException
                || unwrapped is AggregateException))
        {
            unwrapped = unwrapped.InnerException;
        }

        return unwrapped;
    }
}
