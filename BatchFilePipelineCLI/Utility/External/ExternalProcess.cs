using BatchFilePipelineCLI.Logging;
using System.Diagnostics;
using System.Text;

namespace BatchFilePipelineCLI.Utility.External
{
    /// <summary>
    /// Delegate processing to external applications
    /// </summary>
    public static class ExternalProcess
    {
        /// <summary>
        /// Run an external process and handle the return result
        /// </summary>
        /// <param name="exe">The application that is to be run</param>
        /// <param name="args">The arguments that will be supplied to the application being run</param>
        /// <param name="onOutput">[Optional] Callback that will be raised with the output from the external operation</param>
        /// <param name="onError">[Optional] Callback that will be raised with the error output from the external operation</param>
        /// <param name="cancellationToken">[Optional] Cancellation token that can be used to manage the lifespan of the operation</param>
        /// <returns>Returns the result of the external operation that was performed</returns>
        public static async Task<ProcessResult> RunAsync(string exe,
                                                         string args,
                                                         Action<string>? onOutput = null,
                                                         Action<string>? onError = null,
                                                         CancellationToken cancellationToken = default)
        {
            // Setup the process start information
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Initialise the containers that will be handling the output
            var proc = new Process { StartInfo = psi };
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            // Create the callbacks that will respond to standard output events
            proc.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data) == true)
                {
                    return;
                }
                stdOut.AppendLine(e.Data);
                try
                {
                    onOutput?.Invoke(e.Data);
                } catch (Exception ex)
                {
                    Logger.Exception($"[{exe}] Exception on stdout callback", ex);
                }
            };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data) == true)
                {
                    return;
                }
                stdErr.AppendLine(e.Data);
                try
                {
                    onError?.Invoke(e.Data);
                } catch (Exception ex)
                {
                    Logger.Exception($"[{exe}] Exception on stdout callback", ex);
                }
            };

            // Try to start processing the external request
            try
            {
                Logger.Log($"[{nameof(ExternalProcess)}] Running external process: {exe} {args}");
                if (proc.Start() == false)
                {
                    return new ProcessResult(-1, string.Empty, $"Failed to start process '{exe}'");
                }

                // Allow the output to be processed
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }

            // If anything goes wrong, that's a failure
            catch (Exception ex)
            {
                return new ProcessResult(-1, string.Empty, $"Exception starting process '{exe}': {ex}");
            }

            // Wait for the task to be completed
            var waitTask = proc.WaitForExitAsync(cancellationToken);
            try
            {
                await waitTask.ConfigureAwait(false);
            }

            // Check if the operation was cancelled
            catch (OperationCanceledException)
            {
                // Attempt to gracefully kill the process if cancellation is required
                if (proc.HasExited == false)
                {
                    try { proc.CloseMainWindow(); } catch { }

                    // brief grace period
                    await Task.Delay(500).ConfigureAwait(false);
                    if (!proc.HasExited)
                    {
                        try { proc.Kill(true); }
                        catch (Exception ex) { Logger.Exception($"{nameof(External)}.{nameof(RunAsync)}: Kill failed", ex); }
                    }

                    // Ensure exit
                    try { await proc.WaitForExitAsync().ConfigureAwait(false); } catch { }
                }

                // We have our final result
                return new ProcessResult
                (
                    -1,
                    stdOut.ToString(),
                    stdErr.ToString(),
                    true
                );
            }

            // Return whatever result was stored in the process
            return new ProcessResult
            (
                proc.ExitCode,
                stdOut.ToString(),
                stdErr.ToString(),
                false
            );
        }
    }
}
