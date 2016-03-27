namespace EssentialsPlugin.Settings
{
	using System;
	using System.ComponentModel;

    [Serializable]
	public class TicketPlayerItem
    {
	    public TicketPlayerItem( ulong ticketId, int timeAllocated )
	    {
	        this.TicketId = ticketId;
	        this.TimeAllocated = timeAllocated;
	    }

        public TicketPlayerItem( )
        {
        }

        public readonly ulong TicketId;
        public int TimeAllocated;
        public int TimeUsed;
    }
}
