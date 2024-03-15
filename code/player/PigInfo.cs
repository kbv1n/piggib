using Sandbox;
public enum pigType
{
	None,
	Player,
	Friend,
	Enemy,
}
public class PigInfo : Component
{
	[Property] public pigType Team { get; set; } = pigType.None;
	/// <summary>
	/// Max Health of A Pig/Player, Clamps from 0 to MaxHealth
	/// </summary>
	[Property] [Range(0.1f, 10f, 0.1f)]public float MaxHealth { get; set; } = 5f;
	public float Health { get; private set; }
	public bool Alive { get; private set; } = true;

	protected override void OnUpdate()
	{

	}

	protected override void OnFixedUpdate()
	{

	}

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	public void Damage(float damage)
	{
		if ( !Alive ) return;
		{
			Health -= damage;
			if ( Health <= 0f )
			{
				Death();
			}
		}
	}



/// <summary>
/// Death of A Pig/Player
/// </summary>
	public void Death()
	{
		Health = 0f;
		Alive = false;

		GameObject.Destroy();
	}
}
