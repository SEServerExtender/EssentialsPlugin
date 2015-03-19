using System;
using SEModAPIInternal.API.Common;
using System.Reflection;
using SEModAPIInternal.Support;


namespace EssentialsPlugin.Utility
{
	public static class Wrapper
	{
		public static void GameAction( Action action )
		{
			SandboxGameAssemblyWrapper.Instance.GameAction( action );
		}

		public static void BeginGameAction( Action action, SandboxGameAssemblyWrapper.GameActionCallback callback, Object state )
		{
			SandboxGameAssemblyWrapper.Instance.BeginGameAction( action, callback, state );
		}

		internal static FieldInfo GetStaticField( Type objectType, string fieldName )
		{
			try
			{
				FieldInfo field = objectType.GetField( fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy ) ??
								  objectType.BaseType.GetField( fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				return field;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( string.Format( "Failed to get static field '{0}'", fieldName ), ex );
				return null;
			}
		}

		internal static FieldInfo GetEntityField( Object gameEntity, string fieldName )
		{
			try
			{
				FieldInfo field = gameEntity.GetType( ).GetField( fieldName );
				if ( field == null )
				{
					//Recurse up through the class heirarchy to try to find the field
					Type type = gameEntity.GetType( );
					while ( type != typeof( Object ) )
					{
						field = type.GetField( fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
						if ( field != null )
							break;

						type = type.BaseType;
					}
				}
				return field;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( string.Format( "Failed to get entity field '{0}'", fieldName ) );
				return null;
			}
		}

		internal static MethodInfo GetStaticMethod( Type objectType, string methodName )
		{
			try
			{
				if ( string.IsNullOrEmpty( methodName ) )
					throw new Exception( "Method name was empty" );
				MethodInfo method = objectType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy );
				if ( method == null )
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = objectType;
					while ( type != typeof( Object ) )
					{
						method = type.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy );
						if ( method != null )
							break;

						type = type.BaseType;
					}
				}
				if ( method == null )
					throw new Exception( "Method not found" );
				return method;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get static method '" + methodName + "'" );
				return null;
			}
		}

