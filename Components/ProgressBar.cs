using Spectre.Console;
using Spectre.Console.Rendering;

namespace BrainInAJar.Components;

public class ProgressBar(float value = 0, float maxValue = 100) : IRenderable
{
    public float Value { get; set; } = value;
    public float MaxValue { get; set; } = maxValue;
    public float Percent => Value / MaxValue;
    public Style CompletedStyle { get; set; } = Style.Plain with { Foreground = Color.Green };
    public Style CarretStyle { get; set; } = Style.Plain;
    public Style RemainingStyle { get; set; } = Style.Plain with { Foreground = Color.Orange1 };
    public char CompletedChar { get; set; } = '-';
    public string CarretString { get; set; } = "|";
    public char RemainingChar { get; set; } = '-';

    public Measurement Measure(RenderOptions options, int maxWidth)
    => new(CarretString.Length + 1, Math.Max(CarretString.Length + 1, maxWidth));

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var carretSize = CarretString.Length;
        var beforeSize = (int)((maxWidth - carretSize) * Math.Clamp(Percent, 0, 1));
        var afterSize = maxWidth - carretSize - beforeSize;
        yield return new(new(CompletedChar, beforeSize), CompletedStyle);
        yield return new(CarretString, CarretStyle);
        yield return new(new(RemainingChar, afterSize), RemainingStyle);
    }
}
