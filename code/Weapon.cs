
using Sandbox;
using Sandbox.Citizen;

public abstract class WeaponComponent : Component
{
	[Property] public float DeployTime { get; set; } = 0.5f;
	[Property] public float FireRate { get; set; } = 2f;
	[Property] public float Damage { get; set; } = 100f;
	[Property] public float DamageForce { get; set; } = 5f;
	[Property] public GameObject ViewModelPrefab { get; set; }
	[Property] public CitizenAnimationHelper.HoldTypes HoldTypes { get; set; } = CitizenAnimationHelper.HoldTypes.Rifle;
	[Property] public SoundEvent FireSound { get; set; }
	[Property] public ParticleSystem MuzzleFlash { get; set; }
	[Property] public ParticleSystem ImpactEffect { get; set; }
	[Property] public SoundEvent DeploySound { get; set; }
	[Property] public bool IsDeployed { get; set; }
	private ViewModel ViewModel { get; set; }
	public bool HasViewModel => ViewModel.IsValid();
	private SkinnedModelRenderer ModelRenderer { get; set; }
	private TimeUntil NextFire { get; set; }
	private TimeUntil NextFireTime { get; set; }


	
	// ADD REFERENCE TO VIEWMODEL
	private SkinnedModelRenderer EffectRenderer => ViewModel.IsValid() ? ViewModel.ModelRenderer : ModelRenderer;


	[Broadcast]
	public void Deploy()
	{
		if ( !IsDeployed )
		{
			IsDeployed = true;
			OnDeployed();
		}
	}

	[Broadcast]
	public void Holster()
	{
		if ( IsDeployed )
		{
			IsDeployed = false;
			OnHolstered();
		}
	}
	public enum DamageType
	{
		Beam,
		Explosion,
	}
	[Broadcast]
	private void SendAttackMessage( Vector3 startPos, Vector3 endPos, float distance )
	{
		// Missing weapons reaction to firing
		// var tracerStartPosition = startPos;
		// var muzzle = EffectRenderer.SceneModel.GetAttachment( "muzzle" );

		if ( FireSound is not null )
		{
			Sound.Play( FireSound, startPos );
		}

	}	
	public virtual bool DoFire1()
	{
		if ( !NextFire ) return false;

		var player = Components.GetInAncestors<PlayerMovement>();
		var attachment = EffectRenderer.GetAttachment( "muzzle" );
		var startPos = Scene.Camera.Transform.Position;
		var direction = Scene.Camera.Transform.Rotation.Forward;
		var endPos = startPos + direction * 10000f;
		var trace = Scene.Trace.Ray( startPos, endPos )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.UsePhysicsWorld()
			.UseHitboxes()
			.Run();

		var damage = Damage;
		var origin = attachment?.Position ?? startPos;
		SendAttackMessage( origin, trace.EndPosition, trace.Distance);

		IHealthComponent damageable = null;
		if ( trace.Component.IsValid() )
			damageable = trace.Component.Components.GetInAncestorsOrSelf<IHealthComponent>();

		if ( damageable is not null )
		{
			if ( trace.Hitbox is not null && trace.Hitbox.Tags.Has( "head" ) )
			{
					damage *= 3f;
			}
			else
			{
				player.DoHitMarker( false );
			}
		
			damageable.TakeDamage( global::DamageType.Beam, damage, trace.EndPosition, trace.Direction * DamageForce, GameObject.Id );
		}
		else if ( trace.Hit )
		{
			SendImpactMessage( trace.EndPosition, trace.Normal );
		}

		NextFireTime = 1f / FireRate;


		return true;
	}

	protected override void OnStart()
	{
		if ( IsDeployed )
			OnDeployed();
		else
			OnHolstered();
			
		base.OnStart();
	}

	protected virtual void OnDeployed()
	{
		var player = Components.GetInAncestors<PlayerMovement>();

		if ( player.IsValid() )
		{
			foreach ( var animator in player.Animators )
			{
				animator.TriggerDeploy( );
			}
		}

		ModelRenderer.Enabled = !HasViewModel;

		if ( DeploySound is not null )
		{
			Sound.Play( DeploySound, Transform.Position );
		}
		if ( !IsProxy )
		{
			CreateViewModel();
		}
		NextFire = DeployTime;
	}

	protected virtual void OnHolstered()
	{
		ModelRenderer.Enabled = false;
		DestroyViewModel();
	}


	protected override void OnAwake()
	{
		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );
		base.OnAwake(); 
	}

	protected override void OnDestroy()
	{
		if ( IsDeployed )
		{	OnHolstered();	
			IsDeployed = false;
		}
	
		base.OnDestroy();

	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

private void DestroyViewModel()
	{
		ViewModel?.GameObject.Destroy();
		ViewModel = null;
	}

	private void CreateViewModel()
	{
		// if ( !ViewModelPrefab.IsValid() )
		// 	return;

			var player = Components.GetInAncestors<PlayerMovement>();
			var viewModelGameObject = ViewModelPrefab.Clone();
			viewModelGameObject.Flags |= GameObjectFlags.NotNetworked;
			viewModelGameObject.SetParent( player.ViewModelRoot, false );
			ViewModel = viewModelGameObject.Components.Get<ViewModel>();
			ViewModel.SetWeaponComponent( this );
			ViewModel.SetCamera( player.ViewModelCamera );
			ModelRenderer.Enabled = false;
	}
	[Broadcast]
	private void SendImpactMessage( Vector3 position, Vector3 normal )
	{
		if ( ImpactEffect is null ) return;
		var p = new SceneParticles( Scene.SceneWorld, ImpactEffect );
		p.SetControlPoint( 0, position );
		p.SetControlPoint( 0, Rotation.LookAt( normal ) );
		// p.PlayUntilFinished( Task );
	}



}
