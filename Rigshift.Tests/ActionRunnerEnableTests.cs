using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class ActionRunnerEnableTests
{
    private static RigshiftConfig Config() => ConfigLoader.Default();

    [Fact]
    public void EnablingAiStartsTheNativeOllamaApp()
    {
        var plan = ActionRunner.PlanEnable(Config(), "AI");

        Assert.Equal(new Step[]
        {
            new StartProcess(@"%LOCALAPPDATA%\Programs\Ollama\ollama app.exe")
        }, plan.Steps);
    }

    [Fact]
    public void EnablingWslHostedOllamaReproducesTheStartSequence()
    {
        var plan = ActionRunner.PlanEnable(WslHostedOllama.Config(), "AI");

        Assert.Equal(new Step[]
        {
            new EnsureWsl(),                                   // from "requires": ["wsl"]
            new WslPkill("ollama serve"),
            new Wait(1000),
            new StartWslService("Ollama WSL",
                "OLLAMA_HOST=0.0.0.0:11435 OLLAMA_MODELS=/usr/share/ollama/.ollama/models ollama serve"),
            new Wait(3000),
            new PortProxyRemove(11435),
            new PortProxyAdd(11435)
        }, plan.Steps);
    }

    [Fact]
    public void EnablingDevJustEnsuresWsl()
    {
        var plan = ActionRunner.PlanEnable(Config(), "Dev");

        Assert.Equal(new Step[] { new EnsureWsl() }, plan.Steps);
    }

    [Fact]
    public void AppDefinitionWithNoPathProducesNoStartStep()
    {
        var config = new RigshiftConfig
        {
            Definitions = { ["rider"] = new Definition { Kind = "app", ProcessName = "rider64" } },
            Loadouts = { new Loadout { Name = "Dev", Toggle = true, Start = { "rider" } } }
        };

        var plan = ActionRunner.PlanEnable(config, "Dev");

        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void AppDefinitionWithPathProducesStartProcessStep()
    {
        var config = new RigshiftConfig
        {
            Definitions = { ["rider"] = new Definition { Kind = "app", ProcessName = "rider64", Path = @"C:\rider64.exe" } },
            Loadouts = { new Loadout { Name = "Dev", Toggle = true, Start = { "rider" } } }
        };

        var plan = ActionRunner.PlanEnable(config, "Dev");

        Assert.Equal(new Step[] { new StartProcess(@"C:\rider64.exe") }, plan.Steps);
    }

    [Fact]
    public void WslServiceWithoutPortProxyOmitsTheNetshSteps()
    {
        var config = new RigshiftConfig
        {
            Definitions =
            {
                ["svc"] = new Definition
                {
                    Kind = "wslService", ProcessName = "svc", WindowTitle = "Svc",
                    WslCommand = "svc serve", PortProxy = false
                }
            },
            Loadouts = { new Loadout { Name = "X", Toggle = true, Start = { "svc" } } }
        };

        var plan = ActionRunner.PlanEnable(config, "X");

        Assert.DoesNotContain(plan.Steps, s => s is PortProxyAdd or PortProxyRemove);
    }

    [Fact]
    public void UnknownDefinitionIsSkippedAndWarned()
    {
        var config = new RigshiftConfig
        {
            Definitions = { ["wsl"] = new Definition { Kind = "wsl" } },
            Loadouts = { new Loadout { Name = "Dev", Toggle = true, Start = { "ghost", "wsl" } } }
        };

        var plan = ActionRunner.PlanEnable(config, "Dev");

        Assert.Equal(new Step[] { new EnsureWsl() }, plan.Steps);
        Assert.Contains(plan.Warnings, w => w.Contains("ghost"));
    }

    [Fact]
    public void RequiresIsNotDuplicatedWhenAlreadyStartedEarlier()
    {
        var config = WslHostedOllama.Config();
        config.Loadouts.Add(new Loadout { Name = "Both", Toggle = true, Start = { "wsl", "ollama" } });

        var plan = ActionRunner.PlanEnable(config, "Both");

        Assert.Single(plan.Steps, s => s is EnsureWsl);
    }
}
