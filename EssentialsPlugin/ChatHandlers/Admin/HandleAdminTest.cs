namespace EssentialsPlugin.ChatHandlers.Admin
{
    using System.Linq;
    using Sandbox.ModAPI;
    using VRageMath;
    using Utility;
    using System.Collections.Generic;
    using Sandbox.Common.ObjectBuilders;
    using SEModAPI.API.Definitions;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Threading;
    using System.Windows.Forms;
    using NLog;
    using NLog.Targets;
    using Sandbox;
    using Sandbox.Common.ObjectBuilders;
    using SEModAPI.API;
    using SEModAPI.API.Definitions;
    using SEModAPI.API.Sandbox;
    using SEModAPI.API.Utility;
    using SEModAPIInternal.API.Chat;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Server;
    using SEModAPIInternal.Support;
    using Timer = System.Timers.Timer;
    using SEModAPIExtensions.API;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using SEModAPIInternal.API.Entity;
    using VRage;

    public class HandleAdminTest : ChatHandlerBase
	{

        private DedicatedConfigDefinition _dedicatedConfigDefinition;

    public override string GetHelp()
		{
			return "For testing.";
		}

		public override string GetCommandText()
		{
			return "/admin test";
		}

        public override string GetHelpDialog()
        {
            string longMessage =
                "/dialog \"Help\" \"Admin Test\" \"\"" +
                "\"For development testing.\" \"close\" ";
            return longMessage;
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
            if (false)
            {
                /*
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                IMyEntity target = null;
                bool skip = true;
                foreach (IMyEntity entity in entities)
                {
                    if (!(entity is IMyVoxelMap))
                        continue;

                    if (!entity.Save)
                        continue;

                    if (skip)
                    {
                        skip = false;
                        continue;
                    }

                    target = entity;
                    break;
                }

                if (target == null)
                    return true;

                DateTime start = DateTime.Now;
                IMyVoxelMap voxel = (IMyVoxelMap)target;

                Console.WriteLine(string.Format("Here: {0}", voxel.StorageName));
			
                MyStorageDataCache cache = new MyStorageDataCache();
                Vector3I size = voxel.Storage.Size;
                cache.Resize(size);
                Vector3I chunkSize = new Vector3I(32, 32, 32);
                //voxel.Storage.ReadRange(cache, MyStorageDataTypeFlags.All, 0, Vector3I.Zero, voxel.Storage.Size - 1);
                MyStorageDataCache[] cacheParts = new MyStorageDataCache[32 * 32 * 32];

                Parallel.For(0, size.X / chunkSize.X, x =>
                    {
                        Parallel.For(0, size.Y / chunkSize.Y, y =>
                            {
                                Parallel.For(0, size.Z / chunkSize.Z, z =>
                                    {
                                        cacheParts[x * y * z] = new MyStorageDataCache();
                                        MyStorageDataCache localCache = cacheParts[x * y * z];
                                        localCache.Resize(new Vector3I(32, 32, 32));
                                        //Console.WriteLine("Read: {0} - {1} - {2}", x, y, z);
                                        Vector3I cstart = new Vector3I(32 * x, 32 * y, 32 * z);
                                        Vector3I cend = new Vector3I(32 * (x + 1), 32 * (y + 1), 32 * (z + 1));
                                        voxel.Storage.ReadRange(localCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, cstart, cend - 1);
                                        //Console.WriteLine("Done Read: {0} - {1} - {2}", x, y, z);
                                    });
                            });
                    });

                MyObjectBuilder_VoxelMap voxelBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_VoxelMap>();
                voxelBuilder.StorageName = "testRoid";
                voxelBuilder.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
                voxelBuilder.PositionAndOrientation = new MyPositionAndOrientation?(MyPositionAndOrientation.Default);
                voxelBuilder.MutableStorage = false;

                IMyEntity newEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(voxelBuilder);
                if (newEntity != null)
                {
                    Console.WriteLine("Added");
                }

                Parallel.For(0, size.X / chunkSize.X, x =>
                {
                    Parallel.For(0, size.Y / chunkSize.Y, y =>
                    {
                        Parallel.For(0, size.Z / chunkSize.Z, z =>
                        {
                        });
                    });
                });

    //			IMyVoxelMap myVoxelMap = new IMyVoxelMap();
    //			{
    //				EntityId = 0;
    //			};
                //myVoxelMap.Init(str, myStorageBase, asteroidDetails.Position - (myStorageBase.Size * 0.5f));
    //			myVoxelMap.Init(voxelBuilder);

                //for(int x = 0; x < size.X / 32; x++)
                //{
                //}

                Console.WriteLine(string.Format("Cache read in {0} ms", (DateTime.Now - start).TotalMilliseconds));

                //MyObjectBuilder_VoxelMap voxel = 

                 */

                //ServerNetworkManager.ShowRespawnMenu(userId);
            }
            
            CubeGridEntity entity = new CubeGridEntity(new FileInfo(Essentials.PluginPath + "Platform.sbc"));
            //IMyEntity entity = new IMyEntities(new FileInfo(Essentials.PluginPath + "Platform.sbc"));
            long entityId = BaseEntity.GenerateEntityId();
            entity.EntityId = entityId;
            entity.PositionAndOrientation = new MyPositionAndOrientation((new Vector3(0,0,0)), Vector3.Forward, Vector3.Up);
            SectorObjectManager.Instance.AddEntity(entity);
            return true;

            //TimedEntityCleanup.Instance.Add( entityId );

            //MyAPIGateway.Entities.AddEntity (entity, true);
            //IMyPlayer.AddGrid(entityId);
            
            /*
            Vector3D position = Vector3D.Zero;
            Wrapper.GameAction(() =>
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, x => x.SteamUserId == userId);

                if (players.Count > 0)
                {
                    IMyPlayer player = players.First();
                    position = player.GetPosition();
                }
            });

            //return position;

            Communication.SendPrivateInformation(userId, string.Format("Position - X:{0:F2} Y:{1:F2} Z:{2:F2}", position.X, position.Y, position.Z));
            return true;
            
            

            if (File.Exists(System.IO.Path.Combine(Server.Instance.Path, "SpaceEngineers-Dedicated.cfg")))
            {
                MyConfigDedicatedData<MyObjectBuilder_SessionSettings> config = DedicatedConfigDefinition.Load(new FileInfo(System.IO.Path.Combine(Server.Instance.Path, "SpaceEngineers-Dedicated.cfg")));
                _dedicatedConfigDefinition = new DedicatedConfigDefinition(config);
            }

            foreach (string moditem in _dedicatedConfigDefinition.Mods)
            {
                Communication.SendPrivateInformation(userId, moditem);
            }
            return true;
            */
        }

	}

}

