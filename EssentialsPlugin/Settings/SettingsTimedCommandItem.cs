namespace EssentialsPlugin.Settings
{
    using System;

    public class TimedCommandItem
    {
        public DateTime CommandRunTime;
        private string _commandTime;

        public string CommandTime
        {
            get
            {
                return _commandTime;
            }
            set
            {
                _commandTime = value;
                CommandRunTime = DateTime.Parse( _commandTime );
            }
        }

        public string CommandText { get; set; }

        public bool Enabled { get; set; }

        public string WarningText { get; set; }

        public bool WarningEnabled { get; set; }

        public int WarningTime { get; set; }

        public bool HasWarned;

        public bool HasRun;

    }
}
