using System.Diagnostics;
using Spectre.Console;

var brain = new Brain() { Name = "test 1", Days = 1, Mood = Mood.Content, Fog = 5, TimeStamp = DateTime.Now.AddHours(-1) };
brain.DoActivity();

public class Brain
{
    public required string Name { get; init; }
    public required int Days { get; init; }
    public required DateTime TimeStamp { get; set; }
    public required Mood Mood { get; set; }
    public int FoodLevel { get; set; }
    public int Plays { get; set; }
    public int Fog { get; set; }
    public int Love => FoodLevel == 3 && Plays == 2 && Fog == 0 ? 1 : 0;
    public int Care => FoodLevel + Plays + Love;

    public void DoActivity()
    {
        if (DateTime.Now.AddHours(-1) < TimeStamp)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]You should wait at least [italic blue]1 hour[/] after the last activity tried[/] ([blue]{TimeStamp - DateTime.Now.AddHours(-1)}[/] remaining)");
            return;
        }
        var activity = AnsiConsole.Prompt(new SelectionPrompt<Activity>().Title("Select the activity").AddChoices([..FoodLevel >= 3 ? Array.Empty<Activity>() : [Activity.Eat], ..Fog <= 0 ? Array.Empty<Activity>() : [Activity.Clean], ..Mood is Mood.Awakened ? Array.Empty<Activity>() : [Activity.Play]]));
        var numDice = Mood switch
        {
            Mood.Content => 6,
            Mood.Stable => 5,
            Mood.Troubled or Mood.Awakened => 4,
            _ => throw new UnreachableException(),
        };
        AfterClean:
        AnsiConsole.MarkupLine($"You will play the dice game with [underline italic blue]{Name}[/] for [green]{activity}[/] with [green]{numDice}[/] dice because you are [underline italic blue]{Mood}[/].");
        var dice = Random.Shared.GetItems([1, 2, 3, 4, 5, 6], numDice);
        AnsiConsole.MarkupLine("your dice: [green]" + Ext.Join("[/], [green]", "[/] and [green]", dice) + "[/]");
        var die1 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>().Title($"Select [blue]1st[/] die").AddChoices(dice.Index()));
        var die2 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>().Title($"Select [blue]2nd[/] die ([green]{die1.Item2}[/])").AddChoices(dice.Index().Except([die1])));
        var reach = die1.Item2 + die2.Item2;
        AnsiConsole.MarkupLine($"sum to reach: [green]{reach}[/]");
        var die3 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>().Title($"Select [blue]3rd[/] die").AddChoices(dice.Index().Except([die1, die2])));
        var die4 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>().Title($"Select [blue]4th[/] die ([green]{die3.Item2}[/])").AddChoices(dice.Index().Except([die1, die2, die3])));
        var success = die3.Item2 + die4.Item2 == reach;
        if (success)
        {
            AnsiConsole.MarkupLine($"You [green bold]succeded[/] with 2 equals pairs: [green]{die1.Item2}[/] + [green]{die2.Item2}[/] = [blue]{reach}[/] = [green]{die3.Item2}[/] + [green]{die4.Item2}[/]");
            switch (activity)
            {
                case Activity.Eat:
                    FoodLevel++;
                    AnsiConsole.MarkupLine($"Food: [blue]{FoodLevel}[/]");
                    break;
                case Activity.Clean:
                    Fog--;
                    AnsiConsole.MarkupLine($"Fog: [blue]{Fog}[/]");
                    if (Fog > 0)
                        goto AfterClean;
                    break;
                case Activity.Play:
                    Plays++;
                    AnsiConsole.MarkupLine($"Plays: [blue]{Plays}[/]");
                    break;
            }
        }
        else
            AnsiConsole.MarkupLine($"You [red bold]failed[/] with 2 differents pairs: [green]{die1.Item2}[/] + [green]{die2.Item2}[/] != [green]{die3.Item2}[/] + [green]{die4.Item2}[/]");
        TimeStamp = DateTime.Now;
    }
}

public enum Mood
{
    Awakened,
    Troubled,
    Stable,
    Content,
}

public enum Activity
{
    Eat,
    Play,
    Clean,
}

file class Ext
{
    public static string Join<T>(string separator, string lastSeparator, params ReadOnlySpan<T> values)
    {
        switch (values)
        {
            case []:
                return "";
            case [var single]:
                return $"{single}";
            case [..var head, var last]:
                return $"{string.Join(separator, [..head])}{lastSeparator}{last}";
        }
    }
}
