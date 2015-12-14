namespace EssentialsPlugin.ChatHandlers.Dock
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security;
	using EssentialsPlugin.Utility;
	using EssentialsPlugin.UtilityClasses;
	using Microsoft.Xml.Serialization.GeneratedAssembly;
	using Sandbox.Common.ObjectBuilders;
	using Sandbox.ModAPI;
	using SEModAPIInternal.API.Common;
	using SEModAPIInternal.API.Entity;
	using VRage.ModAPI;
	using VRage.ObjectBuilders;
	using VRageMath;

	public class HandleDockDock: ChatHandlerBase
	{
		// We need to only allow one dock at a time right now until I can address this.  It's causing duping when dropped packets occur
		public static bool m_docking = false;

		public override string GetHelp()
		{
			return "This command allows you to dock a ship in a docking zone.  Usage: /dock dock <dockingZoneName>";
		}

		public override string GetCommandText()
		{
			return "/dock dock";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"\" \"\"" +
                "\""+GetHelp()+"\" \"close\" ";
            return longMessage;
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

			if (m_docking)
			{
				Communication.SendPrivateInformation(userId, "Server is busy");
				return true;
			}

			m_docking = true;
			try
			{
				String pylonName = String.Join(" ", words);

				/*
				int timeLeft;
				if (Entity.CheckCoolDown(pylonName, out timeLeft))
				{
					Communication.Message(String.Format("The docking zone '{0}' is on cooldown.  Please wait a {1} seconds before trying to dock/undock again.", pylonName, Math.Max(0, timeLeft)));
					return;
				}
				*/

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
					// Check ownership
					foreach (IMyCubeBlock entityBlock in beaconList)
					{
						IMyTerminalBlock terminal = (IMyTerminalBlock)entityBlock;
						if (!terminal.HasPlayerAccess(playerId))
						{
							Communication.SendPrivateInformation(userId, string.Format("You do not have permission to use '{0}'.  You must either own all the beacons or they must be shared with faction.", pylonName));
							return true;
						}
					}

					// Check for bounding box intsection of other docking zones
					int intersectElement = 0;
					if (Entity.CheckForIntersection(testList, beaconList, out intersectElement))
					{
						Communication.SendPrivateInformation(userId, string.Format("The docking zone '{0}' intersects with docking zone '{1}'.  Make sure you place your docking zones so they don't overlap.", pylonName, testList.ElementAt(intersectElement).Key));
						return true;
					}

					// Check if ship already docked in this zone
					IMyCubeBlock e = beaconList[0];
					IMyCubeGrid parent = (IMyCubeGrid)e.Parent;
					long[] beaconListIds = beaconList.Select(b => b.EntityId).ToArray();
					long ownerId = beaconList.First().OwnerId;
					List<DockingItem> checkItems = Docking.Instance.Find(d => d.PlayerId == ownerId && d.TargetEntityId == parent.EntityId && d.DockingBeaconIds.Intersect(beaconListIds).Count() == 4);
					if (checkItems.Count >= PluginSettings.Instance.DockingShipsPerZone)
					{
						Communication.SendPrivateInformation(userId, string.Format("Docking zone already '{0}' already contains the maximum capacity of ships.", pylonName));
						return true;
					}

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

					// Find ship in docking area
					IMyCubeGrid dockingEntity = null;
					HashSet<IMyEntity> cubeGrids = new HashSet<IMyEntity>();
					MyAPIGateway.Entities.GetEntities(cubeGrids, f => f is IMyCubeGrid);
					foreach (IMyCubeGrid gridCheck in cubeGrids)
					{
						if (gridCheck.IsStatic || gridCheck == parent)
							continue;

						double distance = Vector3D.Distance(gridCheck.GetPosition(), vPos);
						if (distance < maxDistance)
						{
							dockingEntity = gridCheck;
							break;
						}
					}

					// Figure out if the ship fits in docking area, and then save ship    
					if (dockingEntity != null)
					{
						// Get bounding box of both the docking zone and docking ship
						OrientedBoundingBoxD targetBounding = Entity.GetBoundingBox(beaconList);
						OrientedBoundingBoxD dockingBounding = Entity.GetBoundingBox(dockingEntity);

						// Make sure the docking zone contains the docking ship.  If they intersect or are disjointed, then fail
						if (!Entity.GreaterThan(dockingBounding.HalfExtent * 2, targetBounding.HalfExtent * 2))
						{
							Communication.SendPrivateInformation(userId, string.Format("The ship '{0}' is too large for it's carrier.  The ship's bounding box must fit inside the docking zone bounding box!", dockingEntity.DisplayName));
							return true;
						}

						if (targetBounding.Contains(ref dockingBounding) != ContainmentType.Contains)
						{
							Communication.SendPrivateInformation(userId, string.Format("The ship '{0}' is not fully inside the docking zone '{1}'.  Make sure the ship is fully contained inside the docking zone", dockingEntity.DisplayName, pylonName));
							return true;
						}

						// Calculate the mass and ensure the docking ship is less than half the mass of the dock
						float parentMass = Entity.CalculateMass(parent);
						float dockingMass = Entity.CalculateMass(dockingEntity);
						if (dockingMass > parentMass)
						{
							Communication.SendPrivateInformation(userId, string.Format("The ship you're trying to dock is too heavy for it's carrier.  The ship mass must be less than half the large ship / stations mass! (DM={0}kg CM={1}kg)", dockingMass, parentMass));
							return true;
						}

						// Check to see if the ship is piloted, if it is, error out.  
						// TODO: Check to see if we can get a real time copy of this entity?
						List<IMySlimBlock> blocks = new List<IMySlimBlock>();
						dockingEntity.GetBlocks(blocks, x => x.FatBlock != null && x.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Cockpit));
						foreach (IMySlimBlock slim_cbe in blocks)
						{
							MyObjectBuilder_Cockpit c = (MyObjectBuilder_Cockpit)slim_cbe.FatBlock.GetObjectBuilderCubeBlock();
							if (c.Pilot != null)
							{
								Communication.SendPrivateInformation(userId, string.Format("Ship in docking zone '{0}' has a pilot!  Please exit the ship before trying to dock.  (Sometimes this can lag a bit.  Wait 10 seconds and try again)", pylonName));
								return true;
							}
						}

						// Save position and rotation information.  Some fun stuff here.
						// Get our dock rotation as a quaternion
						Quaternion saveQuat = Quaternion.CreateFromRotationMatrix(parent.WorldMatrix.GetOrientation());
						// Transform docked ship's local position by inverse of the the parent (unwinds parent) and save it for when we undock
						Vector3D savePos = Vector3D.Transform(dockingEntity.GetPosition() - parent.GetPosition(), Quaternion.Inverse(saveQuat));
						// Get local rotation of dock ship, and save it for when we undock
						saveQuat = Quaternion.Inverse(saveQuat) * Quaternion.CreateFromRotationMatrix(dockingEntity.WorldMatrix.GetOrientation());

						// Save ship to file and remove
						FileInfo info = new FileInfo( Path.Combine( Essentials.PluginPath, "Docking", string.Format( "docked_{0}_{1}_{2}.sbc", ownerId, parent.EntityId, dockingEntity.EntityId ) ) );
						if ( !Directory.Exists( info.DirectoryName ) )
						{
							Directory.CreateDirectory( info.DirectoryName );
						}
						//CubeGridEntity dockingGrid = new CubeGridEntity((MyObjectBuilder_CubeGrid)dockingEntity.GetObjectBuilder(), dockingEntity);
						MyObjectBuilder_CubeGrid gridBuilder = CubeGrids.SafeGetObjectBuilder(dockingEntity);
						if (gridBuilder == null)
						{
							Communication.SendPrivateInformation(userId, string.Format("Failed to load entity for export: {0}", dockingEntity.DisplayName));
							return true;
						}

						// Save item
						DockingItem dockItem = new DockingItem
						                       {
							                       DockedEntityId = dockingEntity.EntityId,
							                       TargetEntityId = parent.EntityId,
							                       PlayerId = ownerId,
							                       DockingBeaconIds = beaconList.Select( s => s.EntityId ).ToArray( ),
							                       DockedName = dockingEntity.DisplayName,
							                       SavePos = savePos,
							                       SaveQuat = saveQuat
						                       };
						Docking.Instance.Add(dockItem);

						// Serialize and save ship to file
						MyObjectBuilderSerializer.SerializeXML( info.FullName, false, gridBuilder );
						//BaseObjectManager.SaveContentFile<MyObjectBuilder_CubeGrid, MyObjectBuilder_CubeGridSerializer>(gridBuilder, info);
						BaseEntityNetworkManager.BroadcastRemoveEntity(dockingEntity);
						//dockingEntity.Close();

						Communication.SendPrivateInformation(userId, string.Format("Docked ship '{0}' in docking zone '{1}'.", dockItem.DockedName, pylonName));
						Log.Info( "Docked ship \"{0}\" in docking zone \"{1}\". Saved to {2}", dockItem.DockedName, pylonName, info.FullName );
						/*
						// Add a cool down
						DockingCooldownItem cItem = new DockingCooldownItem();
						cItem.name = pylonName;
						cItem.startTime = DateTime.Now;
						PluginDocking.CooldownList.Add(cItem);
						 */
					}
					else
					{
						Communication.SendPrivateInformation(userId, string.Format("No ships in docking zone '{0}'.", pylonName));
					}
				}
				else if (beaconList.Count > 4)
				{
					Communication.SendPrivateInformation(userId, string.Format("Too many beacons with the name or another zone with the name '{0}'.  Place only 4 beacons to create a zone or try a different zone name.", pylonName));
				}
				else
				{
					Communication.SendPrivateInformation(userId, string.Format("Can not locate docking zone '{0}'.  There must be 4 beacons with the name '{0}' to create a docking zone.  Beacons must be fully built!", pylonName));
				}

				return true;
			}
			catch ( SecurityException ex )
			{
				Log.Error( "Can't access docked ship file.", ex );
				return false;
			}
			catch ( UnauthorizedAccessException ex )
			{
				Log.Error( "Can't access docked ship file.", ex );
				return false;
			}
			catch ( DirectoryNotFoundException ex )
			{
				Log.Error( "Directory does not exist", ex );
				return false;
			}
			catch ( IOException ex )
			{
				return false;
			}
			finally
			{
				m_docking = false;
			}
		}
	}
}
