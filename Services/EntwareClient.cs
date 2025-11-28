using System;
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
                ) {
                    Timeout = ConnectionTimeout
                };

                sshClient = new SshClient(connectionInfo);
                await sshClient.ConnectAsync(CancellationToken.None);
            }
            catch (Exception ex) {
                CleanupClient();
                return $"{nameof(EntwareClient)} CONNECTION_ERROR\n{ex.Message}";
            }
        }

        return "SUCCESS";
    }

    public void Disconnect() {
        if (!disposed)
            CleanupClient();
    }

    public bool CheckInitialConnection() {
        return IsConnected();
    }

    public async Task<string> GetUserConfigAsync() {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        try {
            return await ReadOperationAsync(async () => {
                return await Task.Run(() => {
                    using (SshCommand? cmd = sshClient!.CreateCommand($"cat ~/../etc/mihomo/config.yaml")) {
                        var result = cmd.Execute();

                        if (cmd.ExitStatus != 0)
                            throw new Exception($"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}");

                        return result;
                    }
                });
            },
            $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
        }
        catch (Exception ex) {
            return $"{nameof(EntwareClient)} {ex.Message}";
        }
    }

    //public async Task SaveUserConfigAsync(string text) {

    //}

    //public async Task<X> BackupEntwareAsync() {

    //}

    public async Task<string> ExecuteCommandAsync(string command) {
        if (disposed)
            return $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.disposed"]}";

        try {
            return await ReadOperationAsync(async () => {
                return await Task.Run(() => {
                    using (SshCommand? cmd = sshClient!.CreateCommand(command)) {
                        var result = cmd.Execute();

                        if (cmd.ExitStatus != 0)
                            throw new Exception($"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}\n{cmd.Error}");

                        return result;
                    }
                });
            },
            $"{nameof(EntwareClient)} {Localizer.Instance["sshPanel.errors.commandFailed"]}");
        }
        catch (Exception ex) {
            return $"{nameof(EntwareClient)} {ex.Message}";
        }
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
                throw new Exception($"{nameof(EntwareClient)} {errorMessage}\n{ex.Message}");
            }
        }
    }

    private async Task WriteOperationAsync(Func<Task> operation, string errorMessage) {
        try {
            string connectStatus = await ConnectAsync();

            if (connectStatus != "SUCCESS")
                throw new Exception($"{nameof(EntwareClient)} {connectStatus}");

            await operation();
        }
        catch (Exception) {
            try {
                await ForceReconnectAsync();
                await operation();
            }
            catch (Exception ex) {
                throw new Exception($"{nameof(EntwareClient)} {errorMessage}\n{ex.Message}");
            }
        }
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
