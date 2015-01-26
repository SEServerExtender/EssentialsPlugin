namespace EssentialsPlugin.ProcessHandlers
{
	using System;

	public abstract class ProcessHandlerBase
	{
		public DateTime LastUpdate { get; set; }

		/// <summary>
		/// Initializer
		/// </summary>
		protected ProcessHandlerBase()
		{
			LastUpdate = DateTime.Now;

			//Logging.WriteLineAndConsole(string.Format("Added process handler: Raised every {0}ms", GetUpdateResolution()));
		}

		/// <summary>
		/// Returns whether this handler is ready to be run
		/// </summary>
		/// <returns></returns>
		public bool CanProcess()
		{
			if(DateTime.Now - LastUpdate > TimeSpan.FromMilliseconds(GetUpdateResolution()))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the processing resolution of this handler in milliseconds.
		/// </summary>
		/// <returns>resolution in ms</returns>
		public virtual int GetUpdateResolution()
		{
			return 1000;
		}

		/// <summary>
		/// Called when CanProcess() returns true.  This gets overriden and is the main handling function
		/// </summary>
		public virtual void Handle()
		{
			LastUpdate = DateTime.Now;
		}

		/// <summary>
		/// Called when a player joins
		/// </summary>
		/// <param name="remoteUserId"></param>
		public virtual void OnPlayerJoined(ulong remoteUserId)
		{

		}

		/// <summary>
		/// Called when a player leaves
		/// </summary>
		/// <param name="remoteUserId"></param>
		public virtual void OnPlayerLeft(ulong remoteUserId)
		{

		}

		public virtual void OnPlayerWorldSent(ulong remoteUserId)
		{

		}

	}
}
