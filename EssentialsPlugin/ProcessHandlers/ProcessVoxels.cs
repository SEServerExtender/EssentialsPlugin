namespace EssentialsPlugin.ProcessHandlers
{
	using System;
	using EssentialsPlugin.EntityManagers;

	public class ProcessVoxels : ProcessHandlerBase
    {
        private static DateTime _lastEnableCheck;


        public override int GetUpdateResolution()
        {
            return 1000;
        }

        public override void Handle()
        {
            if (PluginSettings.Instance.DynamicVoxelManagementEnabled)
            {
                if (DateTime.Now - _lastEnableCheck > TimeSpan.FromSeconds(5))
                {
                    VoxelManagement.CheckAndSendVoxels();
                    _lastEnableCheck = DateTime.Now;
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

