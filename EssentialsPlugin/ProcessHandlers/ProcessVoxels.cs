using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage.Common.Utils;
using System.Text.RegularExpressions;
using System.Threading;

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Entity;

using Sandbox.Common.ObjectBuilders;

using EssentialsPlugin.Settings;
using EssentialsPlugin.EntityManagers;

using Sandbox.Definitions;

namespace EssentialsPlugin.ProcessHandler
{
    public class ProcessVoxels : ProcessHandlerBase
    {
        private static DateTime m_lastEnableCheck;


        public override int GetUpdateResolution()
        {
            return 1000;
        }

        public override void Handle()
        {
            if (PluginSettings.Instance.DynamicVoxelManagementEnabled)
            {
                if (DateTime.Now - m_lastEnableCheck > TimeSpan.FromSeconds(5))
                {
                    VoxelManagement.CheckAndSendVoxels();
                    m_lastEnableCheck = DateTime.Now;
                }
            }

            base.Handle();
        }

        public override void OnPlayerJoined(ulong remoteUserId)
        {
            if (!PluginSettings.Instance.DynamicVoxelManagementEnabled)
                return;

            VoxelManagement.ClearCache(remoteUserId);
        }
    }
}

