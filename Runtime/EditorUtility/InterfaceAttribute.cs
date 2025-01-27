using UnityEngine;
using System;

namespace RunCmdRedux {
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class InterfaceAttribute : PropertyAttribute {
		public Type InterfaceType = null;
		public string InferTypeFromFieldName;

		public InterfaceAttribute(Type type) {
			InterfaceType = type;
		}

		/// <summary>
		/// Used to match the interface requirement to a field in the same class
		/// </summary>
		/// <param name="fieldName"></param>
		public InterfaceAttribute(string fieldName) {
			InferTypeFromFieldName = fieldName;
		}
	}
}
