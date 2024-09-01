using static Dapper.SqlMapper;
using System;
using VkNet;
using VkNet.Model;

namespace kTVCSSBlazor.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        string filePath;
        static object _lock = new object();
        public FileLogger(string path)
        {
            filePath = path;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose() { }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId,
                    TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
#if DEBUG
            return;
#endif
            lock (_lock)
            {
                if (exception is null)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] ({logLevel}) " + formatter(state, exception));
                    File.AppendAllText(filePath, $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] ({logLevel}) " + formatter(state, exception) + Environment.NewLine);

                    if (logLevel == LogLevel.Information)
                    {
#if DEBUG
                        if (formatter(state, exception).Contains("listen") || formatter(state, exception).Contains("Content root path") || formatter(state, exception).Contains("started") || formatter(state, exception).Contains("Hosting environment")) return;
#endif
                        try
                        {
                            using (VkApi api = new VkApi())
                            {
                                api.Authorize(new ApiAuthParams
                                {
                                    AccessToken = "vk1.a.3laCkStQ5NFxFU537Z7MBFNgWYAfLN-EQ1_ZWd-uewQfiD7FgsIHfyYWF-dNewVPm0_L2uTdITfGDBzLC5RE-1FMJaLE4SHA2-NzBSJqaMsrdjhw5lWwl_k3F2dSj2vqHrkyXHPtwtzlyYAJ0UiDG1wenl4y9oBpbkLOhJ-knYfK8ENXtVzsDbVKHiZxOHe0KFrQ7bE0mgvbkRrxx0culw",
                                });

                                api.Messages.Send(new MessagesSendParams()
                                {
                                    ChatId = 8,
                                    Message = formatter(state, exception),
                                    RandomId = new Random().Next()
                                });
                            }
                        }
                        catch (Exception)
                        {
                            // vk unavailable
                        }
                    }
                }
                else
                {
                    if (logLevel == LogLevel.Error)
                    {
                        try
                        {
                            using (VkApi api = new VkApi())
                            {
                                api.Authorize(new ApiAuthParams
                                {
                                    AccessToken = "vk1.a.3laCkStQ5NFxFU537Z7MBFNgWYAfLN-EQ1_ZWd-uewQfiD7FgsIHfyYWF-dNewVPm0_L2uTdITfGDBzLC5RE-1FMJaLE4SHA2-NzBSJqaMsrdjhw5lWwl_k3F2dSj2vqHrkyXHPtwtzlyYAJ0UiDG1wenl4y9oBpbkLOhJ-knYfK8ENXtVzsDbVKHiZxOHe0KFrQ7bE0mgvbkRrxx0culw",
                                });

                                api.Messages.Send(new MessagesSendParams()
                                {
                                    ChatId = 5,
                                    Message = exception.ToString(),
                                    RandomId = new Random().Next()
                                });
                            }
                        }
                        catch (Exception)
                        {
                            // vk unavailable
                        }
                    }

                    Console.WriteLine($"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] ({logLevel}) " + exception.ToString());
                    File.AppendAllText(filePath, $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] ({logLevel}) " + exception.ToString() + Environment.NewLine);
                }
            }
        }
    }
}
