using System;
using pig;
using Sandbox;

public interface IHealthComponent
{
	public LifeState LifeState { get; }
	public float MaxHealth { get; }
	public float Health { get; }
	public void TakeDamage( DamageInfo type, float damage, Vector3 position, Vector3 force, Guid attackerId );
}
