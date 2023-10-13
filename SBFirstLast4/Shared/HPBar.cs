using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SBFirstLast4.Simulator;

namespace SBFirstLast4;
public class HPBar : ComponentBase
{
	[Parameter]
	public int CurrentHP { get; set; }

	private const int MaxHP = Player.MaxHP;

	private const string HighHPColor = "#00DB0E";

	private const string MediumHPColor = "#E1B740";

	private const string LowHPColor = "#B84731";

	private const string ConsumedHPColor = "#626362";

	private const int Radius = 50;

	private const int StrokeWidth = 10;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		base.BuildRenderTree(builder);

		var percentage = (double)CurrentHP / MaxHP;

		var color = percentage > 0.5 ? HighHPColor : percentage > 0.2 ? MediumHPColor : LowHPColor;

		var circumference = 2 * Math.PI * Radius;

		var remainingDash = percentage * circumference;
		var consumedDash = circumference - remainingDash;

		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "width", Radius * 2);
		builder.AddAttribute(2, "height", Radius * 2);
		builder.AddAttribute(3, "viewBox", $"0 0 {Radius * 2} {Radius * 2}");

		builder.OpenElement(4, "circle");
		builder.AddAttribute(5, "cx", Radius);
		builder.AddAttribute(6, "cy", Radius);
		builder.AddAttribute(7, "r", Radius - StrokeWidth / 2);
		builder.AddAttribute(8, "fill", "none");
		builder.AddAttribute(9, "stroke", ConsumedHPColor);
		builder.AddAttribute(10, "stroke-width", StrokeWidth);
		builder.CloseElement();

		builder.OpenElement(11, "circle");
		builder.AddAttribute(12, "cx", Radius);
		builder.AddAttribute(13, "cy", Radius);
		builder.AddAttribute(14, "r", Radius - StrokeWidth / 2);
		builder.AddAttribute(15, "fill", "none");
		builder.AddAttribute(16, "stroke", color);
		builder.AddAttribute(17, "stroke-width", StrokeWidth);
		builder.AddAttribute(18, "stroke-dasharray", $"{remainingDash} {consumedDash}");
		builder.AddAttribute(19, "transform", $"rotate(-90 {Radius} {Radius})");
		builder.CloseElement();

		builder.OpenElement(20, "text");
		builder.AddAttribute(21, "x", Radius);
		builder.AddAttribute(22, "y", Radius + StrokeWidth / 4);
		builder.AddAttribute(23, "text-anchor", "middle");
		builder.AddAttribute(24, "font-size", StrokeWidth * 2);
		builder.AddContent(25, $"{CurrentHP}/{MaxHP}");
		builder.CloseElement();

		builder.CloseElement();
	}
}
