namespace EssentialsPlugin.Settings
{
	public class SettingsBlockEnforcementItem
	{
		public enum EnforcementMode
		{
			Off = 0,
			BlockTypeId = 1,
			BlockSubtypeId = 2
		}

		private string _blockTypeId;
		public string BlockTypeId
		{
			get { return _blockTypeId; }
			set { _blockTypeId = value; }
		}

		private string _blockSubtypeId;

		public string BlockSubtypeId
		{
			get { return _blockSubtypeId; }
			set { _blockSubtypeId = value; }
		}

		private int _maxPerGrid;
		public int MaxPerGrid
		{
			get { return _maxPerGrid; }
			set { _maxPerGrid = value; }
		}

		private string _maxReachWarning;
		public string MaxReachWarning
		{
			get { return _maxReachWarning; }
			set { _maxReachWarning = value; }
		}

		private string _maxExceedWarning;
		private EnforcementMode _mode;

		public string MaxExceedWarning
		{
			get { return _maxExceedWarning; }
			set { _maxExceedWarning = value; }
		}

		public EnforcementMode Mode
		{
			get { return _mode; }
			set { _mode = value; }
		}

        private bool _adminExempt;
        public bool AdminExempt
        {
            get
            {
                return _adminExempt;
            }
            set
            {
                _adminExempt = value;
            }
        }

		public override int GetHashCode( )
		{
			return ( string.IsNullOrEmpty( _blockSubtypeId ) ? string.Empty : _blockSubtypeId ).GetHashCode( )
			       + ( string.IsNullOrEmpty( _blockTypeId ) ? string.Empty : _blockTypeId ).GetHashCode( );
		}
	}
}
