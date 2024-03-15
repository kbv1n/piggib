using System.Drawing;
using Sandbox;
public sealed class CameraMovement : Component
{
	// Movement Properties
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;
	[Property] public GameObject Ball { get; set; }
	[Property] public GameObject rlStart { get; set; }
	[Property] public GameObject rlEnd { get; set; }

	// Member Variables
	public bool IsFirstPerson => Distance == 0;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;
	public PigInfo PigType;
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
		var eyeAngles = Head.Transform.Rotation.Angles();
        eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
        eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
        eyeAngles.roll = 0f;
        eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
        Head.Transform.Rotation = eyeAngles.ToRotation();
		shootRay();
		
		// Set the camera position
		if(Camera is not null)
		{
			var camPos = Head.Transform.Position;
			if(!IsFirstPerson)
			{
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

			}
			// Set the camera position
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();	
			}
	}

	public void shootRay()
	{
		Angles eyeAngles = Head.Transform.Rotation.Angles(); // Declare the eyeAngles variable
		if (Input.Pressed("Attack1"))
		{
			var ray = Scene.Camera.ScreenNormalToRay(0.5f); var tr = Scene.Trace.Ray(ray, 5000)
			.WithoutTags("player", "trigger")
			.Run();
			// something to check if it hits a player
			if (tr.Hit && tr.Hitbox != null)
			{
				Log.Info("Hit a pig" + tr.Hitbox.GameObject.Name);
			}
			Ball.Transform.Position = tr.HitPosition;
			//rlStart.Transform.Position = ray.StartPosition;
			//rlEnd.Transform.Position = ray.HitPosition;
		}
		
	}
}
