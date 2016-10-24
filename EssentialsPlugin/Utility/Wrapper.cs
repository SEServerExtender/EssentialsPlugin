namespace EssentialsPlugin.Utility
{
	using System;
	using System.Reflection;
	using NLog;
	using SEModAPIInternal.API.Common;

	public static class Wrapper
	{
		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		public static void GameAction( Action action )
		{
			SandboxGameAssemblyWrapper.Instance.GameAction(action);
		}

		public static void BeginGameAction(Action action, SandboxGameAssemblyWrapper.GameActionCallback callback, object state)
		{
			SandboxGameAssemblyWrapper.Instance.BeginGameAction(action, callback, state);
		}

		internal static FieldInfo GetStaticField(Type objectType, string fieldName)
		{
			try
			{
				FieldInfo field = objectType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy) ??
				                  objectType.BaseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				return field;
			}
			catch (Exception ex)
			{
				Log.Info( "Failed to get static field '{0}'", fieldName );

				Log.Error(ex);
				return null;
			}
		}

		internal static FieldInfo GetEntityField(object gameEntity, string fieldName)
		{
			try
			{
				FieldInfo field = gameEntity.GetType().GetField(fieldName);
				if (field == null)
				{
					//Recurse up through the class heirarchy to try to find the field
					Type type = gameEntity.GetType();
					while (type != typeof(object))
					{
						field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
						if (field != null)
							break;

						type = type.BaseType;
					}
				}
				return field;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get entity field '" + fieldName + "'");

				Log.Error( ex );
				return null;
			}
		}

		internal static MethodInfo GetStaticMethod(Type objectType, string methodName)
		{
			try
			{
				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = objectType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = objectType;
					while (type != typeof(object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get static method '" + methodName + "'");

				Log.Error( ex );
				return null;
			}
		}

		internal static MethodInfo GetStaticMethod(Type objectType, string methodName, Type[] argTypes)
		{
			try
			{
				if (argTypes == null || argTypes.Length == 0)
					return GetStaticMethod(objectType, methodName);

				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = objectType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = objectType;
					while (type != typeof(object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get static method '" + methodName + "'");

				Log.Error( ex );
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod(object gameEntity, string methodName)
		{
			try
			{
				if (gameEntity == null)
					throw new Exception("Game entity was null");
				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = gameEntity.GetType().GetMethod(methodName);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType();
					while (type != typeof(object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get entity method '" + methodName + "': " + ex.Message);

				Log.Error( ex );
				return null;
			}
		}

		internal static MethodInfo GetEntityMethod(object gameEntity, string methodName, Type[] argTypes)
		{
			try
			{
				if (argTypes == null || argTypes.Length == 0)
					return GetEntityMethod(gameEntity, methodName);

				if (gameEntity == null)
					throw new Exception("Game entity was null");
				if (methodName == null || methodName.Length == 0)
					throw new Exception("Method name was empty");
				MethodInfo method = gameEntity.GetType().GetMethod(methodName, argTypes);
				if (method == null)
				{
					//Recurse up through the class heirarchy to try to find the method
					Type type = gameEntity.GetType();
					while (type != typeof(object))
					{
						method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
						if (method != null)
							break;

						type = type.BaseType;
					}
				}
				if (method == null)
					throw new Exception("Method not found");
				return method;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get entity method '" + methodName + "': " + ex.Message);

				Log.Error( ex );
				return null;
			}
		}

		internal static object GetStaticFieldValue(Type objectType, string fieldName)
		{
			try
			{
				FieldInfo field = GetStaticField(objectType, fieldName);
				if (field == null)
					return null;
				object value = field.GetValue(null);
				return value;
			}
			catch (Exception ex)
			{
				Log.Error( ex );
				return null;
			}
		}

		internal static void SetStaticFieldValue(Type objectType, string fieldName, object value)
		{
			try
			{
				FieldInfo field = GetStaticField(objectType, fieldName);
				if (field == null)
					return;
				field.SetValue(null, value);
			}
			catch (Exception ex)
			{
				Log.Error( ex );
			}
		}

		internal static object GetEntityFieldValue(object gameEntity, string fieldName)
		{
			try
			{
				FieldInfo field = GetEntityField(gameEntity, fieldName);
				if (field == null)
					return null;
				object value = field.GetValue(gameEntity);
				return value;
			}
			catch (Exception ex)
			{
				Log.Error( ex );
				return null;
			}
		}

		internal static void SetEntityFieldValue(object gameEntity, string fieldName, object value)
		{
			try
			{
				FieldInfo field = GetEntityField(gameEntity, fieldName);
				if (field == null)
					return;
				field.SetValue(gameEntity, value);
			}
			catch (Exception ex)
			{
				Log.Error( ex );
			}
		}

		internal static object InvokeStaticMethod(Type objectType, string methodName)
		{
			return InvokeStaticMethod(objectType, methodName, new object[] { });
		}

		internal static object InvokeStaticMethod(Type objectType, string methodName, object[] parameters)
		{
			try
			{
				MethodInfo method = GetStaticMethod(objectType, methodName);
				if (method == null)
					throw new Exception("Method is empty");
				object result = method.Invoke(null, parameters);

				return result;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to invoke static method '" + methodName + "': " + ex.Message);

				Log.Error( ex );
				return null;
			}
		}

		internal static object InvokeEntityMethod(object gameEntity, string methodName)
		{
			return InvokeEntityMethod(gameEntity, methodName, new object[] { });
		}

		internal static object InvokeEntityMethod(object gameEntity, string methodName, object[] parameters)
		{
			return InvokeEntityMethod(gameEntity, methodName, parameters, null);
		}

		internal static object InvokeEntityMethod(object gameEntity, string methodName, object[] parameters, Type[] argTypes)
		{
			try
			{
				MethodInfo method = GetEntityMethod(gameEntity, methodName, argTypes);
				if (method == null)
					throw new Exception("Method is empty");
				object result = method.Invoke(gameEntity, parameters);

				return result;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to invoke entity method '" + methodName + "' on type '" + gameEntity.GetType().FullName + "': " + ex.Message);


				Log.Error( ex );
				return null;
			}
		}

		internal static PropertyInfo GetEntityProperty(object gameEntity, string propertyName)
		{
			try
			{
				PropertyInfo property = gameEntity.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy) ??
				                        gameEntity.GetType().BaseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

				return property;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get entity property '" + propertyName + "'");

				Log.Error( ex );
				return null;
			}
		}

		internal static object GetEntityPropertyValue(object gameEntity, string propertyName)
		{
			try
			{
				PropertyInfo property = GetEntityProperty(gameEntity, propertyName);
				if (property == null)
					return null;

				object result = property.GetValue(gameEntity, null);
				return result;
			}
			catch (Exception ex)
			{
				Log.Info("Failed to get entity property value '" + propertyName + "'");

				Log.Error( ex );
				return null;
			}
		}

		internal static void SetEntityPropertyValue(object gameEntity, string propertyName, object value)
		{
			try
			{
				PropertyInfo property = GetEntityProperty(gameEntity, propertyName);
				if (property == null)
					return;

				property.SetValue(gameEntity, value, null);
			}
			catch (Exception ex)
			{
				Log.Info("Failed to set entity property value '" + propertyName + "'");

				Log.Error( ex );
				return;
			}
		}
	}
}
