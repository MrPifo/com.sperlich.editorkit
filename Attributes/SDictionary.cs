using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>
	/// A <see cref="Dictionary{TKey,TValue}"/> Unity can serialize (Unity ignores real dictionaries). Backed
	/// by two parallel <c>[SerializeField]</c> lists that the inspector edits; the live dictionary is rebuilt
	/// on deserialize. Duplicate or null keys are dropped silently (the editor drawer flags them first).
	///
	/// <code>
	/// [SInspector] class Loot : MonoBehaviour { public SDictionary&lt;string,int&gt; table = new(); }
	/// </code>
	/// </summary>
	[Serializable]
	public class SDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {

		[SerializeField] private List<TKey> _keys = new();
		[SerializeField] private List<TValue> _values = new();

		public SDictionary() { }
		public SDictionary(IDictionary<TKey, TValue> source) : base(source) { }

		public void OnBeforeSerialize() {
			_keys.Clear();
			_values.Clear();
			foreach (KeyValuePair<TKey, TValue> kv in this) {
				_keys.Add(kv.Key);
				_values.Add(kv.Value);
			}
		}

		public void OnAfterDeserialize() {
			Clear();
			int n = Mathf.Min(_keys.Count, _values.Count);
			for (int i = 0; i < n; i++) {
				if (_keys[i] == null) continue;
				this[_keys[i]] = _values[i]; // last duplicate wins
			}
		}
	}
}
