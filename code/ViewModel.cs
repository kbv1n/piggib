using Sandbox;
namespace pig;
public sealed class ViewModel : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	private PlayerMovement playerMovement => Components.GetInAncestors<PlayerMovement>();
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
		Camera.Transform.Position = Scene.Camera.Transform.Position;
		Camera.Transform.Rotation = Scene.Camera.Transform.Rotation;
		
	}














}
