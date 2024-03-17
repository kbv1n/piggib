using System.Drawing;
using Sandbox;

public class CameraMovement : Component
{
	// Movement Properties
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;

	// Member Variables
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;
	public Angles EyeAngles { get; set; }
	public Vector3 EyePosition {get; set;}
	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );


	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}


	protected override void OnUpdate()
	{
		// Set the camera position
		var eyeAngles = Head.Transform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles.ToRotation();

		if(Camera is not null)
		{
			var camPos = Head.Transform.Position;
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();	
		}
	}
}
