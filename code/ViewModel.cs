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
		base.OnStart();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		if (Weapon.IsValid())
		{
			ModelRenderer.Enabled = Weapon.IsDeployed;
		}
		else
		{
			ModelRenderer.Enabled = false;
		}

		Camera.Transform.Position = Scene.Camera.Transform.Position;
		Camera.Transform.Rotation = Scene.Camera.Transform.Rotation;

	}


	private Vector3 LerpedWishLook { get; set; }
}
