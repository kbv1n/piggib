using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Citizen;
using Sandbox;

// Main problems;
// This should be called playerController its not just movement
// Fix playermodel showing on local player 
// Fix Respawning
// Fix view model jerking
// add Flag capturing mechanics
// Texture the map or get a new one

public class PlayerMovement : Component, Component.ITriggerListener, IHealthComponent
{
	// Component Properties
	[Property] public Vector3 Gravity { get; set; } = new ( 0f, 0f, 800f );
	[Property] public WeaponContainer Weapons { get; set; }
	public CharacterController CharacterController { get; private set; }
	public SkinnedModelRenderer ModelRenderer { get; set; }

    // Object references
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Eye { get; set; }

	// Camera properties
	[Property] public CameraComponent ViewModelCamera { get; set; }
	[Property] public GameObject ViewModelRoot { get; set; }
	[Sync] public Angles EyeAngles { get; private set; }

	// Stat tracking
	[Sync] public int Kills { get; private set; }
	[Sync] public int Deaths { get; private set; }
	[Sync, Property] public float MaxHealth { get; private set; } = 100f;
	[Sync] public LifeState LifeState { get; private set; } = LifeState.Alive;
	[Sync] public float Health { get; private set; } = 100f;
	public RealTimeSince LastHitmarkerTime { get; private set; }
	[Sync] public string currentTeam { get; set; }
	public enum playerTeam { Red, Blue, None }
 	[Sync] public RealTimeSince MatchStart { get; set; } // add a method for ending the match at 15 minutes or max captures
 
	// Sound Properties
	[Property] public SoundEvent HurtSound { get; set; }
	[Property] public SoundEvent DeathSound { get; set; }
	[Property] public SoundEvent Headshot { get; set; }

	// Animation Properties
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] private CitizenAnimationHelper ShadowAnimator { get; set; }
	public List<CitizenAnimationHelper> Animators { get; private set; } = new();

    // Movement Properties
    [Property] public float Friction { get; set; } = 4.0f;
    [Property] public float AirControl { get; set; } = 0.1f;
    [Property] public float MaxForce { get; set; } = 50f;
    [Property] public float Speed { get; set; } = 160f;
    [Property] public float JumpForce { get; set; } = 400f;
	public bool IsSwinging { get; set; }
	public Vector3 WishVelocity { get; private set; }
	public bool dJumpUsed { get; set; }
    public bool IsCrouching = false;
	private bool WantsToCrouch { get; set; }
	private RealTimeSince LastGroundedTime { get; set; }
	private RealTimeSince LastUngroundedTime { get; set; }

	protected override void OnStart()
	{
		Animators.Add( ShadowAnimator );
		Animators.Add( AnimationHelper );
		if ( !IsProxy )
		{
			Respawn();
		}

		if ( IsProxy && ViewModelCamera.IsValid() )
		{
			ViewModelCamera.Enabled = false;
		}

		base.OnStart();
	}
	protected override void OnPreRender()
	{
		
		if ( !Scene.IsValid() || !Scene.Camera.IsValid() )
			return;

		UpdateModelVis();

		if ( IsProxy )
			return;
		if ( !Eye.IsValid() )
			return;

		var idealEyePos = Eye.Transform.Position;
		var headPos = Transform.Position + Vector3.Up * CharacterController.Height;
		var headTrace = Scene.Trace.Ray( Transform.Position, headPos)
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Run();


		headPos = headTrace.EndPosition - headTrace.Direction * 2f;
		var trace = Scene.Trace.Ray( headPos, idealEyePos )
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Radius( 2f )
			.Run();
		
		var deployedWeapon = Weapons.Deployed;
		var HasViewModel = deployedWeapon.IsValid() && deployedWeapon.HasViewModel;

		if ( HasViewModel )
		   Scene.Camera.Transform.Position = Head.Transform.Position;
		else
		   Scene.Camera.Transform.Position = trace.Hit ? trace.EndPosition : idealEyePos;

		   Scene.Camera.Transform.Rotation = EyeAngles.ToRotation() * Rotation.FromPitch( -10f );
	}
	protected override void OnUpdate()
    {
        //set spritning and crouching
		if (LifeState == LifeState.Dead) return;
		if ( !IsProxy )
		{
        	IsCrouching = Input.Down("Duck");
        	if(Input.Pressed("Jump")) Jump();
			if(Input.Down("Use")) Rope();
			var angles = EyeAngles.Normal;
			angles += Input.AnalogLook * 0.5f;
			angles.pitch = angles.pitch.Clamp( -60f, 80f );
			EyeAngles = angles.WithRoll( 0f );

		}

		var weapon = Weapons.Deployed;

			foreach ( var animator in Animators )
		{
			animator.HoldType = weapon.IsValid() ? weapon.HoldTypes : CitizenAnimationHelper.HoldTypes.None;
			animator.WithVelocity( CharacterController.Velocity );
			animator.WithWishVelocity( WishVelocity );
			animator.IsGrounded = CharacterController.IsOnGround;
			animator.FootShuffle = 0f;
			animator.DuckLevel = IsCrouching ? 1f : 0f;
			animator.WithLook( EyeAngles.Forward );
			animator.MoveStyle =  (!IsCrouching ) ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}

    }
    protected override void OnFixedUpdate()
    {
		if ( IsProxy ) return;
		if ( LifeState == LifeState.Dead ) return;
		
		DoMovementInput();

		var weapon = Weapons.Deployed;
		if ( !weapon.IsValid() ) return;

		if ( Input.Down( "Use") )
		{
			// shoot rope
		}

		if ( Input.Down( "Attack1" ))
		{
			if ( weapon.DoFire1() )
			{
				SendAttackMessage();
			}	
		}
	}
    protected override void OnAwake()
    {
		base.OnAwake();
		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );
		CharacterController = Components.GetInDescendantsOrSelf<CharacterController>( true );
		CharacterController.IgnoreLayers.Add( "player" );

		if ( IsProxy ) return;
		ResetViewAngles();
    }

