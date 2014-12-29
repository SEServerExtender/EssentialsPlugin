using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using EssentialsPlugin.Utility;
using System.Windows.Forms;
using SEModAPIInternal.API.Common;
using SEModAPIExtensions.API;
using Sandbox.ModAPI;
using VRageMath;

using EssentialsPlugin.Settings;

namespace EssentialsPlugin.ProcessHandler
{
	class ProcessRestart : ProcessHandlerBase
	{
		static private DateTime? m_forcedRestart = null;

		private DateTime m_start;
		private int m_done = -1;

		static public DateTime? ForcedRestart
		{
			get { return m_forcedRestart; }
			set { m_forcedRestart = value; }
		}

		public ProcessRestart()
		{
			m_start = DateTime.Now;
			SetRestartTime();
			StopAllShips();
		}

		public override int GetUpdateResolution()
		{
			return 10000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.RestartWhenUnresponsive)
				CheckResponse();

			if(!PluginSettings.Instance.RestartEnabled)
				return;

			SetRestartTime();
			if(m_done < 0)
				return;


			//Logging.WriteLineAndConsole(string.Format("Restart in {0} minutes", m_done));

			if (DateTime.Now - m_start > TimeSpan.FromMinutes(m_done))
			{
				DoRestart();
				return;
			}

			foreach (RestartNotificationItem item in PluginSettings.Instance.RestartNotificationItems)
			{
				if (item.completed)
					continue;

				if (m_done - item.MinutesBeforeRestart < -2)
					continue;
				
				if(DateTime.Now - m_start > TimeSpan.FromMinutes(m_done - item.MinutesBeforeRestart))
				{
					item.completed = true;					
					Communication.SendPublicInformation(item.Message);

					if(item.StopAllShips)
					{
						StopAllShips();
					}

					if(item.Save)
					{
						WorldManager.Instance.SaveWorld();
					}
				}
			}

			base.Handle();
		}

		private void DoRestart()
		{
			// If we're not a service, restart with a .bat otherwise just exit and let the service be restarted
			if (Environment.UserInteractive)
			{
				String restartText = "%windir%/system32/timeout /t 30\r\n";
				restartText += String.Format("cd /d \"{0}\"\r\n", System.IO.Path.GetDirectoryName(Application.ExecutablePath));
				restartText += PluginSettings.Instance.RestartAddedProcesses + "\r\n";
				restartText += System.IO.Path.GetFileName(Application.ExecutablePath) + " " + Server.Instance.CommandLineArgs.args + "\r\n";

				File.WriteAllText("RestartApp.bat", restartText);
				System.Diagnostics.Process.Start("RestartApp.bat");
			}

			Environment.Exit(1);
		}

		private void StopAllShips()
		{
			Logging.WriteLineAndConsole("Stopping all ships");
			int shipsStopped = 0;

			Wrapper.GameAction(() =>
			{
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
				foreach (IMyEntity entity in entities)
				{
					if (entity.Physics == null)
						continue;

					double linear = Math.Round(((Vector3)entity.Physics.LinearVelocity).LengthSquared(), 1);
					double angular = Math.Round(((Vector3)entity.Physics.AngularVelocity).LengthSquared(), 1);

					if(linear > 0 || angular > 0)
					{
						entity.Physics.LinearVelocity = Vector3.Zero;
						entity.Physics.AngularVelocity = Vector3.Zero;
						shipsStopped++;
					}
				}
			});

			Logging.WriteLineAndConsole(string.Format("{0} ships have been stopped", shipsStopped));
		}

		private void SetRestartTime()
		{
			DateTime? restartTime = GetNextRestartTime();
			if (restartTime != null)
				m_done = (int)(restartTime.Value - m_start).TotalMinutes;
			else
				m_done = -1;
		}

		private DateTime? GetNextRestartTime()
		{
			DateTime? result = null;

			if (m_forcedRestart != null)
				return m_forcedRestart;

			foreach (RestartTimeItem item in PluginSettings.Instance.RestartTimeItems)
			{
				if (!item.Enabled)
					continue;

				DateTime time = new DateTime(m_start.Year, m_start.Month, m_start.Day, item.Restart.Hour, item.Restart.Minute, 0);
				if (time < m_start.AddMinutes(-1))
					time = time.AddDays(1);

				//Logging.WriteLineAndConsole(string.Format("Time: {0}", time));

				if (result == null)
					result = time;
				else
				{
					if (result.Value > time)
						result = time;
				}
			}

			return result;
		}

		private void CheckResponse()	
		{
			ThreadPool.QueueUserWorkItem((object state) =>
			{
				DateTime start = DateTime.Now;
				AutoResetEvent are = new AutoResetEvent(false);
				SandboxGameAssemblyWrapper.Instance.EnqueueMainGameAction(() =>
				{
					are.Set();
				});

				if (!are.WaitOne(120000))
				{
					Logging.WriteLineAndConsole("Server unresponsive for 60 seconds, restarting in 5 seconds.");
					Thread.Sleep(5000);
					DoRestart();
					return;
				}

				if((DateTime.Now - start).TotalMilliseconds > 10000)
					Logging.WriteLineAndConsole(string.Format("Warning: Server Response Time: {0}ms", (DateTime.Now - start).TotalMilliseconds));
			});
		}
	}
}
