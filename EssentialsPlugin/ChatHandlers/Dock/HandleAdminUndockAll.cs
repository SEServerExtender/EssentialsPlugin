namespace EssentialsPlugin.ChatHandlers.Dock
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EssentialsPlugin.Utility;
    using Microsoft.Xml.Serialization.GeneratedAssembly;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Replication;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Entity;
    using VRage;
    using VRage.Game;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    public class HandleAdminUndockAll : ChatHandlerBase
	{
		// We need to only allow one dock at a time right now until I can address this.  It's causing duping when dropped packets occur

		public override string GetHelp()
		{
			return "This command allows you to undock all ships from all docks.  It tries to find a safe spot for the ship.  Usage: /admin undock all";
		}

		public override string GetCommandText()
		{
			return "/admin undock all";
		}

        public override Communication.ServerDialogItem GetHelpDialog( )
        {
            Communication.ServerDialogItem DialogItem = new Communication.ServerDialogItem( );
            DialogItem.title = "Help";
            DialogItem.header = "";
            DialogItem.content = GetHelp( );
            DialogItem.buttonText = "close";
            return DialogItem;
        }

        public override bool IsAdminCommand()
		{
			return true;
		}

		public override bool AllowedInConsole()
		{
			return true;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			lock (Docking.Instance)
			{
				foreach (DockingItem dockingItem in Docking.Instance.DockingItems)
				{
					string dockedShipFileName = Essentials.PluginPath + String.Format("\\Docking\\docked_{0}_{1}_{2}.sbc", dockingItem.PlayerId, dockingItem.TargetEntityId, dockingItem.DockedEntityId);

					DockingItem dockedShip = dockingItem;
					IMyEntity entity = MyAPIGateway.Entities.GetEntity(x => x.EntityId == dockedShip.TargetEntityId && x is IMyCubeGrid);
					if (entity == null)
					{
						//Communication.SendPrivateInformation(userId, string.Format("Unable to undock ship due to error."));
						Log.Info(string.Format("Unable to find parent '{0}' for '{1}' - '{2}'", dockingItem.TargetEntityId, dockingItem.DockedEntityId, dockingItem.DockedName));
						//continue;
					}

					if (!File.Exists(dockedShipFileName))
					{
						Log.Info(string.Format("Unable to find ship file: {0}", dockedShipFileName));
						continue;
					}

//					FileInfo fileInfo = new FileInfo(dockedShipFileName);
					MyObjectBuilder_CubeGrid cubeGrid;
					MyObjectBuilderSerializer.DeserializeXML( dockedShipFileName, out cubeGrid );

					if (entity != null)
					{
						// Rotate our ship relative to our saved rotation and the new carrier rotation                
						cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(Matrix.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(entity.Physics.GetWorldMatrix().GetOrientation()) * dockingItem.SaveQuat).GetOrientation());
						// Move our ship relative to the new carrier position and orientation
						Quaternion newQuat = Quaternion.CreateFromRotationMatrix(entity.Physics.GetWorldMatrix().GetOrientation());
						Vector3D rotatedPos = Vector3D.Transform(dockingItem.SavePos, newQuat);
						//cubeGrid.Position = rotatedPos + parent.GetPosition();				
						cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(MathUtility.RandomPositionFromPoint(entity.GetPosition(), 250f), cubeGrid.PositionAndOrientation.Value.Forward, cubeGrid.PositionAndOrientation.Value.Up);
					}
					else
					{
						cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(MathUtility.RandomPositionFromPoint(cubeGrid.PositionAndOrientation.Value.Position, 500f), cubeGrid.PositionAndOrientation.Value.Forward, cubeGrid.PositionAndOrientation.Value.Up);
					}

					// Add object to world
					cubeGrid.EntityId = BaseEntity.GenerateEntityId();
					cubeGrid.LinearVelocity = Vector3.Zero;
					cubeGrid.AngularVelocity = Vector3.Zero;

					bool undock = false;
					Wrapper.GameAction(() =>
					{
						try
						{
							IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cubeGrid);
                            MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( newEntity ) );
                            undock = true;
						}
						catch (Exception ex)
						{
							Log.Info(string.Format("Error undocking ship: {0}", ex.ToString()));
							Communication.SendPrivateInformation(userId, string.Format("Unable to undock ship due to error."));
						}
					});

					if (!undock)
						return true;

					File.Delete(dockedShipFileName);
					Communication.SendPrivateInformation(userId, string.Format("The ship '{0}' has been undocked from docking zone", dockingItem.DockedName));
				}

				Docking.Instance.DockingItems.Clear();
				Docking.Instance.Save();
			}

			return true;
		}
	}
}