// LifeState Health and Related Methods
	protected virtual void OnKilled( GameObject attacker )
	{
		if ( attacker.IsValid() )
		{
			var player = attacker.Components.GetInAncestorsOrSelf<PlayerMovement>();

			if (player.IsValid())
			{
				var chat = Scene.GetAllComponents<Chat>().FirstOrDefault();
				if ( chat.IsValid())
					chat.AddTextLocal($"{player.Network.OwnerConnection.DisplayName}", $" killed {Network.OwnerConnection.DisplayName}");

				if (!player.IsProxy)
				{
					// SceneUser killed network user
					player.Kills++;
				}
			}
		}
		if ( IsProxy ) return;
			RespawnAsync( 1f );
			Deaths++;
	}
	public async void RespawnAsync( float seconds ) // respawning doesnt work
	{
		if ( IsProxy ) return;
		await Task.DelaySeconds( seconds );
		Respawn();
	}
	public void Respawn()
	{
		if ( IsProxy ) return;
		Weapons.Clear();
		Weapons.GiveDefault();
		MoveToSpawnPoint();
		LifeState = LifeState.Alive;
		Health = MaxHealth;
	
	}
	private void MoveToSpawnPoint()
	{
		if ( IsProxy ) return;
		var spawnpoints = Scene.GetAllComponents<SpawnPoint>();
		var randomSpawnpoint = Game.Random.FromList( spawnpoints.ToList() );
		Transform.Position = randomSpawnpoint.Transform.Position;
		Transform.Rotation = Rotation.FromYaw( randomSpawnpoint.Transform.Rotation.Yaw() );
		EyeAngles = Transform.Rotation;
	}
	[Broadcast] private void SendKilledMessage( Guid attackerId )
	{
		var attacker = Scene.Directory.FindByGuid( attackerId );
		OnKilled( attacker );
	}
	[Broadcast] private void SendAttackMessage()
	{
		foreach ( var animator in Animators )
		{
		
		var renderer = animator.Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		renderer?.Set( "b_attack", true );
		}
	}
	[Broadcast] public void TakeDamage( DamageType type, float damage, Vector3 position, Vector3 force, Guid attackerId )
	{
		if ( LifeState == LifeState.Dead )
			return;
		
		 if ( type == DamageType.Beam && HurtSound is not null) // Create a prefab for a gib effect and call it here this is an insta kill
		{	
			Sound.Play( HurtSound, Transform.Position );
		}
  	// if ( type == DamageType.Blast )
   	//	{
	//		calculate damage based on distance from explosion vector 
 	// 		clamped range between 5 - 25 damage 
  	// 		play hurtsound 
    // 		vector3.punch player in direction opposite of blast area
	//	}
 	// 		I guess add a melee too for shits and giggles
		
		if ( IsProxy )
			return;
			
		Health = MathF.Max( Health - damage, 0f );
		
		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			SendKilledMessage( attackerId );
			Sound.Play( DeathSound );
		}
	}
	public void DoHitMarker( bool isHeadshot )
	{
		Sound.Play( isHeadshot ? "hitmarker.headshot" : "hitmarker.hit" ); //get this working
		LastHitmarkerTime = 0f;
	}	

	// Trigger Methods
	private void UpdateModelVis()
	{
		if ( !ModelRenderer.IsValid() )
			return;
		
		var deployedWeapon = Weapons.Deployed;
		var shadowRenderer = ShadowAnimator.Components.Get<SkinnedModelRenderer>( true );
		var hasViewModel = deployedWeapon.IsValid() && deployedWeapon.HasViewModel;
		// var clothing = ModelRenderer.Components.GetAll<ClothingComponent>( FindMode.EverythingInSelfAndDescendants );
		
		if ( hasViewModel )
		{
			shadowRenderer.Enabled = false;
			ModelRenderer.Enabled = false;
			ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			
			// foreach ( var c in clothing )
			// {
			// 	c.ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			// }

			return;
		}
			
		ModelRenderer.SetBodyGroup( "head", IsProxy ? 0 : 1 );
		ModelRenderer.Enabled = true;
		if ( !IsProxy )
		{
			ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			shadowRenderer.Enabled = false;
		}
		else
		{
			ModelRenderer.RenderType = IsProxy
				? Sandbox.ModelRenderer.ShadowRenderType.On
				: Sandbox.ModelRenderer.ShadowRenderType.Off;

			shadowRenderer.Enabled = true;
		}


		shadowRenderer.Enabled = true;

		// foreach ( var c in clothing )
		// {
		// 	c.ModelRenderer.Enabled = true;

		// 	if ( c.Category is Clothing.ClothingCategory.Hair or Clothing.ClothingCategory.Facial or Clothing.ClothingCategory.Hat )
		// 	{
		// 		c.ModelRenderer.RenderType = IsProxy ? Sandbox.ModelRenderer.ShadowRenderType.On : Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
		// 	}
		// }
	}
	public void ResetViewAngles()
	{
		var rotation = Rotation.Identity;
		EyeAngles = rotation.Angles().WithRoll( 0f );
	}

	// Movement Methods
	private void BuildWishVelocity()
	{
		var rotation = EyeAngles.ToRotation();

		WishVelocity = rotation * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0f );

		if ( !WishVelocity.IsNearZeroLength )
			WishVelocity = WishVelocity.Normal;

		if ( IsCrouching )
			WishVelocity *= 350f;
		else
			WishVelocity *= 430;
	}
	public void DoubleJump()
	{
		
		if ( dJumpUsed == true ) return;
		
		else
		{
			CharacterController.Punch( Vector3.Up * 600f );
			dJumpUsed = true;
		}
	}
	protected virtual void DoMovementInput()
	{
		BuildWishVelocity();
		if ( CharacterController.IsOnGround && Input.Down( "Jump" ) )
		{
			CharacterController.Punch( Vector3.Up * 350f  );
			dJumpUsed = false;
		}
		if ( !CharacterController.IsOnGround && Input.Pressed( "Jump" ) && LastGroundedTime > 0.1f) 
		{
			DoubleJump();
		}

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0f );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( 4.0f );
			LastGroundedTime = 0f;
		}
		else
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.6f;
			CharacterController.Accelerate( WishVelocity.ClampLength( 50f ) );
			CharacterController.ApplyFriction( 0.3f );
			LastUngroundedTime = 0f;
		}

		CharacterController.Move();
		Transform.Rotation = Rotation.FromYaw( EyeAngles.ToRotation().Yaw() );
	}
	protected void Jump()
	{
		if(!CharacterController.IsOnGround) return;

		CharacterController.Punch(Vector3.Up * JumpForce);
		if ( !IsProxy ) 
		AnimationHelper?.TriggerJump();
	}
	protected void Rope()
	{
		// Rope logic


	}
	protected void StartSwing()
	{ 
		
	}
	protected void EndSwing()
	{
		IsSwinging = false;
	}

}

