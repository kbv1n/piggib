using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Title( "Weapon Container" )]
public sealed class WeaponContainer : Component
{
	[Property] public PrefabScene WeaponPrefab { get; set; }
	[Property] public GameObject WeaponBone { get; set; }

	public WeaponComponent Deployed => Components.GetAll<WeaponComponent>( FindMode.EverythingInSelfAndDescendants ).FirstOrDefault( x => x.IsDeployed );
	public IEnumerable<WeaponComponent> All => Components.GetAll<WeaponComponent>( FindMode.EverythingInSelfAndDescendants );
	public bool HasAny => All.Any();


	public bool Has( GameObject prefab )
	{
		return All.Any ( x => x.GameObject.Name == prefab.Name );
	}


	public void Clear()
	{
		if ( IsProxy ) return;
		foreach ( var weapon in All )
		{
			weapon.GameObject.Destroy();
		}
	}


	public void GiveDefault()
	{
		if ( IsProxy ) return;
		if ( !WeaponPrefab.IsValid() ) return;
		Give( WeaponPrefab, true );
	}


	public void Give( GameObject prefab, bool shouldDeploy = false )
	{
		if ( IsProxy ) return;
		var weaponGo = prefab.Clone();
		var weapon = weaponGo.Components.GetInDescendantsOrSelf<WeaponComponent>( true );

		if ( !weapon.IsValid() )
		{
			weaponGo.DestroyImmediate();
			return;
		}


		if ( shouldDeploy )
		{
			foreach ( var w in All )
			{
				w.Holster();
			}


		}

		weaponGo.SetParent( WeaponBone );
		weaponGo.Transform.Position = WeaponBone.Transform.Position;
		weaponGo.Transform.Rotation = WeaponBone.Transform.Rotation;
		weapon.IsDeployed = !Deployed.IsValid();
		weaponGo.NetworkSpawn();


	}

}	
