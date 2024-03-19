using Sandbox;

public class WeaponManage : Component
{
	public WeaponManage Instance { get; private set; }
	public GameObject Gun { get; set; } = new();
	[Property] public PrefabScene Prefabs { get; set; }
	protected override void OnAwake()
	{
		Instance = this;
		
		if (Prefabs.IsValid())
		{
			Prefabs.Clone();
		}


		base.OnAwake();
	}


	protected override void OnDestroy()
	{
		Instance = null;
		base.OnDestroy();
	}









}
