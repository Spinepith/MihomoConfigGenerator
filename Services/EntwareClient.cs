using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using XKeenMihomoGenerator.Data.Localization;

namespace XKeenMihomoGenerator.Services;

public class EntwareClient : IDisposable {
    private readonly string username;
    private readonly string host;
    private readonly string port;
    private readonly string password;

    private SshClient? sshClient;

    private bool disposed;

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);
    private static readonly string configPath = "~/../etc/mihomo/config.yaml";

    public event Action<bool>? OnBusyStateChanged;
    public event Action<double, bool>? OnProgressChanged;

    private CancellationTokenSource? currentOperationCts;

    public EntwareClient(string username, string host, string port, string password) {
        this.username = username;
        this.host = host;
        this.port = port;
        this.password = password;

        disposed = false;
    }

    public async Task<string> ConnectAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        if (!IsConnected()) {
            try {
                CleanupClient();

                var connectionInfo = new ConnectionInfo(
                    host: host,
                    port: int.Parse(port),
                    username: username,
                    authenticationMethods: new PasswordAuthenticationMethod(username, password)
                ) { Timeout = ConnectionTimeout };

                sshClient = new SshClient(connectionInfo);
                await sshClient.ConnectAsync(CancellationToken.None);
            }
            catch (Exception ex) {
                CleanupClient();
                Logger.Log($"{nameof(ConnectAsync)}\n{ex}");
                return $"{nameof(EntwareClient)} CONNECTION_ERROR\n{ex.Message}";
            }
        }

        return "SUCCESS";
    }

    public void Disconnect() {
        if (!disposed)
            CleanupClient();
    }

    public bool CheckConnection() {
        return IsConnected();
    }

    public async Task<string> GetUserConfigAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await ReadOperationAsync(async () => {
            return await Task.Run(() => {
                using (SshCommand? cmd = sshClient!.CreateCommand($"cat {configPath}")) {
                    var result = cmd.Execute();

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(GetUserConfigAsync)}\n{result}");
                    return result;
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> SaveUserConfigAsync(string text) {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await WriteOperationAsync(async () => {
            await Task.Run(() => {
                string base64content = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

                using (SshCommand? cmd = sshClient!.CreateCommand($"echo  -n '{base64content}' | base64 -d > {configPath}")) {
                    var result = cmd.Execute();

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(SaveUserConfigAsync)}\n{result}");
                    return result;
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> BackupEntwareAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        currentOperationCts = new CancellationTokenSource();
        var token = currentOperationCts.Token;

        return await ReadOperationAsync(async () => {
            string tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{Path.GetRandomFileName()}.tar.gz");
            try {
                long? fileSize = null;
                using (SshCommand? sizeCmd = sshClient!.CreateCommand("du -sk /opt | cut -f1")) {
                    var sizeResult = sizeCmd.Execute();
                    if (sizeCmd.ExitStatus == 0 && long.TryParse(sizeResult.Trim(), out long kilobytes))
                        fileSize = kilobytes * 1024;
                }

                string command = "tar --exclude=entware_backup.tar.gz --exclude=*.pid --exclude=*.sock --warning=no-file-changed -czf - -C /opt .";
                string result = await DownloadOperationAsync(tempFile, command, token, fileSize);

                if (result == "CANCELED_BY_TOKEN") {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                    return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}";
                }
                else if (result.StartsWith(nameof(EntwareClient)) && File.Exists(tempFile))
                    File.Delete(tempFile);

                Logger.Log($"{nameof(ExecuteCommandAsync)}\n{result}");
                return result;
            }
            catch (Exception ex) {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{ex.Message}";
            }
            finally {
                currentOperationCts?.Dispose();
                currentOperationCts = null;
            }
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> BackupConfigAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        currentOperationCts = new CancellationTokenSource();
        var token = currentOperationCts.Token;

        return await ReadOperationAsync(async () => {
            string tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{Path.GetRandomFileName()}.yaml");
            try {
                long? fileSize = null;
                
                using (SshCommand? sizeCmd = sshClient!.CreateCommand($"wc -c < {configPath}")) {
                    var sizeResult = sizeCmd.Execute();
                    if (sizeCmd.ExitStatus == 0 && long.TryParse(sizeResult.Trim(), out long size))
                        fileSize = size;
                }

                string command = $"cat {configPath}";
                string result = await DownloadOperationAsync(tempFile, command, token, fileSize);

                if (result == "CANCELD_BY_TOKEN") {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                    return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}";
                }
                else if (result.StartsWith(nameof(EntwareClient)) && File.Exists(tempFile))
                    File.Delete(tempFile);

                Logger.Log($"{nameof(ExecuteCommandAsync)}\n{result}");
                return result;
            }
            catch (Exception ex) {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{ex.Message}";
            }
            finally {
                currentOperationCts?.Dispose();
                currentOperationCts = null;
            }
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> XKeenStatusAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await ReadOperationAsync(async () => {
            return await Task.Run(() => {
                using (SshCommand? cmd = sshClient!.CreateCommand("xkeen -status")) {
                    var result = cmd.Execute();

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(XKeenStatusAsync)}\n{result}");

                    string[] greenColorCodes = { "\u001b[32m", "\x1B[32m", "\\033[32m", "[32m" };
                    string[] redColorCodes = { "\u001b[31m", "\x1B[31m", "\\033[31m", "[31m" };

                    if (greenColorCodes.Any(code => result.Contains(code)))
                        return "ON";

                    if (redColorCodes.Any(code => result.Contains(code)))
                        return "OFF";
                    
                    return result;
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> XKeenStartAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await ReadOperationAsync(async () => {
            return await Task.Run(async () => {
                string[] greenColorCodes = { "\u001b[32m", "\x1B[32m", "\\033[32m", "[32m" };
                string[] redColorCodes = { "\u001b[31m", "\x1B[31m", "\\033[31m", "[31m" };

                var output = new StringBuilder();
                var startTime = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(15);

                using (ShellStream? shellStream = sshClient!.CreateShellStream("xkeen-term", 80, 24, 800, 600, 1024)) {
                    using var reader = new StreamReader(shellStream);
                    using var writer = new StreamWriter(shellStream) { AutoFlush = true };

                    await writer.WriteLineAsync("xkeen -start");
                    await writer.WriteLineAsync();

                    while (DateTime.Now - startTime < timeout) {
                        var remainingTime = timeout - (DateTime.Now - startTime);
                        if (remainingTime <= TimeSpan.Zero)
                            break;

                        var lineTask = reader.ReadLineAsync();

                        if (await Task.WhenAny(lineTask, Task.Delay(remainingTime)) != lineTask)
                            break;

                        var line = await lineTask;
                        if (line == null)
                            break;

                        if (string.IsNullOrWhiteSpace(line) || line.Contains("xkeen -start"))
                            continue;

                        output.AppendLine(line);
                        Logger.Log($"{nameof(XKeenStartAsync)} {line}\n", false);

                        if (greenColorCodes.Any(code => line.Contains(code))) {
                            try {
                                await writer.WriteAsync("\n");
                                await Task.Delay(1000);
                            }
                            catch { }
                            return "";
                        }
                        
                        if (redColorCodes.Any(code => line.Contains(code))) {
                            try {
                                await writer.WriteAsync("\x03");
                                await Task.Delay(1000);
                            }
                            catch { }
                            return $"{nameof(EntwareClient)}\n{output.ToString()}";
                        }
                    }

                    return $"{nameof(EntwareClient)} {output.ToString()}";
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> XKeenStopAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await ReadOperationAsync(async () => {
            return await Task.Run(() => {
                using (SshCommand? cmd = sshClient!.CreateCommand("xkeen -stop")) {
                    var result = cmd.Execute();

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(XKeenStopAsync)}\n{result}");
                    return result;
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public async Task<string> ExecuteCommandAsync(string command) {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        return await ReadOperationAsync(async () => {
            return await Task.Run(() => {
                using (SshCommand? cmd = sshClient!.CreateCommand(command)) {
                    var result = cmd.Execute();

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(ExecuteCommandAsync)}\n{result}");
                    return result;
                }
            });
        },
        $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
    }

    public void CancelOperation() {
        currentOperationCts?.Cancel();
    }

    private async Task ForceReconnectAsync() {
        CleanupClient();
        await ConnectAsync();
    }

    private void CleanupClient() {
        if (sshClient != null) {
            if (sshClient.IsConnected)
                sshClient.Disconnect();

            sshClient.Dispose();
            sshClient = null;
        }
    }

    private bool IsConnected() {
        return !disposed && sshClient?.IsConnected is true;
    }

    private async Task<string> ReadOperationAsync(Func<Task<string>> operation, string errorMessage) {
        try {
            OnBusyStateChanged?.Invoke(true);
            string connectStatus = await ConnectAsync();

            if (connectStatus != "SUCCESS")
                throw new Exception($"{nameof(EntwareClient)} {connectStatus}");

            return await operation();
        }
        catch (Exception) {
            try {
                await ForceReconnectAsync();
                return await operation();
            }
            catch (Exception ex) {
                return $"{nameof(EntwareClient)} {errorMessage}\n{ex.Message}";
            }
        }
        finally {
            OnBusyStateChanged?.Invoke(false);
        }
    }

    private async Task<string> WriteOperationAsync(Func<Task> operation, string errorMessage) {
        try {
            OnBusyStateChanged?.Invoke(true);
            string connectStatus = await ConnectAsync();

            if (connectStatus != "SUCCESS")
                throw new Exception($"{nameof(EntwareClient)} {connectStatus}");

            await operation();
            return "";
        }
        catch (Exception) {
            try {
                await ForceReconnectAsync();
                await operation();
                return "";
            }
            catch (Exception ex) {
                return $"{nameof(EntwareClient)} {errorMessage}\n{ex.Message}";
            }
        }
        finally {
            OnBusyStateChanged?.Invoke(false);
        }
    }

    private async Task<string> DownloadOperationAsync(string path, string command, CancellationToken token, long? fileSize) {
        return await Task.Run(() => {
            try {
                try {
                    string? _path = Path.GetDirectoryName(path);
                    if (_path != null && !Directory.Exists(_path))
                        Directory.CreateDirectory(_path);

                    string testFile = Path.Combine(_path!, "TEST.tmp");
                    File.WriteAllText(testFile, "TEST");
                    File.Delete(testFile);
                }
                catch {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string fileName = Path.GetFileName(path);
                    path = Path.Combine(appData, "XKeenMihomoGenerator", "Data", fileName);
                }

                using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (SshCommand cmd = sshClient!.CreateCommand(command)) {
                    var result = cmd.BeginExecute();

                    using (Stream? reader = cmd.OutputStream) {
                        var buffer = new byte[8192];
                        int bytesReaded;
                        long totalBytesRead = 0;
                        double lastReportedValue = 0;

                        while ((bytesReaded = reader.Read(buffer, 0, buffer.Length)) > 0) {
                            if (token.IsCancellationRequested)
                                return "CANCELED_BY_TOKEN";

                            outputStream.Write(buffer, 0, bytesReaded);
                            totalBytesRead += bytesReaded;

                            if (OnProgressChanged != null) {
                                double currentProgress;
                                if (fileSize != null)
                                    currentProgress = totalBytesRead / (double)fileSize * 100;
                                else
                                    currentProgress = totalBytesRead / 1024 / 1024;

                                if (currentProgress - lastReportedValue >= 0.1 || (fileSize != null && currentProgress >= 100)) {
                                    OnProgressChanged?.Invoke(currentProgress, fileSize != null);
                                    lastReportedValue = currentProgress;
                                }
                            }
                        }

                        outputStream.Flush();
                        OnProgressChanged?.Invoke(fileSize != null ? 100 : (double)totalBytesRead / 1024 / 1024, fileSize != null);
                    }

                    cmd.EndExecute(result);

                    if (cmd.ExitStatus != 0)
                        return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}";

                    Logger.Log($"{nameof(ExecuteCommandAsync)}\n{result}");
                    return path;
                }
            }
            catch (Exception ex) {
                return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{ex.Message}";
            }
        },
        token);
    }

    public void Dispose() {
        if (disposed)
            return;

        try {
            CleanupClient();
        }
        finally {
            disposed = true;
        }
    }
}
