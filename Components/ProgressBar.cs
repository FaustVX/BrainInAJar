using Spectre.Console;
using Spectre.Console.Rendering;

namespace BrainInAJar.Components;

public class ProgressBar(float value = 0, float maxValue = 100) : IRenderable
{
    public float Value { get; set; } = value;
    public float MaxValue { get; set; } = maxValue;
    public float Percent => Value <= 0 ? 0f:
        Value >= MaxValue ? 1f:
        Value / MaxValue;
    public Style CompletedStyle { get; set; } = Style.Plain with { Foreground = Color.Green };
    public Style CarretStyle { get; set; } = Style.Plain;
    public Style RemainingStyle { get; set; } = Style.Plain with { Foreground = Color.Orange1 };
    public char CompletedChar { get; set; } = '─';
    public string? NonStartedCarretMarkup { get; set; }
    public string CarretMarkup { get; set; } = "|";
    public string? CompletedCarretMarkup { get; set; }
    public char RemainingChar { get; set; } = '─';

    public Measurement Measure(RenderOptions options, int maxWidth)
    => new(new Markup(GetCarretMarkup(), CarretStyle).Length + 1, Math.Max(new Markup(GetCarretMarkup(), CarretStyle).Length + 1, maxWidth));

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var carretSize = new Markup(GetCarretMarkup(), CarretStyle).Length;
        var beforeSize = (int)((maxWidth - carretSize) * Percent);
        var afterSize = maxWidth - carretSize - beforeSize;
        yield return new(new(CompletedChar, beforeSize), CompletedStyle);
        foreach (var seg in new Markup(GetCarretMarkup(), CarretStyle).GetSegments(AnsiConsole.Console))
            yield return seg;
        yield return new(new(RemainingChar, afterSize), RemainingStyle);
    }

    private string GetCarretMarkup()
    => (Value < 0 ? NonStartedCarretMarkup :
       Value > MaxValue ? CompletedCarretMarkup :
       CarretMarkup) ?? CarretMarkup;
}
