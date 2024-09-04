using CurrieTechnologies.Razor.SweetAlert2;
using Dapper;
using kTVCSSBlazor;
using kTVCSSBlazor.Components;
using kTVCSSBlazor.Components.Pages.Players;
using kTVCSSBlazor.Data;
using kTVCSSBlazor.Db;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.Players;
using kTVCSSBlazor.Db.Repository;
using kTVCSSBlazor.Hubs;
using kTVCSSBlazor.Mixes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using kTVCSSBlazor.Logging;
using Microsoft.EntityFrameworkCore;
using Blazored.Toast;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ClipboardService>();

builder.Logging.AddFile(Path.Combine(Directory.GetCurrentDirectory(), "logger.txt"));

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024 * 100;
    options.MaximumParallelInvocationsPerClient = 1;
});

builder.Services.AddSingleton<kTVCSSHub>(x => new kTVCSSHub(builder.Configuration.GetConnectionString("db"), new FileLogger("logger.txt"), builder.Configuration));
builder.Services.AddSingleton<STVDirectorHub>(x => new STVDirectorHub(builder.Configuration.GetConnectionString("db")));

builder.Services.AddScoped<NotificationService>();

builder.Services.AddScoped(sp =>
{
    var hubConnection = new HubConnectionBuilder()
        .WithUrl("http://localhost:4001/ktvcss")
        .Build();

    return hubConnection;
});

builder.Services.AddSweetAlert2(options => {
    options.Theme = SweetAlertTheme.Dark;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddOptions();

builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddScoped<kTVCSSUserService>();
builder.Services.AddScoped<kTVCSSAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<kTVCSSAuthenticationStateProvider>());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;

    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 1000;
    config.SnackbarConfiguration.ShowTransitionDuration = 1000;
});

//var serviceProvider = builder.Services.BuildServiceProvider();

builder.Services.AddSingleton(provider => new Context(builder.Configuration.GetConnectionString("db")));
builder.Services.AddSingleton<IUserFeed, kTVCSSBlazor.Db.Repository.UserFeed>(provider => new kTVCSSBlazor.Db.Repository.UserFeed(builder.Configuration.GetConnectionString("db")));
builder.Services.AddSingleton<IAdmins, kTVCSSBlazor.Db.Repository.Admins>(provider => new kTVCSSBlazor.Db.Repository.Admins(builder.Configuration, new FileLogger("logger.txt")));
builder.Services.AddSingleton<IModerators, kTVCSSBlazor.Db.Repository.Moderators>(provider => new kTVCSSBlazor.Db.Repository.Moderators(builder.Configuration, new FileLogger("logger.txt")));
builder.Services.AddSingleton<IPlayers, kTVCSSBlazor.Db.Repository.Players>(provider => new kTVCSSBlazor.Db.Repository.Players(builder.Configuration, new FileLogger("logger.txt")));
builder.Services.AddSingleton<IHighlights, kTVCSSBlazor.Db.Repository.Highlights>(provider => new kTVCSSBlazor.Db.Repository.Highlights(builder.Configuration, new Vips(builder.Configuration, new FileLogger("logger.txt"))));
builder.Services.AddSingleton<IMatches, Matches>(provider => new Matches(builder.Configuration.GetConnectionString("db")));
builder.Services.AddSingleton<ITeams, Teams>(provider => new Teams(builder.Configuration.GetConnectionString("db"), new FileLogger("logger.txt")));
builder.Services.AddSingleton<IVips, Vips>(provider => new Vips(builder.Configuration, new FileLogger("logger.txt")));
builder.Services.AddSingleton<IGameServers, GameServers>(provider => new GameServers(builder.Configuration.GetConnectionString("db")));
builder.Services.AddSingleton<IFAQ, FAQ>(provider => new FAQ(builder.Configuration.GetConnectionString("db")));
builder.Services.AddSingleton<IIM, IM>(provider => new IM(builder.Configuration.GetConnectionString("db")));

builder.Services.AddBlazoredToast();

var app = builder.Build();

NodesKeeper nodesKeeper = new NodesKeeper(builder.Configuration, new FileLogger("logger.txt"));
nodesKeeper.Start();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStaticFiles();

app.MapGet("/api/RequestUpdateTotalPlayers", () => {
    Task task = new Task(async () =>
    {
        using (SqlConnection db = new SqlConnection(builder.Configuration.GetConnectionString("db")))
        {
            db.Open();

            var players = await db.QueryAsync<TotalPlayer>("[kTVCSS].[dbo].[GetTotalPlayers]", commandType: CommandType.StoredProcedure, commandTimeout: 6000);

            kTVCSSBlazor.Db.Repository.Players.MemoryCache.Set("TotalPlayerListMemory", players);

            File.WriteAllText("players.cache", JsonConvert.SerializeObject(players));
        }
    });
    task.Start();
});

