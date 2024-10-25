using System;
using System.Collections;
using System.Collections.Generic;
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
			private static void UnityLog(object message) {
#if UNITY_EDITOR
				Debug.Log(message);
#endif
			}

			private static void UnityWarn(object message) {
#if UNITY_EDITOR
				Debug.LogWarning(message);
#endif
			}

			private static void UnityErr(object message) {
#if UNITY_EDITOR
				Debug.LogError(message);
#endif
			}

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
					UnityErr(parseResult);
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
					UnityErr($"??? {targetType}");
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
					object name = kvp.Key.ToString();
					if (!TryGetValueStructured(targetObject, name, out object value, out Type valueType)) {
						UnityErr($"missing {name} in type {targetType}");
					}
					TryAssign(ref value, valueType, kvp.Value);
					TrySetValueStructured(targetObject, name, value);
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
			/// Set a value by member name, or possibly <see cref="IDictionary"/>/<see cref="IList"/>
			/// </summary>
			/// <param name="self"></param>
			/// <param name="member"></param>
			/// <param name="memberType"></param>
			/// <returns></returns>
			public static bool TryGetValueStructured(object self, object member, out object memberValue, out Type memberType) {
				if (self == null) {
					memberValue = memberType = null;
					return false;
				}
				Type type = self.GetType();
				if (self is IDictionary dict) {
					if (dict.Contains(member)) {
						memberValue = dict[member];
						Type[] arguments = dict.GetType().GetGenericArguments();
						memberType = arguments.Length > 1 ? arguments[1] : typeof(object);
						return true;
					}
				} else if (self is IList ilist) {
					int index = Convert.ToInt32(member);
					if (index < 0 || index >= ilist.Count) {
						memberValue = memberType = null;
						return false;
					}
					memberValue = ilist[index];
					memberType = ilist.GetType().GetElementType();
					return true;
				}
				return TryGetValue(self, member.ToString(), out memberValue, out memberType);
			}

			/// <summary>
			/// Use reflection to get a value by the member name
			/// </summary>
			/// <param name="self"></param>
			/// <param name="memberName"></param>
			/// <param name="memberType"></param>
			/// <returns></returns>
			public static bool TryGetValue(object self, string memberName, out object memberValue, out Type memberType) {
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
			/// Set a value by member name, or possibly <see cref="IDictionary"/>/<see cref="IList"/>
			/// </summary>
			/// <param name="obj"></param>
			/// <param name="memberName"></param>
			/// <param name="value"></param>
			/// <returns></returns>
			public static bool TrySetValueStructured(object obj, object memberName, object value) {
				if (obj is IDictionary dict) {
					// Debug.Log($"SETTING '{memberName}'");
					dict[memberName] = value;
					return true;
				} else if (obj is IList list) {
					int index = Convert.ToInt32(memberName);
					if (index == list.Count) {
						if (list.IsFixedSize) {
							UnityErr("probably unable to add to end of list");
						}
						list.Add(value);
						return true;
					} else if (index >= 0 && index < list.Count) {
						list[index] = value;
						return true;
					}
					UnityErr($"{index} is OOB ({list.Count})");
					return false;
				}
				return TrySetValue(obj, memberName.ToString(), value);
			}

			/// <summary>
			/// Use reflection to set a value by the member name
			/// </summary>
			/// <param name="obj"></param>
			/// <param name="memberName"></param>
			/// <param name="value"></param>
			/// <returns></returns>
			public static bool TrySetValue(object obj, string memberName, object value) {
				if (obj == null) { return false; }
				Type type = obj.GetType();
				BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				while (type != null) {
					FieldInfo field = type.GetField(memberName, bindFlags);
					if (field != null) {
						field.SetValue(obj, value);
						return true;
					}
					PropertyInfo prop = type.GetProperty(memberName, bindFlags | BindingFlags.IgnoreCase);
					if (prop != null) {
						prop.SetValue(obj, null);
						return true;
					}
					type = type.BaseType;
				}
				UnityErr($"could not set ({obj})[{memberName}]");
				return false;
			}

			/// <summary>
			/// List 
			/// </summary>
			/// <param name="path"></param>
			/// <returns></returns>
			public static string ShowList(IList<object> path) {
				string listStr = string.Join(",", path);
				return $"[{listStr}]{(listStr.Length == 0 && path.Count != 0 ? "(NOT EMPTY)" : "")}";
			} 

			/// <summary>
			/// Set a value in a branching data structure
			/// </summary>
			/// <param name="rootObj">Root data structure</param>
			/// <param name="ids">Path of member variables to traverse, including member that needs to be set</param>
			/// <param name="value">value to apply to the member at the end of the given member path</param>
			/// <returns></returns>
			public static bool TrySet(object rootObj, IList<object> ids, object value) {
				if (ids == null || ids.Count == 0) {
					UnityErr("Is this trying to reset the root object? Shouldn't that be handled in the previous function?");
					return false;
				}
				if (!TryTraverse(rootObj, ids, out object objectWithMember, out Type branchType, 0, ids.Count - 1)) {
					UnityErr("FAIL!");
					return false;
				}
				object memberId = ids[ids.Count - 1];
				switch (memberId) {
					case Token token:
					case string text:
					case int index:
						Object.TryGetValueStructured(objectWithMember, memberId, out object currentValue, out _);
						//if (currentValue == value) {
						//	UnityLog($"{ShowList(ids)} set correctly");
						//} else {
						//	UnityWarn($"Setting {ShowList(ids)}\n{objectWithMember}[{memberId}] = {value}");
						//}
						return Object.TrySetValueStructured(objectWithMember, memberId, value);
					default:
						UnityWarn($"what is this?! {memberId} ({memberId.GetType()})");
						break;
				}
				return false;
			}

			public static bool TryGet(object obj, IList<object> memberPath, out object memberValue) {
				return TryTraverse(obj, memberPath, out memberValue, out _);
			}

			public static bool TryTraverse(object rootObject, IList<object> ids, out object memberValue, out Type memberType, int idIndexStart = 0, int idIndexEnd = -1) {
				if (rootObject == null) {
					memberValue = memberType = null;
					UnityErr($"cannot traverse from null object\n{ShowList(ids)}");
					return false;
				}
				if (idIndexEnd < 0) {
					idIndexEnd = ids.Count;
				}
				object cursor = memberValue = rootObject;
				memberType = memberValue != null ? memberValue.GetType() : null;
				for (int i = idIndexStart; i < idIndexEnd; ++i) {
					switch (ids[i]) {
						case string text:
						case Token token:
						case int index:
							object memberName = ids[i];
							if (!Object.TryGetValueStructured(cursor, memberName, out memberValue, out memberType)) {
								UnityErr($"{Parse.ToString(cursor)} does not have member '{Parse.ToString(memberName)}'\n{ShowList(ids)}");
								return false;
							}
							cursor = memberValue;
							break;
						default:
							UnityErr($"{ids[i]} is not a traversable type ({ids[i].GetType()})\n{ShowList(ids)}");
							return false;
					}
				}
				return true;
			}
		}
	}
}
