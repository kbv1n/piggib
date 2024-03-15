using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Reflection.Metadata;
using Sandbox;
using Sandbox.Citizen;
public sealed class PigPlayer : Component
{
	/// <summary>
	/// Set Pig Team to Player
	/// </summary>
	/// <param name="Team">Player</param>
	[Property] public pigType Team { get; set; } = pigType.Player;

	[Property]
	[Category("Shooting")]
	[Description("The amount of damage the player does with their weapon")]
	[Range(0.1f, 10f, 0.1f)]
	public float Damage { get; set; } = 1f;


	protected override void OnUpdate()
	{

	}
}