		internal static MethodInfo GetStaticMethod( Type objectType, string methodName, Type[ ] argTypes )
		{
			try
			{
				if ( argTypes == null || argTypes.Length == 0 )
					return GetStaticMethod( objectType, methodName );

				if ( string.IsNullOrEmpty( methodName ) )
					throw new Exception( "Method name was empty" );
				MethodInfo method = objectType.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null );
				if ( method == null )
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = objectType;
					while ( type != typeof( Object ) )
					{
						method = type.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null );
						if ( method != null )
							break;

						type = type.BaseType;
					}
				}
				if ( method == null )
					throw new Exception( "Method not found" );
				return method;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get static method '" + methodName + "'" );
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod( Object gameEntity, string methodName )
		{
			try
			{
				if ( gameEntity == null )
					throw new Exception( "Game entity was null" );
				if ( string.IsNullOrEmpty( methodName ) )
					throw new Exception( "Method name was empty" );
				MethodInfo method = gameEntity.GetType( ).GetMethod( methodName );
				if ( method == null )
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType( );
					while ( type != typeof( Object ) )
					{
						method = type.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );
						if ( method != null )
							break;

						type = type.BaseType;
					}
				}
				if ( method == null )
					throw new Exception( "Method not found" );
				return method;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get entity method '" + methodName + "': " + ex.Message );
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod( Object gameEntity, string methodName, Type[ ] argTypes )
		{
			try
			{
				if ( argTypes == null || argTypes.Length == 0 )
					return GetEntityMethod( gameEntity, methodName );

				if ( gameEntity == null )
					throw new Exception( "Game entity was null" );
				if ( string.IsNullOrEmpty( methodName ) )
					throw new Exception( "Method name was empty" );
				MethodInfo method = gameEntity.GetType( ).GetMethod( methodName, argTypes );
				if ( method == null )
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType( );
					while ( type != typeof( Object ) )
					{
						method = type.GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null );
						if ( method != null )
							break;

						type = type.BaseType;
					}
				}
				if ( method == null )
					throw new Exception( "Method not found" );
				return method;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get entity method '" + methodName + "': " + ex.Message );
				return null;
			}
		}

		internal static Object GetStaticFieldValue( Type objectType, string fieldName )
		{
			try
			{
				FieldInfo field = GetStaticField( objectType, fieldName );
				if ( field == null )
					return null;
				Object value = field.GetValue( null );
				return value;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( ex.Message, ex );
				return null;
			}
		}

		internal static void SetStaticFieldValue( Type objectType, string fieldName, Object value )
		{
			try
			{
				FieldInfo field = GetStaticField( objectType, fieldName );
				if ( field == null )
					return;
				field.SetValue( null, value );
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( ex.Message, ex );
			}
		}

		internal static Object GetEntityFieldValue( Object gameEntity, string fieldName )
		{
			try
			{
				FieldInfo field = GetEntityField( gameEntity, fieldName );
				if ( field == null )
					return null;
				Object value = field.GetValue( gameEntity );
				return value;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( ex.Message, ex );
				return null;
			}
		}

		internal static void SetEntityFieldValue( Object gameEntity, string fieldName, Object value )
		{
			try
			{
				FieldInfo field = GetEntityField( gameEntity, fieldName );
				if ( field == null )
					return;
				field.SetValue( gameEntity, value );
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( ex.Message, ex );
			}
		}

		internal static Object InvokeStaticMethod( Type objectType, string methodName )
		{
			return InvokeStaticMethod( objectType, methodName, new object[ ] { } );
		}

		internal static Object InvokeStaticMethod( Type objectType, string methodName, Object[ ] parameters )
		{
			try
			{
				MethodInfo method = GetStaticMethod( objectType, methodName );
				if ( method == null )
					throw new Exception( "Method is empty" );
				Object result = method.Invoke( null, parameters );

				return result;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to invoke static method '" + methodName + "': " + ex.Message );
				return null;
			}
		}

		internal static Object InvokeEntityMethod( Object gameEntity, string methodName )
		{
			return InvokeEntityMethod( gameEntity, methodName, new object[ ] { } );
		}

		internal static Object InvokeEntityMethod( Object gameEntity, string methodName, Object[ ] parameters )
		{
			return InvokeEntityMethod( gameEntity, methodName, parameters, null );
		}

		internal static Object InvokeEntityMethod( Object gameEntity, string methodName, Object[ ] parameters, Type[ ] argTypes )
		{
			try
			{
				MethodInfo method = GetEntityMethod( gameEntity, methodName, argTypes );
				if ( method == null )
					throw new Exception( "Method is empty" );
				Object result = method.Invoke( gameEntity, parameters );

				return result;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to invoke entity method '" + methodName + "' on type '" + gameEntity.GetType( ).FullName + "': " + ex.Message );
				return null;
			}
		}

		internal static PropertyInfo GetEntityProperty( Object gameEntity, string propertyName )
		{
			try
			{
				PropertyInfo property = gameEntity.GetType( ).GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy ) ??
										gameEntity.GetType( ).BaseType.GetProperty( propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy );

				return property;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get entity property '" + propertyName + "'" );
				return null;
			}
		}

		internal static Object GetEntityPropertyValue( Object gameEntity, string propertyName )
		{
			try
			{
				PropertyInfo property = GetEntityProperty( gameEntity, propertyName );
				if ( property == null )
					return null;

				Object result = property.GetValue( gameEntity, null );
				return result;
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to get entity property value '" + propertyName + "'" );
				return null;
			}
		}

		internal static void SetEntityPropertyValue( Object gameEntity, string propertyName, Object value )
		{
			try
			{
				PropertyInfo property = GetEntityProperty( gameEntity, propertyName );
				if ( property == null )
					return;

				property.SetValue( gameEntity, value, null );
			}
			catch ( Exception ex )
			{
				ApplicationLog.Error( "Failed to set entity property value '" + propertyName + "'" );
				if ( SandboxGameAssemblyWrapper.IsDebugging )
				return;
			}
		}
	}
}
