using System;
using System.Collections.Generic;
using System.Linq;
using pig;
using Sandbox;
using Sandbox.Citizen;
using Sandbox;
using Facepunch.Arena;

namespace pig;

// this should be called playerController or something because its not just movement but im retarded

public class PlayerMovement : Component, Component.ITriggerListener
{


	[Property] public Vector3 Gravity { get; set; } = new ( 0f, 0f, 800f );

	public CharacterController CharacterController { get; set; }
	public SkinnedModelRenderer ModelRenderer { get; set; }
	public RealTimeSince LastHitmarkerTime { get; private set; }
	public Vector3 WishVelocity { get; private set; }
	public List<CitizenAnimationHelper> Animators { get; private set; } = new();
	[Property] private CitizenAnimationHelper ShadowAnimator { get; set; }
	[Property] public WeaponContainer Weapons { get; set; }

	[Property] public CameraComponent ViewModelCamera { get; set; }
	[Property] public GameObject ViewModelRoot { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] public SoundEvent HurtSound { get; set; }
	[Sync, Property] public float MaxHealth { get; private set; } = 100f;
	[Sync] public LifeState LifeState { get; private set; } = LifeState.Alive;
	[Sync] public float Health { get; private set; } = 100f;
	[Sync] public int Kills { get; private set; }
	[Sync] public int Deaths { get; private set; }
	[Sync] public Angles EyeAngles { get; private set; }
	private RealTimeSince TimeSinceDamaged { get; set; }
	private RealTimeSince LastGroundedTime { get; set; }
	private RealTimeSince LastUngroundedTime { get; set; }
	private bool WantsToCrouch { get; set; }

    // Movement Properties
    [Property] public float GroundControl { get; set; } = 4.0f;
    [Property] public float AirControl { get; set; } = 0.1f;
    [Property] public float MaxForce { get; set; } = 50f;
    [Property] public float Speed { get; set; } = 160f;
    [Property] public float RunSpeed { get; set; } = 290f;
    [Property] public float WalkSpeed { get; set; } = 90f;
    [Property] public float JumpForce { get; set; } = 400f;
	[Property] public float DuckLevel { get; set; } = 1f;

    // Object references
    
    [Property] public GameObject Body { get; set; }
	[Property] public GameObject Head { get; set; }
	public GameObject Camera { get; set; }




    // Member Variables
    public bool IsCrouching = false;
    public bool IsSprinting = false;
    private CharacterController characterController;
    private CitizenAnimationHelper animationHelper;
    // Component Methods



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

		if ( !IsProxy )
		    return;