app.MapGet("/api/DeleteMixMemory", (string guid) => {
    Task task = new Task(async () =>
    {
        using (SqlConnection db = new SqlConnection(builder.Configuration.GetConnectionString("db")))
        {
            db.Open();

            try
            {
                var mix = kTVCSSHub.Mixes.Where(x => x.Guid.ToString().ToLower() == guid.ToLower()).FirstOrDefault();

                if (mix is not null)
                {
                    foreach (var player in mix.MixPlayers)
                    {
                        try
                        {
                            kTVCSSHub.Instance.Clients.Client(player.ConnectionID).SendAsync("MixEnded");
                        }
                        catch (Exception)
                        {
                            // ����� ����� ��� �� � ����
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ����� ��� � ������
            }

            kTVCSSHub.Mixes.RemoveAll(x => x.Guid.ToString().ToLower() == guid.ToLower());

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("GUID", guid);

            db.Execute("DeleteMixByGUID", dynamicParameters, commandType: CommandType.StoredProcedure);
        }
    });
    task.Start();
});

app.MapHub<kTVCSSHub>("/ktvcss");
app.MapHub<STVDirectorHub>("/STVDirectorHub");
 
// причина - количество дней
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("Читерство", 3650);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("Использование багов", 2);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.1.1 Никнеймы и клан-теги, содержащие в себе любую рекламу, фашизм, экстремизм, ссылки на сторонние ресурсы, оскорбления Игроков или Администрации", 30);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.1.2 Никнеймы, содержащие плагиат текущих ников Администраторов Проекта", 7);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.2.1 Умышленные действия, препятствующие нормальному игровому процессу других Игроков, т.е. грифинг или троллинг (умышленное удерживание бомбы или ее закидывание в недоступные места, стрельба в голову союзникам, загораживание снайперам обзора, бросание световых гранат перед союзниками и т.д.)", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.2.2 Мониторинг Игроков противоположной команды, когда вы мертвы  или  играете за другую команду (сообщение местоположения вражеских Игроков через любую стороннюю связь: Discord, Skype, TeamSpeak3 соцсети, мессенджеры и т.д.)", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.2.4 Использование багов карты, дающих любые преимущества над другими Игроками (разминирование через стены, щели и отверстия в стенах карты и т.д.)", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.2.5 Выход во время игры в Matchmaking (находясь в лобби, до запуска матча, во время матча)", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.2 Любые неадекватные действия с микрофоном: крики, свист, музыка", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.3 Флуд чата однотипными сообщениями", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.4 Оскорбления других Игроков или Администрации. Ответ оскорблением на оскорбление также считается нарушением", 3);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.5 Реклама сторонних ресурсов, не относящихся к Проекту", 5);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.6 Использование программы HLDJ, ClownFish и других подобных программ", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.7 Подстрекательство к бану Игроков, которые не нарушают правил Проекта", 1);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.3.3.8 Запрещено вставать на защиту людей использующих сторонние ПО. Выпрашивать разбан или кам-статус для подобных людей", 7);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.4.1 Разжигать ненависть или вражду, унижать достоинства Пользователей Проекта и их родных по признакам пола, расы, национальности, языка, происхождения, насилия, отношения к религии, а равно принадлежности к какой-либо социальной группе", 21);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.4.2 Обсуждать политику в любом виде, размещать в изображениях ClientMod профиле или любых сообщениях символику или лозунги или призывы использующиеся военными любой из сторон, флаги и символику непризнанных государств, любых политических партий, запрещенных и террористических организаций", 7);
kTVCSSBlazor.Db.Models.Players.Block.Reasons.Add("3.4.3 Создавать более одного Аккаунта. При обнаружении такие Аккаунты блокируются безвозвратно", 30);

Task.Run(async () =>
{
    Console.WriteLine("Starting demos cleaner");

    int deleted = 0;

    if (!Directory.Exists(Path.Combine("wwwroot", "demos")))
    {
        Directory.CreateDirectory(Path.Combine("wwwroot", "demos"));
    }

    FileInfo[] files = new DirectoryInfo(Path.Combine("wwwroot", "demos")).GetFiles("*", SearchOption.AllDirectories);

    foreach (FileInfo file in files)
    {
        if (DateTime.Now.Subtract(file.CreationTime).TotalDays > 30)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                deleted += 1;
            }
        }
    }

    Console.WriteLine($"Finished: {deleted} files");
});

app.UseAntiforgery();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();