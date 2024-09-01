using CurrieTechnologies.Razor.SweetAlert2;
using kTVCSSBlazor.Components.Pages;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System.Text.RegularExpressions;
using VkNet.Model;

namespace kTVCSSBlazor
{
    public class NodesKeeper
    {
        private Dictionary<string, bool> nodes = new Dictionary<string, bool>();
        private IConfiguration configuration;
        private ILogger logger;
        private SshClient client;

        private string RunSshCommand(string serviceName)
        {
            if (!client.IsConnected)
            {
                client.Connect();
            }

            var command = client.CreateCommand($"systemctl status {serviceName}");
            var result = command.Execute();

            return result;
        }

        private bool ParseServiceStatus(string output)
        {
            var regex = new Regex(@"Active:\s+active \(running\)");
            return regex.IsMatch(output);
        }

        private async Task ExecuteNode(string serviceName, string execType)
        {
            if (!client.IsConnected)
            {
                client.Connect();
            }

            var command = client.CreateCommand($"systemctl {execType} {serviceName}");
            var result = command.Execute();
        }

        public async Task Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        foreach (var node in nodes)
                        {
                            string output = RunSshCommand(node.Key);

                            var result = ParseServiceStatus(output);

                            nodes[node.Key] = result;

                            if (result == false)
                            {
                                ExecuteNode(node.Key, "restart");
                                //logger.LogInformation($"kTVCSSBlazor выполнил перезапуск ноды {node.Key} из-за DEAD стейта");
                            }
                        }
                    }
                    catch (Exception ex) 
                    {
                        logger.LogError(ex.ToString());
                    }

                    await Task.Delay(30 * 1000);
                }
            });
        }

        public NodesKeeper(IConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;

            nodes.Add("node-ktvcss1", false);
            nodes.Add("node-ktvcss2", false);
            nodes.Add("node-ktvcss3", false);
            nodes.Add("node-ktvcss4", false);
            nodes.Add("node-ktvcss5", false);
            nodes.Add("node-ktvcss6", false);
            nodes.Add("node-ktvcss-auth", false);

            client = new SshClient(configuration["ssh_host"], 1337, configuration["ssh_user"], configuration["ssh_password"]);
        }
    }
}
