using Sandbox;

public class Gun : Component
{
	public GameObject Head;
	public PigInfo pigInfo;
	private GameObject Camera;
	private GameObject Player;
	public PigPlayer pig;
	/// <summary>
	/// The amount of damage the player does with their weapon
	/// </summary>
	public int damage;
	public float fireRate;
	private float nextFire;
	

	protected override void OnUpdate()
	{
		if (nextFire > 0)
		{
			nextFire -= Time.Delta;
		}

		if (Input.Pressed("Attack1") && nextFire <= 0)
		{
			nextFire = 1 / fireRate;

			Fire();
		}
	
	}	

	public void Fire()
	{
			var ray = Scene.Camera.ScreenNormalToRay(0.5f); var tr = Scene.Trace.Ray(ray, 5000)
			.WithoutTags("player", "trigger")
			.Run();

				// something to check if it hits a player
			if (tr.Hit && tr.Hitbox != null)
			{
				Log.Info("Hit a pig" + tr.Hitbox.GameObject.Name);

				if (tr.Hitbox.GameObject.Tags.Has("player"))
				{
					//tr.GameObject.Components.Get<Rpc>()(damage);
						
						var pigInfo = tr.Hitbox.GameObject.Components.Get<PigInfo>();
						
						Log.Info("Hit a player");
				}
			}	
		}
	}
