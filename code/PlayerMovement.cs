using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Reflection.Metadata;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using Sandbox.UI.Tests;


public sealed class PlayerMovement : Component
{

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

    protected override void OnAwake()
    {
        characterController = Components.Get<CharacterController>();
        animationHelper = Components.Get<CitizenAnimationHelper>();
    }



    protected override void OnUpdate()
    {

        
        //set spritning and crouching
        IsCrouching = Input.Down("Duck");
        IsSprinting = Input.Down("Sprint");
        if(Input.Pressed("Jump")) Jump();
        RotateBody();
    	UpdateAnimations();
    }
    protected override void OnFixedUpdate()
    {
		
        BuildWishVelocity();
        Move();
    }

    // Movement Methods
    Vector3 WishVelocity = Vector3.Zero;
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
    void UpdateAnimations()
    {
        if(animationHelper is null) return;

        animationHelper.WithWishVelocity(WishVelocity);
        animationHelper.WithVelocity(characterController.Velocity);
        animationHelper.AimAngle = Head.Transform.Rotation;
        animationHelper.IsGrounded = characterController.IsOnGround;
        animationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
        animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
        animationHelper.DuckLevel = IsCrouching ? 0.55f : 0f;
    }

}
