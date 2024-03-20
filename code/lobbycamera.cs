using System;
using System.Numerics;
using Sandbox;

public sealed class CameraRotation : Component
{
    private Vector3 rotationPoint = new Vector3(832, 1024, 672);
    private float rotationSpeed = 0.01f;
	[Property] public GameObject rotatePoint { get; set; }
	[Property] public GameObject camera { get; set; }

    protected override void OnUpdate()
    {
		if (rotatePoint.IsValid())
		{
			rotationPoint = rotatePoint.Transform.Position;
		}
		if (camera.IsValid())
		{
			camera.Transform.Position = rotationPoint + new Vector3(100, 0, 100);
			camera.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(0, Time.Now * rotationSpeed, 0);
		}
    }
}
