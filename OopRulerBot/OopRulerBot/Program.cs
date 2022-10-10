﻿// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OopRulerBot.DI;
using OopRulerBot.DisscordControllers;
using OopRulerBot.Infra;
using OopRulerBot.Settings;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Abstractions;

namespace OopRulerBot;

public static class Program
{
    private static IContainer Container = null!;

    public static async Task Main(string[] args)
    {
        Container = BotContainerBuilder.Build();
        var serviceProvider = new AutofacServiceProvider(Container);
        

        var discordSocketClient = Container.Resolve<DiscordSocketClient>();
        discordSocketClient.Log += Container.Resolve<IDiscordLogAdapter>().HandleLogEvent;

        var discordMessageHandler =
            new DiscordCommandServiceHandler(discordSocketClient, Container.Resolve<CommandService>(), serviceProvider);
        discordSocketClient.MessageReceived += discordMessageHandler.HandleMessage;

        var discordToken = Container
            .ResolveNamed<IConfigurationProvider>(ConfigurationScopes.BotSettingsScope)
            .Get<BotSecretSettings>().DiscordToken;

        
        var commandService = Container.Resolve<CommandService>();
        await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

        var modules = commandService.Modules.ToList();
        var log = Container.Resolve<ILog>();
        foreach (var module in modules)
            log.Info(module.Name);

        await discordSocketClient.LoginAsync(TokenType.Bot, discordToken);
        await discordSocketClient.StartAsync();
        await Task.Delay(-1);
    }
}