using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class HerdrKindTests
{
    private static RigshiftConfig Config() => new()
    {
        IdleColor = "#8A8A8E",
        Definitions = new Dictionary<string, Definition>
        {
            ["herdr"] = new() { Kind = "herdr", ProcessName = "hc", Path = @"%APPDATA%\Rigshift\herdr-server.lnk" },
            ["herdrAgain"] = new() { Kind = "herdr", ProcessName = "hc" }
        },
        Loadouts = new List<Loadout>
        {
            new()
            {
                Name = "AI", Toggle = true, Color = "#2E6BFF",
                Start = new List<string> { "herdr" },
                KillOnDisable = new List<string> { "herdr" }
            },
            new()
            {
                Name = "Both", Toggle = true, Color = "#FF3B30",
                Start = new List<string>(),
                KillOnDisable = new List<string> { "herdr", "herdrAgain" }
            },
            new()
            {
                Name = "Game", Toggle = false,
                Kill = new List<string> { "herdr" }
            }
        }
    };

    [Fact]
    public void EnablingStartsThePath()
    {
        var plan = ActionRunner.PlanEnable(Config(), "AI");

        Assert.Equal(new Step[] { new StartProcess(@"%APPDATA%\Rigshift\herdr-server.lnk") }, plan.Steps);
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void DisablingKillsTheCoreThenHerdrElevated()
    {
        var plan = ActionRunner.PlanDisable(Config(), "AI", Array.Empty<string>());

        Assert.Equal(new Step[] { new KillProcess("hc"), new KillHerdr() }, plan.Steps);

        var split = PlanPartition.Split(plan.Steps);
        Assert.Equal(new Step[] { new KillProcess("hc") }, split.User);
        Assert.Equal(new Step[] { new KillHerdr() }, split.Admin);
    }

    [Fact]
    public void KillHerdrIsPlannedOnceForSeveralHerdrDefinitions()
    {
        var plan = ActionRunner.PlanDisable(Config(), "Both", Array.Empty<string>());

        Assert.Single(plan.Steps, s => s is KillHerdr);
    }

    [Fact]
    public void OneShotKillsHerdrToo()
    {
        var plan = ActionRunner.PlanOneShot(Config(), "Game");

        Assert.Equal(new Step[] { new KillProcess("hc"), new KillHerdr() }, plan.Steps);
    }

    [Fact]
    public void LeftAloneWhileAnotherEnabledLoadoutStartsIt()
    {
        var config = Config();
        config.Loadouts[1].Start.Add("herdr");

        var plan = ActionRunner.PlanDisable(config, "AI", new[] { "Both" });

        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void KillHerdrRequiresElevation()
    {
        Assert.True(StepClass.RequiresElevation(new KillHerdr()));
    }

    [Fact]
    public void KillHerdrRoundTripsAsAFixedToken()
    {
        var formatted = AdminPayload.Format([new KillHerdr()]);

        Assert.Equal(new[] { "--admin", "kill-herdr" }, formatted);
        Assert.True(AdminPayload.TryParse(formatted, out var parsed));
        Assert.Equal(new Step[] { new KillHerdr() }, parsed);
    }

    [Theory]
    [InlineData("kill-herdr:hc")]
    [InlineData("kill-herdr:")]
    [InlineData("Kill-Herdr")]
    public void RejectsHerdrTokenVariants(string token)
    {
        Assert.False(AdminPayload.TryParse(new[] { "--admin", token }, out _));
    }
}
