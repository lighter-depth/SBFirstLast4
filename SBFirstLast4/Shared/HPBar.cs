using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SBFirstLast4.Simulator;

namespace SBFirstLast4;
public class HPBar : ComponentBase
{
	// The current HP value
	[Parameter]
	public int CurrentHP { get; set; }

	// The maximum HP value
	private const int MaxHP = Player.MaxHP;

	// The color of the HP bar when above 50%
	private const string HighHPColor = "#00DB0E";

	// The color of the HP bar when below 50%
	private const string MediumHPColor = "#E1B740";

	// The color of the HP bar when below 20%
	private const string LowHPColor = "#B84731";

	// The color of the consumed HP segment
	private const string ConsumedHPColor = "#626362";

	// The radius of the circle in pixels
	private const int Radius = 50;

	// The stroke width of the circle in pixels
	private const int StrokeWidth = 10;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		base.BuildRenderTree(builder);

		// Calculate the percentage of the current HP value
		var percentage = (double)CurrentHP / MaxHP;

		// Determine the color of the HP bar based on the percentage
		var color = percentage > 0.5 ? HighHPColor : percentage > 0.2 ? MediumHPColor : LowHPColor;

		// Calculate the circumference of the circle
		var circumference = 2 * Math.PI * Radius;

		// Calculate the stroke dash array for the remaining and consumed segments of the circle
		var remainingDash = percentage * circumference;
		var consumedDash = circumference - remainingDash;

		// Render a svg element for the circle container
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "width", Radius * 2);
		builder.AddAttribute(2, "height", Radius * 2);
		builder.AddAttribute(3, "viewBox", $"0 0 {Radius * 2} {Radius * 2}");

		// Render a circle element for the consumed segment of the HP bar
		builder.OpenElement(4, "circle");
		builder.AddAttribute(5, "cx", Radius);
		builder.AddAttribute(6, "cy", Radius);
		builder.AddAttribute(7, "r", Radius - StrokeWidth / 2);
		builder.AddAttribute(8, "fill", "none");
		builder.AddAttribute(9, "stroke", ConsumedHPColor);
		builder.AddAttribute(10, "stroke-width", StrokeWidth);
		builder.CloseElement();

		// Render a circle element for the remaining segment of the HP bar
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

		// Render a text element for the current HP value
		builder.OpenElement(20, "text");
		builder.AddAttribute(21, "x", Radius);
		builder.AddAttribute(22, "y", Radius + StrokeWidth / 4);
		builder.AddAttribute(23, "text-anchor", "middle");
		builder.AddAttribute(24, "font-size", StrokeWidth * 2);
		builder.AddContent(25, $"{CurrentHP}/{MaxHP}");
		builder.CloseElement();

		// Close the svg element for the circle container
		builder.CloseElement();
	}
}
