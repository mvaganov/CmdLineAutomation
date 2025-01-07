using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommandAssetExecutionStack
{
	public struct AssetContextKey {
		public object asset, context;
		public AssetContextKey(object asset, object context) { this.asset = asset; this.context = context; }
		public override bool Equals(object obj) => obj is AssetContextKey ack && ack.asset == asset && ack.context == context;
		public override int GetHashCode() => asset.GetHashCode() ^ context.GetHashCode();
		public override	string ToString() => asset.ToString() + context.ToString();
		public static implicit operator AssetContextKey((object a, object c) tuple) => new AssetContextKey(tuple.a, tuple.c);
	}
	private static Dictionary<AssetContextKey, object> _stack = new Dictionary<AssetContextKey, object> ();

	public static void SetData(object asset, object context, object data) {
		AssetContextKey key = (asset, context);
		_stack[key] = data;
	}

	public static bool TryGetData(object asset, object context, out object data) {
		return _stack.TryGetValue((asset, context), out data);
	}

	public static TYPE GetDataIfMissing<TYPE>(object asset, object context, Func<TYPE> howToCreateIfMissing) where TYPE : class {
		if (!TryGetData(asset, context, out object data)) {
			data = howToCreateIfMissing();
		}
		return data;
	}
}
