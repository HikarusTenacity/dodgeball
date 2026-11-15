using Godot;
using System.Text;

public partial class UIOverlay : CanvasLayer
{
	private Player Player { get; set; }
	[Export] public Label StatsLabel { get; set; }

	public override void _Ready() {
		Player ??= GetParent<Player>();
		StatsLabel ??= GetNode<Label>("UIContainer/StatsLabel");
	}

	public override void _Process(double delta) {
		if (Player == null || StatsLabel == null)
			return;

		Vector3 vel = Player.Velocity;
		float totalSpeed = vel.Length();
		float horizontalSpeed = new Vector3(vel.X, 0, vel.Z).Length();
		int x = (int) Player.GlobalPosition.X;
		int y = (int) Player.GlobalPosition.Y;
		int z = (int) Player.GlobalPosition.Z;
		
		var sb = new StringBuilder();
        sb.AppendLine($"pos:      {x}, {y}, {z}");
		sb.AppendLine($"speed:    {totalSpeed:F2} m/s");
		sb.AppendLine($"speed (h):{horizontalSpeed:F2} m/s");
		sb.AppendLine($"speed (v):{vel.Y:F2} m/s");

		StatsLabel.Text = sb.ToString();
	}
}
