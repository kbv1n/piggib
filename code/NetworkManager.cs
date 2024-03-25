using System.Linq;
using Sandbox;
using Sandbox.Network;


public class NetworkManager : Component, Component.INetworkListener
{
	[Property] public PrefabScene RedPlayer { get; set; }
	[Property] public PrefabScene BluePlayer { get; set; }
	public int redTeam { get; set; }
	public int blueTeam { get; set; }
	private PlayerMovement player { get; set; } 

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
		
		base.OnStart();
	}
	
	void INetworkListener.OnActive( Connection connection )
	{
		{		
			 if ( blueTeam <= redTeam )
			 {
				var blue = BluePlayer.Clone();
			 	blueTeam++;
				blue.NetworkSpawn( connection );
				player.currentTeam = "Blue";
				Log.Info( player.currentTeam );
			}
			else
			{
				
				var red = RedPlayer.Clone();
				redTeam++;
				red.NetworkSpawn( connection );
				player.currentTeam = "Red";
			}
		}
	}

	
}
