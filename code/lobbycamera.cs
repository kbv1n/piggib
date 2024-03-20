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
		camera.Transform.Position.LerpTo(rotatePoint.Transform.Position, 1f);
		camera.Transform.Rotation = rotatePoint.Transform.Rotation;

    }
}
