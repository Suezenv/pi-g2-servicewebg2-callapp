using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWebG2_CallApp
{
    internal static class AppToCall
    {
        internal static async Task CallAppFromConfig(string appCalled, AppDefinition configValues, string additionalArgs = null)
        {
            ProcessResult result = new ProcessResult(appCalled,configValues);

            try
            {
                //Execution du programme :
                //Vérification de l'existance du répertoire de travail
                string workingDirectory = Path.GetDirectoryName(configValues.CustomProcessStartInfo.FileName);
                if (Directory.Exists(workingDirectory))
                {
                    //Lecture de la configuration du programme
                    ProcessStartInfo pinfo = new ProcessStartInfo();
                    PropertyInfo[] properties = typeof(ProcessStartInfo).GetProperties();
                    PropertyInfo[] customProperties = typeof(CustomProcessStartInfo).GetProperties();
                    foreach (PropertyInfo customProperty in customProperties)
                    {
                        foreach (PropertyInfo property in properties)
                        {
                            if (property.Name == customProperty.Name)
                            {
                                property.SetValue(pinfo, customProperty.GetValue(configValues.CustomProcessStartInfo));
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(additionalArgs))
                        pinfo.Arguments += " " + additionalArgs;

                    //Création du programme
                    using (Process process = new Process() { StartInfo = pinfo, EnableRaisingEvents = true })
                    {
                        // List of tasks to wait for a whole process exit
                        List<Task> processTasks = new List<Task>();

                        // === EXITED Event handling ===
                        TaskCompletionSource<object> processExitEvent = new TaskCompletionSource<object>();
                        process.Exited += (sender, args) =>
                        {
                            processExitEvent.TrySetResult(true);
                        };
                        processTasks.Add(processExitEvent.Task);

                        // === STDOUT handling ===
                        StringBuilder stdOutBuilder = new StringBuilder();
                        if (process.StartInfo.RedirectStandardOutput)
                        {
                            TaskCompletionSource<bool> stdOutCloseEvent = new TaskCompletionSource<bool>();
                            process.OutputDataReceived += (s, e) =>
                            {
                                if (e.Data == null)
                                    stdOutCloseEvent.TrySetResult(true);
                                else
                                    stdOutBuilder.AppendLine(e.Data);
                            };
                            processTasks.Add(stdOutCloseEvent.Task);
                        }

                        // === STDERR handling ===
                        StringBuilder stdErrBuilder = new StringBuilder();
                        if (process.StartInfo.RedirectStandardError)
                        {
                            TaskCompletionSource<bool> stdErrCloseEvent = new TaskCompletionSource<bool>();
                            process.ErrorDataReceived += (s, e) =>
                            {
                                if (e.Data == null)
                                    stdErrCloseEvent.TrySetResult(true);
                                else
                                    stdErrBuilder.AppendLine(e.Data);
                            };
                            processTasks.Add(stdErrCloseEvent.Task);
                        }

                        // === START OF PROCESS ===
                        if (process.Start())
                        {
                            // Reads the output stream first as needed and then waits because deadlocks are possible
                            if (process.StartInfo.RedirectStandardOutput)
                                process.BeginOutputReadLine();

                            if (process.StartInfo.RedirectStandardError)
                                process.BeginErrorReadLine();

                            // === ASYNC WAIT OF PROCESS ===

                            // Process completion = exit AND stdout (if defined) AND stderr (if defined)
                            Task processCompletionTask = Task.WhenAll(processTasks);

                            // Task to wait for exit OR timeout (if defined)
                            Task<Task> awaitingTask = configValues.TimeoutMs.HasValue ? Task.WhenAny(Task.Delay(configValues.TimeoutMs.Value), processCompletionTask) : Task.WhenAny(processCompletionTask);

                            // Let's now wait for something to end...
                            if ((await awaitingTask.ConfigureAwait(false)) == processCompletionTask)
                            {
                                // -> Process exited cleanly
                                result.ExitCode = process.ExitCode;
                            }
                            else
                            {
                                // -> Timeout, let's kill the process
                                try
                                {
                                    process.Kill();
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            // Read stdout/stderr
                            result.StdOut = stdOutBuilder.ToString();
                            result.StdErr = stdErrBuilder.ToString();
                        }
                        else
                        {
                            result.ExitCode = process.ExitCode;
                            result.UnexpectedError = "Le lancement du programme a échoué";
                        }
                    }
                }
                else
                {
                    result.UnexpectedError = "Le répertoire " + workingDirectory + " n'existe pas.";
                }

                result.LogProcessResult();
            }
            catch (Exception ex)
            {
                string errMsg = "Une erreur inattendue est survenue lors du démarrage du programme";
                result.UnexpectedError = string.Concat(errMsg, ex);
            }
        }

        internal class ProcessResult
        {
            private readonly string _appCalled;
            private readonly AppDefinition _configValues;
            private readonly bool _isActive;
            private readonly bool _logError;
            private readonly bool _logOutput;

            internal ProcessResult(string appCalled, AppDefinition configValues)
            {
                _appCalled = appCalled;
                _configValues = configValues;
                _logError = _configValues.CustomProcessStartInfo.RedirectStandardError;
                _logOutput = _configValues.CustomProcessStartInfo.RedirectStandardOutput;
                _isActive = !string.IsNullOrWhiteSpace(_configValues.RedirectionPath) && (_logError || _logOutput);
            }

            /// <summary>
            /// Exit code
            /// <para>If NULL, process exited due to timeout</para>
            /// </summary>
            internal int? ExitCode { get; set; } = null;

            /// <summary>
            /// Standard error stream
            /// </summary>
            internal string StdErr { get; set; } = "";

            /// <summary>
            /// Standard output stream
            /// </summary>
            internal string StdOut { get; set; } = "";

            /// <summary>
            /// Erreur de traitement innatendue
            /// </summary>
            internal string UnexpectedError { get; set; } = "";

            private string LogFilePath
            {
                get { return _configValues.RedirectionPath + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_ServiceWebG2_CallApp_" + _appCalled + "_AppStandardOutput.log"; }
            }
            private string ErrorFilePath
            {
                get { return _configValues.RedirectionPath + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_ServiceWebG2_CallApp_" + _appCalled + "_AppStandardError.err"; }
            }

            internal void LogProcessResult()
            {
                if (_isActive)
                {
                    if (_logOutput && !string.IsNullOrWhiteSpace(StdOut))
                    {
                        try
                        {
                            File.AppendAllText(LogFilePath, DateTime.Now.ToString("HH:mm:ss") + "\t" + StdOut, Encoding.Unicode);
                        }
                        catch (Exception) { }
                    }

                    if (_logError && !string.IsNullOrWhiteSpace(StdErr))
                    {
                        try
                        {
                            File.AppendAllText(ErrorFilePath, DateTime.Now.ToString("HH:mm:ss") + "\t" + StdErr, Encoding.Unicode);
                        }
                        catch (Exception) { }
                    }
                }
            }
        }
    }
}