			RespawnAsync( 3f );
			Deaths++;
	
	}


	[Broadcast]
	public async void RespawnAsync( float seconds )
	{
		if ( IsProxy ) return;

		await Task.DelaySeconds( seconds );
		Respawn();
	}

	[Broadcast]
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

    protected override void OnAwake()
    {
		base.OnAwake();
		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );
		CharacterController = Components.GetInDescendantsOrSelf<CharacterController>( true );
		CharacterController.IgnoreLayers.Add( "player" );
	 
	    characterController = Components.Get<CharacterController>();
        animationHelper = Components.Get<CitizenAnimationHelper>();

		if ( IsProxy )
		    return;

		ResetViewAngles();

    }
	public void ResetViewAngles()
	{
		var rotation = Rotation.Identity;
		EyeAngles = rotation.Angles().WithRoll( 0f );
	}

	[Broadcast]
	private void SendKilledMessage( Guid attackerId )
	{
		var attacker = Scene.Directory.FindByGuid( attackerId );
		OnKilled( attacker );
	}


	[Broadcast]
	private void SendAttackMessage()
	{
		foreach ( var animator in Animators )
		{
		
		var renderer = animator.Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		renderer?.Set( "b_attack", true );
		}
	}

	[Broadcast]
	public void TakeDamage( DamageType type, float damage, Vector3 position, Vector3 force, Guid attackerId )
	{
		if ( LifeState == LifeState.Dead )
			return;
		
		 if ( type == DamageType.Beam && HurtSound is not null)
		{	
			Sound.Play( HurtSound, Transform.Position );
			
		}
		
		if ( IsProxy )
			return;

		TimeSinceDamaged = 0f;
		Health = MathF.Max( Health - damage, 0f );
		
		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			SendKilledMessage( attackerId );
		}
	}

	public void DoHitMarker( bool isHeadshot )
	{
		Sound.Play( isHeadshot ? "hitmarker.headshot" : "hitmarker.hit" );
		LastHitmarkerTime = 0f;
	}	
	protected override void OnStart()
	{
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

	private void UpdateModelVis()
	{
		if ( !ModelRenderer.IsValid() )
			return;

		var deployedWeapon = Weapons.Deployed;
		var shadowRenderer = ShadowAnimator.Components.Get<SkinnedModelRenderer>(true);
		var HasViewModel = deployedWeapon.IsValid() && deployedWeapon.HasViewModel;
		var clothing = ModelRenderer.Components.GetAll<ClothingContainer>( FindMode.EnabledInSelfAndDescendants );
		
		ModelRenderer.SetBodyGroup( "head", IsProxy ? 0 : 1 );	
		ModelRenderer.Enabled = true;
	}


	protected override void OnPreRender()
	{
		base.OnPreRender();
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
	}




	protected override void OnUpdate()
    {
        //set spritning and crouching
        IsCrouching = Input.Down("Duck");
        IsSprinting = Input.Down("Sprint");
        if(Input.Pressed("Jump")) Jump();
        RotateBody();
    }
    protected override void OnFixedUpdate()
    {
		if ( IsProxy ) return;
		if ( LifeState == LifeState.Dead ) return;
        BuildWishVelocity();
        Move();

		var weapon = Weapons.Deployed;
		if ( !weapon.IsValid() ) return;

		if ( Input.Down( "Attack1" ) )
		{
			if ( weapon.DoFire1() )
			{
				SendAttackMessage();
			}	
		}
	}



    void BuildWishVelocity()
    {
        WishVelocity = 0;
        var rot = Head.Transform.Rotation;
        if (Input.Down("Forward")) WishVelocity += rot.Forward;
        if (Input.Down("Backward")) WishVelocity += rot.Backward;
        if (Input.Down("Left")) WishVelocity -= rot.Right;
        if (Input.Down("Right")) WishVelocity += rot.Right;

        WishVelocity = WishVelocity.WithZ(0);
        if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

        if (IsCrouching) WishVelocity *= WalkSpeed;
        else if (IsSprinting) WishVelocity *= RunSpeed;
        else WishVelocity *= Speed;
    }
    void Move()
    {
        //Gravity From Scene
        var gravity = Scene.PhysicsWorld.Gravity;

        if (characterController.IsOnGround)
        {
            characterController.Velocity = characterController.Velocity.WithZ(0);
            characterController.Accelerate(WishVelocity);
            characterController.ApplyFriction(GroundControl);
        }
        else
        {
            characterController.Velocity += gravity * Time.Delta * 0.5f;
            characterController.Accelerate(WishVelocity.ClampLength(MaxForce));
            characterController.ApplyFriction(AirControl);
        }

        characterController.Move();

        if(!characterController.IsOnGround)
        {
            characterController.Velocity += gravity * Time.Delta * 0.5f;
        }
        else
        {
            characterController.Velocity = characterController.Velocity.WithZ(0);
        }
    }
	void RotateBody()
	{
	if(Body is null) return;

	var targetAngle = new Angles(0, Head.Transform.Rotation.Yaw(), 0).ToRotation();
	float rotateDifference = Body.Transform.Rotation.Distance(targetAngle);

	// Lerp body rotation if we're moving or rotating far enough
	if(rotateDifference > 0f || characterController.Velocity.Length > 0f)
		{
		Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 100f);
		}
	}
    void Jump()
    {
        if(!characterController.IsOnGround) return;

        characterController.Punch(Vector3.Up * JumpForce);
        animationHelper?.TriggerJump();
	}
}
