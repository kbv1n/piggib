using Sandbox;

public sealed class ViewModel : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	private PlayerMovement PlayerMovement => Components.GetInAncestors<PlayerMovement>();
	private CameraComponent Camera { get; set; }
	private WeaponComponent Weapon { get; set; }

	public void SetWeaponComponent(WeaponComponent weapon)
	{
		Weapon = weapon;
	}
	public void SetCamera(CameraComponent camera)
	{
		Camera = camera;
	}
	protected override void OnStart()
	{
		ModelRenderer.Set( "b_deploy", true );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		ApplyVelocity();
		ApplyStates();
		ApplyAnimationParameters();


		 LerpedLocalRotation = Rotation.Lerp( LerpedLocalRotation, LocalRotation, Time.Delta * 10f );
		 LerpedLocalPosition = LerpedLocalPosition.LerpTo( LocalPosition, Time.Delta * 10f );

		//  Camera.Transform.Position = Scene.Camera.Transform.Position;
		//  Camera.Transform.Rotation = Scene.Camera.Transform.Rotation;
		
		Transform.LocalRotation = LerpedLocalRotation;
		Transform.LocalPosition = LerpedLocalPosition;

	}

private void OnPlayerJumped()
	{
		ModelRenderer.Set( "b_jump", true );
	}
	private Vector3 LerpedWishLook { get; set; }
	private Vector3 LocalPosition { get; set; }
	private Rotation LocalRotation { get; set; }
	private Vector3 LerpedLocalPosition { get; set; }
	private Rotation LerpedLocalRotation { get; set; }

	 private void ApplyVelocity()
	{
		var moveVel = PlayerMovement.CharacterController.Velocity;
		var moveLen = moveVel.Length;
		var wishLook = PlayerMovement.WishVelocity.Normal * 1f;
		ModelRenderer.Set( "move_groundspeed", moveLen );
		
		LerpedWishLook = LerpedWishLook.LerpTo( wishLook, Time.Delta * 5.0f );

		LocalRotation *= Rotation.From( 0, -LerpedWishLook.y * 3f, 0 );
		LocalPosition += -LerpedWishLook;

		ModelRenderer.Set( "move_groundspeed", moveLen );
	}

	private void ApplyStates()
	{
		LocalPosition += Vector3.Backward * 2f;
		LocalRotation *= Rotation.From( 10f, 25f, -5f );
	}

	private void ApplyAnimationParameters()
	{
		ModelRenderer.Set( "b_grounded", PlayerMovement.CharacterController.IsOnGround );
	}
}
