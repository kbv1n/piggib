using System.Drawing;
using Sandbox;
public sealed class CameraMovement : Component
{
	// Movement Properties
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;

	// Member Variables
	public bool IsFirstPerson => Distance == 0;
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
			if(!IsFirstPerson)
			{
							// Set the camera position
				var camForward = eyeAngles.ToRotation().Forward;
                var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance))
                    .WithoutTags("player", "trigger")
                    .Run();
				if (camTrace.Hit)
				{
					camPos = camTrace.HitPosition + camTrace.Normal;
				}
				else
				{
					camPos = camTrace.EndPosition;
				}
				Camera.Transform.Position = camPos;
				Camera.Transform.Rotation = eyeAngles.ToRotation();	
			}

		}
	}
}
