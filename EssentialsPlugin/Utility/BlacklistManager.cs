using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.ModAPI;
using System.Threading.Tasks;

namespace EssentialsPlugin.Utility
{
    /*
    using System.Collections.Specialized;
    using System.Reflection;
    using Settings;
    using VRage.Collections;
    using VRage.Library.Collections;
    using VRage.Scripting;

    public class BlacklistManager
    {
        private static BlacklistManager _instance;

        public static BlacklistManager Instance
        {
            get
            {
                if ( _instance == null )
                    _instance = new BlacklistManager();
                return _instance;
            }
        }

        public void _blacklistItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateBlacklist();
        }

        public void UpdateBlacklist()
        {
            var blacklist = MyScriptCompiler.Static.Whitelist.OpenIngameBlacklistBatch();
            var types = new MyConcurrentHashSet<Type>();
            var memberDict = new MyConcurrentDictionary<Type, List<string>>();
            Parallel.ForEach( PluginSettings.Instance.BlacklistItems, item =>
                                                                      {
                                                                          if ( string.IsNullOrEmpty( item.Type ) )
                                                                              return;

                                                                          var targetType = FindTypeInAllAssemblies( item.Type );
                                                                          if ( targetType == null )
                                                                              return;

                                                                          if(string.IsNullOrEmpty( item.Member ))
                                                                              lock ( types )
                                                                                  types.Add( targetType );

                                                                          var members = targetType.GetMember( item.Member );
                                                                          if ( members.Length != 0 )
                                                                          {
                                                                              if(!memberDict.ContainsKey( targetType ))
                                                                                  memberDict.Add( targetType, new List<string>() );
                                                                              memberDict[targetType].Add( item.Member );
                                                                          }
                                                                      } );

            if ( types.Count > 0 )
            {
                blacklist.AddTypes( types.ToArray() );
                foreach(var type in types)
                    Essentials.Log.Info( $"Added type {type.Name} to PB blacklist." );
            }
            foreach ( var entry in memberDict )
            {
                blacklist.AddMembers( entry.Key, entry.Value.ToArray() );
                foreach ( var name in entry.Value )
                {
                    Essentials.Log.Info( $"Added {entry.Key.Name}.{name} to PB blacklist." );
                }
            }
        }

        private Type FindTypeInAllAssemblies( string typeName )
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach ( var assembly in assemblies )
            {
                var types = assembly.GetTypes();
                foreach ( var type in types )
                {
                    if ( type.Name == typeName )
                    {
                        return type;
                    }
                }
            }
            return null;
        }
        
        public enum BlacklistVerificationEnum
        {
            Ok,
            InvalidType,
            InvalidMember,
            NoType,
            Empty
        }

        public BlacklistVerificationEnum ValidateBlacklistItem( BlacklistItem item )
        {
            if ( string.IsNullOrEmpty( item.Type ) && string.IsNullOrEmpty( item.Member ) )
                return BlacklistVerificationEnum.Empty;
            if ( string.IsNullOrEmpty( item.Type ) && !string.IsNullOrEmpty( item.Member ) )
                return BlacklistVerificationEnum.NoType;

            var targetType = FindTypeInAllAssemblies( item.Type );

            if ( targetType != null && string.IsNullOrEmpty( item.Member ))
                return BlacklistVerificationEnum.Ok;

            if (targetType == null)
                return BlacklistVerificationEnum.InvalidType;

            if ( targetType.GetMember( item.Member ).Length > 0 )
                return BlacklistVerificationEnum.Ok;
            else
                return BlacklistVerificationEnum.InvalidMember;
        }
    }
    */
}
