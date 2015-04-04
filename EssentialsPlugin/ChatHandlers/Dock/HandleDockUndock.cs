namespace EssentialsPlugin.ChatHandlers.Dock
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using EssentialsPlugin.Utility;
	using Microsoft.Xml.Serialization.GeneratedAssembly;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using VRageMath;

	public class HandleDockUndock: ChatHandlerBase
	{
		// We need to only allow one dock at a time right now until I can address this.  It's causing duping when dropped packets occur
		public static bool m_undocking = false;

		public override string GetHelp()
		{
			return "This command allows you to undock a ship from a docking zone.  Usage: /dock undock <dockingZoneName>";
		}

		public override string GetCommandText()
		{
			return "/dock undock";
		}

		public override bool IsAdminCommand()
		{
			return false;
		}

		public override bool AllowedInConsole()
		{
			return false;
		}

		public override bool HandleCommand(ulong userId, string[] words)
		{
			if (!PluginSettings.Instance.DockingEnabled)
				return false;

			if (words.Length < 1)
			{
				Communication.SendPrivateInformation(userId, GetHelp());
				return true;
			}

			if (m_undocking)
			{
				Communication.SendPrivateInformation(userId, string.Format("Server is busy, try again"));
				return true;
			}

			m_undocking = true;
			try
			{
				String pylonName = String.Join(" ", words);
				if (PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).Count < 1)
				{
					Communication.SendPrivateInformation(userId, string.Format("Unable to find player Id: {0}", userId));
					return true;
				}

				long playerId = PlayerMap.Instance.GetPlayerIdsFromSteamId(userId).First();

				Dictionary<String, List<IMyCubeBlock>> testList;
				List<IMyCubeBlock> beaconList;
				DockingZone.FindByName(pylonName, out testList, out beaconList, playerId);

				if (beaconList.Count == 4)
				{
					foreach (IMyCubeBlock entity in beaconList)
					{
						if (!Entity.CheckOwnership(entity, playerId))
						{
							Communication.SendPrivateInformation(userId, string.Format("You do not have permission to use '{0}'.  You must either own all the beacons or they must be shared with faction.", pylonName));
							return true;
						}
					}

					IMyCubeBlock e = beaconList.First();
					IMyCubeGrid parent = (IMyCubeGrid)e.Parent;

					long[] beaconListIds = beaconList.Select(p => p.EntityId).ToArray();
					long ownerId = beaconList.First().OwnerId;
					List<DockingItem> dockingItems = Docking.Instance.Find(d => d.PlayerId == ownerId && d.TargetEntityId == parent.EntityId && d.DockingBeaconIds.Intersect(beaconListIds).Count() == 4);
					if (dockingItems.Count < 1)
					{
						Communication.SendPrivateInformation(userId, string.Format("You have no ships docked in docking zone '{0}'.", pylonName));
						return true;
					}

					DockingItem dockingItem = dockingItems.First();

					// Figure out center of docking area, and other distance information
					double maxDistance = 99;
					Vector3D vPos = new Vector3D(0, 0, 0);
					foreach (IMyCubeBlock b in beaconList)
					{
						Vector3D beaconPos = Entity.GetBlockEntityPosition(b);
						vPos += beaconPos;
					}

					vPos = vPos / 4;
					foreach (IMyCubeBlock b in beaconList)
					{
						Vector3D beaconPos = Entity.GetBlockEntityPosition(b);
						maxDistance = Math.Min(maxDistance, Vector3D.Distance(vPos, beaconPos));
					}

					List<IMySlimBlock> blocks = new List<IMySlimBlock>();
					parent.GetBlocks(blocks);
					foreach (IMySlimBlock slim_cbe in blocks)
					{
						if (slim_cbe is IMyCubeBlock)
						{
							IMyCubeBlock cbe = slim_cbe.FatBlock;
							if (cbe.GetObjectBuilderCubeBlock() is MyObjectBuilder_Cockpit)
							{
								MyObjectBuilder_Cockpit c = (MyObjectBuilder_Cockpit)cbe.GetObjectBuilderCubeBlock();
								if (c.Pilot != null)
								{
									Communication.SendPrivateInformation(userId, string.Format("Carrier ship has a pilot.  The carrier should be unpiloted and fully stopped before undocking.  (Sometimes this can lag a bit.  Wait 10 seconds and try again)", pylonName));
									return true;
								}
							}
						}
					}

					String dockedShipFileName = Essentials.PluginPath + String.Format("\\Docking\\docked_{0}_{1}_{2}.sbc", ownerId, dockingItem.TargetEntityId, dockingItem.DockedEntityId);

					// Load Entity From File and add to game
					FileInfo fileInfo = new FileInfo(dockedShipFileName);
					//CubeGridEntity cubeGrid = new CubeGridEntity(fileInfo);

					MyObjectBuilder_CubeGrid cubeGrid = BaseObjectManager.ReadSpaceEngineersFile<MyObjectBuilder_CubeGrid, MyObjectBuilder_CubeGridSerializer>(dockedShipFileName);

					// Rotate our ship relative to our saved rotation and the new carrier rotation                
					cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(Matrix.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(parent.Physics.GetWorldMatrix().GetOrientation()) * dockingItem.SaveQuat).GetOrientation());
					// Move our ship relative to the new carrier position and orientation
					Quaternion newQuat = Quaternion.CreateFromRotationMatrix(parent.Physics.GetWorldMatrix().GetOrientation());
					Vector3D rotatedPos = Vector3D.Transform(dockingItem.SavePos, newQuat);
					//cubeGrid.Position = rotatedPos + parent.GetPosition();				
					cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(rotatedPos + parent.GetPosition(), cubeGrid.PositionAndOrientation.Value.Forward, cubeGrid.PositionAndOrientation.Value.Up);

					// Add object to world
					cubeGrid.EntityId = BaseEntity.GenerateEntityId();
					cubeGrid.LinearVelocity = Vector3.Zero;
					cubeGrid.AngularVelocity = Vector3.Zero;

					bool undock = false;
					Wrapper.GameAction(() =>
					{
						try
						{
							MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cubeGrid);
							List<MyObjectBuilder_EntityBase> addList = new List<MyObjectBuilder_EntityBase>();
							addList.Add(cubeGrid);
							MyAPIGateway.Multiplayer.SendEntitiesCreated(addList);
							undock = true;
						}
						catch (Exception Ex)
						{
							Log.Info(string.Format("Error undocking ship: {0}", Ex.ToString()));
							Communication.SendPrivateInformation(userId, string.Format("Unable to undock ship due to error."));
						}
					});

					if (!undock)
						return true;

					//SectorObjectManager.Instance.AddEntity(cubeGrid);

					// Remove the docking file
					File.Delete(dockedShipFileName);
					Docking.Instance.Remove(dockingItem);

					Communication.SendPrivateInformation(userId, string.Format("The ship '{0}' has been undocked from docking zone '{1}'", dockingItem.DockedName, pylonName));

					/*
					// Queue for cooldown
					DockingCooldownItem cItem = new DockingCooldownItem();
					cItem.Name = pylonName;
					cItem.startTime = DateTime.Now;

					lock (m_cooldownList)
						m_cooldownList.Add(cItem);

					IMyEntity gridEntity = MyAPIGateway.Entities.GetEntityById(dockingItem.DockedEntityId);
					IMyCubeGrid cubeGrid = (IMyCubeGrid)gridEntity;

					Quaternion q = Quaternion.CreateFromRotationMatrix(parent.WorldMatrix.GetOrientation()) * dockingItem.SaveQuat;
					Quaternion newQuat = Quaternion.CreateFromRotationMatrix(parent.WorldMatrix.GetOrientation());
					Vector3 parentPosition = parent.GetPosition();
					Vector3 rotatedPos = Vector3.Transform(dockingItem.savePos, newQuat);
					Vector3 position = rotatedPos + parentPosition;
					Matrix positionMatrix = Matrix.CreateFromQuaternion(q);

					cubeGrid.ChangeGridOwnership(playerId, MyOwnershipShareModeEnum.None);
					gridEntity.SetPosition(dockingItem.savePos);

					gridEntity.WorldMatrix = positionMatrix;
					gridEntity.SetPosition(position);

					// We need to update again, as this doesn't seem to sync properly?  I set world matrix, and setposition, and it doesn't go where it should, and I
					// have to bump into it for it to show up, it's mega weird.

					if (PluginDocking.Settings.DockingItems == null)
						throw new Exception("DockingItems is null");

					// Remove from docked items
					PluginDocking.Settings.DockingItems.Remove(dockingItem);

					// Notify user
					Communication.SendPrivateInformation(userId, string.Format("The ship '{0}' has been undocked from docking zone '{1}'", gridEntity.DisplayName, pylonName));
					*/
					// Queue for cooldown
					/*
					DockingCooldownItem cItem = new DockingCooldownItem();
					cItem.name = pylonName;
					cItem.startTime = DateTime.Now;
					PluginDocking.CooldownList.Add(cItem);
					 */
				}
				else if (beaconList.Count > 4) // Too many beacons, must be 4
				{
					Communication.SendPrivateInformation(userId, string.Format("Too many beacons with the name or another zone with the name '{0}'.  Place only 4 beacons to create a zone or try a different zone name.", pylonName));
				}
				else // Can't find docking zone
				{
					Communication.SendPrivateInformation(userId, string.Format("Can not locate docking zone '{0}'.  There must be 4 beacons with the name '{0}' to create a docking zone.  Beacons must be fully built!", pylonName));
				}
			}
			finally
			{
				m_undocking = false;
			}

			return true;

		}
	}
}
