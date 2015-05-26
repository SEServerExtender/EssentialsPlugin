using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

using EssentialsPlugin.Utility;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;

using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Server;

using EssentialsPlugin.UtilityClasses;

namespace EssentialsPlugin.EntityManagers
{
    public class VoxelManagement
    {
        private static Dictionary<ulong, HashSet<long>> m_userVoxels = new Dictionary<ulong, HashSet<long>>();
        private static bool m_voxelCheck = false;

        public static void CheckAndSendVoxels()
        {
            if (m_voxelCheck)
                return;

            m_voxelCheck = true;

            try
            {
                DateTime start = DateTime.Now;
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                List<IMyPlayer> players = new List<IMyPlayer>();
                try
                {
                    MyAPIGateway.Entities.GetEntities(entities);
                    MyAPIGateway.Players.GetPlayers(players);
                }
                catch
                {
	                Essentials.Log.Info( "Entity list busy, skipping check" );
                    return;
                }

                HashSet<Tuple<ulong, IMyEntity>> voxelsToSend = new HashSet<Tuple<ulong, IMyEntity>>();
                foreach (IMyEntity entity in entities)
                {
                    if (!(entity is IMyVoxelMap))
                        continue;

                    IMyVoxelMap voxel = (IMyVoxelMap)entity;

                    foreach (IMyPlayer player in players)
                    {
                        double distance = 0d;
                        if (Entity.GetDistanceBetweenPointAndPlayer(entity.GetPosition(), player, out distance))
                        {
                            if (!m_userVoxels.ContainsKey(player.SteamUserId))
                                m_userVoxels.Add(player.SteamUserId, new HashSet<long>());

                            HashSet<long> voxels = m_userVoxels[player.SteamUserId];

                            if (distance < PluginSettings.Instance.DynamicVoxelDistance && !voxels.Contains(entity.EntityId))
                            {
                                voxelsToSend.Add(new Tuple<ulong, IMyEntity>(player.SteamUserId, entity));
                            }
                        }
                    }
                }

                if (voxelsToSend.Count > 0)
                {
                    Wrapper.GameAction(() =>
                    {
                        foreach (Tuple<ulong, IMyEntity> p in voxelsToSend)
                        {
                            SendVoxel(p.Item1, p.Item2);
                        }
                    });
                }

                if ((DateTime.Now - start).TotalSeconds > 1)
                    Essentials.Log.Debug("CheckAndSendVoxels(): {0}ms", (DateTime.Now - start).TotalMilliseconds);
            }
            catch (Exception ex)
            {
	            Essentials.Log.Error( "{0}", ex );
            }
            finally
            {
                m_voxelCheck = false;
            }
        }

        public static void ClearCache(ulong steamId)
        {
            if (!m_userVoxels.ContainsKey(steamId))
                return;

            HashSet<long> voxels = m_userVoxels[steamId];
            voxels.Clear();
        }

        private static void SendVoxel(ulong steamId, IMyEntity voxel)
        {
            if (!m_userVoxels.ContainsKey(steamId))
                m_userVoxels.Add(steamId, new HashSet<long>());

            HashSet<long> voxels = m_userVoxels[steamId];
            voxels.Add(voxel.EntityId);

            SendVoxelData(steamId, voxel);
        }

        private static void SendVoxelData(ulong steamId, IMyEntity voxel)
        {
            try
            {
                IMyVoxelMap voxelMap = (IMyVoxelMap)voxel;

                byte[] voxelData;
                voxelMap.Storage.Save(out voxelData);

                VoxelHeaderData header = new VoxelHeaderData();
                header.EntityId = voxel.EntityId;
                header.HalfExtent = voxelMap.Storage.Size / 2;
                header.Position = voxel.GetPosition();
                header.Name = voxelMap.StorageName;
                header.DataLength = voxelData.Length;

                string headerString = MyAPIGateway.Utilities.SerializeToXML<VoxelHeaderData>(header);
                ushort length = (ushort)headerString.Length;

                byte[] headerData = new byte[2 + headerString.Length];

                headerData[0] = (byte)length;
                headerData[1] = (byte)(length >> 8);

                for (int r = 0; r < headerString.Length; r++)
                {
                    headerData[r + 2] = (byte)headerString[r];
                }
                Essentials.Log.Debug("Sending Voxel Header Data: {0} / {1} - {2} ({3})", voxelData.Length, headerData.Length, steamId, voxel.GetPosition());
                Communication.SendDataMessage(steamId, 5001, headerData);

                int blockSize = 4096;
                for (ushort r = 0; r < (voxelData.Length / blockSize) + 1; r++)
                {
                    int partLength = voxelData.Length - (r * blockSize);

                    if (partLength > blockSize)
                        partLength = blockSize;

                    byte[] outData = new byte[partLength + 12];

                    for (int s = 0; s < 8; s++)
                        outData[s] = (byte)(header.EntityId >> (s * 8));

                    for (int s = 0; s < 2; s++)
                        outData[s + 8] = (byte)(partLength >> (s * 8));

                    for (int s = 0; s < 2; s++)
                        outData[s + 10] = (byte)(r >> (s * 8));

                    Buffer.BlockCopy(voxelData, r * blockSize, outData, 12, partLength);
                    Communication.SendDataMessage(steamId, 5002, outData);
                }
            }
            catch (Exception ex)
            {
	            Essentials.Log.Error( "{0}", ex );
            }
        }

        public class VoxelHeaderData
        {
            public long EntityId { get; set; }
            public string Name { get; set; }
            public SerializableVector3I HalfExtent { get; set; }
            public SerializableVector3D Position { get; set; }
            public int DataLength { get; set; }
        }
    }
}
