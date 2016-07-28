namespace EssentialsPlugin.Settings
{
	using System;
	using Utility;

    [Serializable]
	public class BlacklistItem
	{
		private string _type;
	    public string Type
		{
			get { return _type; }
	        set
	        {
	            _type = value;
                /*
	            switch ( BlacklistManager.Instance.ValidateBlacklistItem( this ) )
	            {
	                case BlacklistManager.BlacklistVerificationEnum.InvalidType:
	                    throw new ArgumentException( $"{_type} is not a valid type!" );

	                case BlacklistManager.BlacklistVerificationEnum.InvalidMember:
	                    throw new ArgumentException( $"{_member} is not a valid member of {_type}!" );

	                case BlacklistManager.BlacklistVerificationEnum.NoType:
	                    throw new ArgumentException( "You mus give a type if you specify a member!" );                        
	            }
                */
	        }
		}

		private string _member;
		public string Member
		{
			get { return _member; }
			set
			{
                _member = value;
                /*
                switch (BlacklistManager.Instance.ValidateBlacklistItem(this))
                {
                    case BlacklistManager.BlacklistVerificationEnum.InvalidType:
                        throw new ArgumentException($"{_type} is not a valid type!");

                    case BlacklistManager.BlacklistVerificationEnum.InvalidMember:
                        throw new ArgumentException($"{_member} is not a valid member of {_type}!");

                    case BlacklistManager.BlacklistVerificationEnum.NoType:
                        throw new ArgumentException("You mus give a type if you specify a member!");
                }
                */
            }
		}
	}
}
