using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace EssentialsPlugin.UtilityClasses
{
	public class MTObservableCollection<T> : ObservableCollection<T>
	{
		public override event NotifyCollectionChangedEventHandler CollectionChanged;
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;
			if (CollectionChanged != null)
			{
				foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
				{
					DispatcherObject dispObj = nh.Target as DispatcherObject;
					if (dispObj != null)
					{
						Dispatcher dispatcher = dispObj.Dispatcher;
						if (dispatcher != null && !dispatcher.CheckAccess())
						{
							dispatcher.BeginInvoke((Action)(() => nh.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))), DispatcherPriority.DataBind);
							continue;
						}
					}
					nh.Invoke(this, e);
				}
			}
		}
	}
}
