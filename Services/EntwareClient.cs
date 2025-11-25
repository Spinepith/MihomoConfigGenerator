using System;
using System.Threading.Tasks;
using Renci.SshNet;
using MihomoProxyGenerator.Data.Localization;
using Renci.SshNet.Common;

namespace MihomoProxyGenerator.Services;

public class EntwareClient : IDisposable {
    private readonly string username;
    private readonly string ip;
    private readonly string port;
    private readonly string password;

    private SshClient? sshClient;
    private SftpClient? sftpClient;

    private bool disposed;

    public EntwareClient(string username, string ip, string port, string password) {
        this.username = username;
        this.ip = ip;
        this.port = port;
        this.password = password;

        disposed = false;

        try {
            System.Diagnostics.Debug.WriteLine("Пытаемся подключиться по SSH");
            sshClient = new SshClient(ip, int.Parse(port), username, password);
            sshClient.Connect();

            System.Diagnostics.Debug.WriteLine("SSH подключен, проверяем команды");
            var testCommand = sshClient.CreateCommand("pwd");
            string result = testCommand.Execute();
            System.Diagnostics.Debug.WriteLine($"Команда выполнена: {result}");

            System.Diagnostics.Debug.WriteLine("Пытаемся подключиться по SFTP");
            sftpClient = new SftpClient(ip, int.Parse(port), username, password);
            sftpClient.Connect();
            System.Diagnostics.Debug.WriteLine("SFTP подключен успешно");
        }
        catch (SshAuthenticationException ex) {
            Dispose();
            throw new Exception($"Ошибка аутентификации: {ex.Message}");
        }
        catch (SshConnectionException ex) {
            Dispose();
            throw new Exception($"Ошибка подключения SSH: {ex.Message}");
        }
        catch (Exception ex) {
            Dispose();
            throw new Exception($"Общая ошибка: {ex.Message}");
        }
    }

    public string CheckInitialConnection() {
        return IsConnected() ? "SUCCESSFULL" : "NOT_CONNECTED";
    }

    public async Task<string> ExecuteCommand(string command) {
        CheckDisposed();

        return await Task.Run(() => {
            return ReadOperation(() => {
                SshCommand? cmd = sshClient!.CreateCommand(command);
                var result = cmd.Execute();

                if (cmd.ExitStatus != 0)
                    throw new Exception(cmd.Error);

                return result;
            },
            Localizer.Instance["sshPanel.errors.commandFailed"]);
        });
    }

    private void Connect() {
        CheckDisposed();

        if (sshClient is null || !sshClient.IsConnected) {
            sshClient?.Dispose();
            sshClient = new SshClient(username: username, host: ip, port: int.Parse(port), password: password);
            sshClient.Connect();
        }

        if (sftpClient is null || !sftpClient.IsConnected) {
            sftpClient?.Dispose();
            sftpClient = new SftpClient(username: username, host: ip, port: int.Parse(port), password: password);
            sftpClient.Connect();
        }
    }

    private void ForceReconnect() {
        CheckDisposed();

        sshClient?.Dispose();
        sftpClient?.Dispose();

        sshClient = null;
        sftpClient = null;

        Connect();
    }

    private bool IsConnected() {
        return !disposed && sshClient?.IsConnected is true && sftpClient?.IsConnected is true;
    }

    private string ReadOperation(Func<string> operation, string errorMessage) {
        CheckDisposed();

        try {
            Connect();
            return operation();
        }
        catch (Exception) {
            try {
                ForceReconnect();
                return operation();
            }
            catch (Exception ex) {
                throw new Exception($"{errorMessage}: {ex.Message}");
            }
        }
    }

    private void WriteOperation(Action operation, string errorMessage) {
        CheckDisposed();

        try {
            Connect();
            operation();
        }
        catch (Exception) {
            try {
                ForceReconnect();
                operation();
            }
            catch (Exception ex) {
                throw new Exception($"{errorMessage}: {ex.Message}");
            }
        }
    }

    private void CheckDisposed() {
        if (disposed)
            throw new ObjectDisposedException(nameof(EntwareClient), Localizer.Instance["sshPanel.errors.disposed"]);
    }

    public void Dispose() {
        if (disposed)
            return;

        try {
            sshClient?.Disconnect();
            sshClient?.Dispose();

            sftpClient?.Disconnect();
            sftpClient?.Dispose();
        }
        finally {
            disposed = true;
        }
    }
}
