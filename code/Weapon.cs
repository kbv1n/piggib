using Facepunch.Arena;
using Sandbox;
using Sandbox.Citizen;

public abstract class WeaponComponent : Component
{
	// Stat Properties
	[Property] public string DisplayName { get; set; }
	[Property] public float DeployTime { get; set; } = 1f;
	[Property] public float FireRate { get; set; } = 0.1f;
	[Property] public float Damage { get; set; } = 100f;
	[Property] public float DamageForce { get; set; } = 5f;
	[Property] public bool IsDeployed { get; set; }
	private TimeUntil NextFire { get; set; }
	private TimeUntil NextFireTime { get; set; }
	
	// Display Properties
	[Property] public GameObject ViewModelPrefab { get; set; }
	[Property] public ParticleSystem MuzzleFlash { get; set; }
	[Property] public ParticleSystem MuzzleSmoke { get; set; }
	[Property] public ParticleSystem ImpactEffect { get; set; }
	private ViewModel ViewModel { get; set; }
	public bool HasViewModel => ViewModel.IsValid();
	private SkinnedModelRenderer ModelRenderer { get; set; }
	private SkinnedModelRenderer EffectRenderer => ViewModel.IsValid() ? ViewModel.ModelRenderer : ModelRenderer;

	// Sound Properties
	[Property] public SoundEvent DeploySound { get; set; }
	[Property] public SoundEvent FireSound { get; set; }

    // Animation Properties
		[Property] public CitizenAnimationHelper.HoldTypes HoldTypes { get; set; } = CitizenAnimationHelper.HoldTypes.Shotgun;

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
		Blast,
	}
	[Broadcast]
	private void SendAttackMessage( Vector3 startPos, Vector3 endPos, float distance )
	{
		// Missing weapons reaction to firing
		 var tracerStartPosition = startPos;
		var p = new SceneParticles( Scene.SceneWorld, "particles/entity/rope2.vpcf" );
		var muzzle = EffectRenderer.SceneModel.GetAttachment( "muzzle" );

		if ( IsProxy && muzzle.HasValue )
		{
			tracerStartPosition = muzzle.Value.Position;
		}
		
		p.SetControlPoint( 0, tracerStartPosition );
		p.SetControlPoint( 1, endPos );
		p.SetControlPoint( 2, distance );
		p.PlayUntilFinished( Task );

		if ( MuzzleFlash is not null && muzzle.HasValue )
		{
			p = new( Scene.SceneWorld, MuzzleFlash );
			p.SetControlPoint( 0, muzzle.Value );
			p.PlayUntilFinished( Task );
		}
				
		if ( MuzzleSmoke is not null && muzzle.HasValue )
		{
			p = new( Scene.SceneWorld, MuzzleSmoke );
			p.SetControlPoint( 0, muzzle.Value );
			p.PlayUntilFinished( Task );
		}
		
		if ( FireSound is not null )
		{
			Sound.Play( FireSound, startPos );
			
		}
	}	

	public virtual bool DoFire1()
	{
		if ( !NextFireTime ) return false;

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
				animator.TriggerDeploy();
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
		NextFireTime = DeployTime;
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
		 if ( !ViewModelPrefab.IsValid() ) return;
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
		p.PlayUntilFinished( Task );
	}



}
