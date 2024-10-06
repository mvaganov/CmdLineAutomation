using System;
using System.Collections;
using System.Reflection;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace RunCmd {
	/// <summary>
	/// Reflectively applies parsed data to compiled objects.
	/// Use this sparingly, because C# reflection caches reflection tables, akin
	/// to creating a memory leak for the entire app's lifecycle.
	/// </summary>
	public static partial class Parse {
		public static partial class Object {
			/// <param name="targetObject">cannot be null</param>
			/// <param name="text"></param>
			/// <param name="resultData"></param>
			/// <returns></returns>
			public static bool TryParse(ref object targetObject, string text, out Parse.ParseResult resultData) {
				if (targetObject == null) {
					resultData = new Parse.ParseResult(ParseResult.Kind.MissingTarget, -1);
					return false;
				}
				return TryParse(ref targetObject, targetObject.GetType(), text, out resultData);
			}

			/// <param name="targetObject">can be null</param>
			/// <param name="text"></param>
			/// <param name="resultData"></param>
			/// <returns></returns>
			public static bool TryParse(ref object targetObject, Type targetType, string textToParse, out Parse.ParseResult parseResult) {
				object parsedData = ParseText(textToParse, out parseResult);
				if (parseResult.IsError) {
#if UNITY_EDITOR
					Debug.LogError(parseResult);
#endif
					return false;
				}
				TryAssign(ref targetObject, targetType, parsedData);
				return true;
			}

			/// <summary>
			/// Applies parsed data (a combination of IDictionary, IList, and Token, from
			/// <see cref="ParseText(string, out ParseResult)"/>) to the given target object
			/// </summary>
			/// <param name="targetObject"></param>
			/// <param name="targetType"></param>
			/// <param name="parsedData"></param>
			/// <returns></returns>
			public static bool TryAssign(ref object targetObject, Type targetType, object parsedData) {
				bool isString = targetType == typeof(string);
				if (targetType.IsClass && !isString && !targetType.IsArray) {
					bool isPrimitive = targetType.IsPrimitive || isString;
					if (targetObject == null && !isPrimitive) {
						targetObject = Activator.CreateInstance(targetType);
					}
					return TryCompileObject(ref targetObject, targetType, parsedData);
				} else if (targetType.IsArray) {
					return TryCompileArray(ref targetObject, targetType, parsedData);
				} else if (isString || targetType.IsValueType) {
					return TryCompilePrimitive(ref targetObject, targetType, parsedData);
				} else {
#if UNITY_EDITOR
					Debug.LogError($"??? {targetType}");
#endif
				}
				return false;
			}

			/// <summary>
			/// Applies parsed data (an IDictionary from
			/// <see cref="ParseText(string, out ParseResult)"/>) to the given target object
			/// </summary>
			/// <param name="targetObject"></param>
			/// <param name="targetType"></param>
			/// <param name="parsedData"></param>
			/// <returns></returns>
			private static bool TryCompileObject(ref object targetObject, Type targetType, object parsedData) {
				if (!(parsedData is IDictionary dict)) {
					return false;
				}
				foreach (DictionaryEntry kvp in dict) {
					string name = kvp.Key.ToString();
					if (!TryGetValue(targetObject, name, out object value, out Type valueType)) {
#if UNITY_EDITOR
						Debug.LogError($"missing {name} in type {targetType}");
#endif
					}
					TryAssign(ref value, valueType, kvp.Value);
					TrySetValue(targetObject, name, value);
				}
				return true;
			}

			/// <summary>
			/// Applies parsed data (an IList from
			/// <see cref="ParseText(string, out ParseResult)"/>) to the given target object
			/// </summary>
			/// <param name="targetObject"></param>
			/// <param name="targetType"></param>
			/// <param name="data"></param>
			/// <returns></returns>
			private static bool TryCompileArray(ref object targetObject, Type targetType, object data) {
				if (!(data is IList iList)) {
					return false;
				}
				Type elementType = targetType.GetElementType();
				Array arr = Array.CreateInstance(elementType, iList.Count);
				bool allSuccess = true;
				for (int i = 0; i < iList.Count; ++i) {
					object elementData = iList[i];
					object elementObject = null;
					if (elementType.IsValueType || elementType == typeof(string)) {
						if (!TryCompilePrimitive(ref elementObject, elementType, elementData)) {
							allSuccess = false;
						}
					} else {
						elementObject = Activator.CreateInstance(elementType);
						TryAssign(ref elementObject, elementType, elementData);
					}
					arr.SetValue(elementObject, i);
				}
				targetObject = arr;
				return allSuccess;
			}

			/// <summary>
			/// Applies parsed data (a Token from
			/// <see cref="ParseText(string, out ParseResult)"/>) to the given target object
			/// </summary>
			/// <param name="targetObject"></param>
			/// <param name="targetType"></param>
			/// <param name="parsedData"></param>
			/// <returns></returns>
			private static bool TryCompilePrimitive(ref object targetObject, Type targetType, object parsedData) {
				if (parsedData is Token token) {
					targetObject = Convert.ChangeType(token.Text, targetType);
					return true;
				}
				targetObject = Convert.ChangeType(parsedData.ToString(), targetType);
				return true;
			}

			/// <summary>
			/// Use reflection to get a value by the member name
			/// </summary>
			/// <param name="self"></param>
			/// <param name="memberName"></param>
			/// <param name="memberType"></param>
			/// <returns></returns>
			private static bool TryGetValue(object self, string memberName, out object memberValue, out Type memberType) {
				if (self == null) {
					memberType = typeof(int);
					memberValue = null;
					return false;
				}
				Type type = self.GetType();
				BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				while (type != null) {
					FieldInfo field = type.GetField(memberName, bindFlags);
					if (field != null) {
						memberType = field.FieldType;
						memberValue = field.GetValue(self);
						return true;
					}
					PropertyInfo prop = type.GetProperty(memberName, bindFlags | BindingFlags.IgnoreCase);
					if (prop != null) {
						memberType = prop.PropertyType;
						memberValue = prop.GetValue(self, null);
						return true;
					}
					type = type.BaseType;
				}
				memberType = typeof(byte);
				memberValue = null;
				return false;
			}

			/// <summary>
			/// Use reflection to set a value by the member name
			/// </summary>
			/// <param name="self"></param>
			/// <param name="memberName"></param>
			/// <param name="memberType"></param>
			/// <returns></returns>
			private static bool TrySetValue(object source, string name, object value) {
				if (source == null) { return false; }
				Type type = source.GetType();
				BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				while (type != null) {
					FieldInfo field = type.GetField(name, bindFlags);
					if (field != null) {
						field.SetValue(source, value);
						return true;
					}
					PropertyInfo prop = type.GetProperty(name, bindFlags | BindingFlags.IgnoreCase);
					if (prop != null) {
						prop.SetValue(source, null);
						return true;
					}
					type = type.BaseType;
				}
				return true;
			}
		}
	}
}
