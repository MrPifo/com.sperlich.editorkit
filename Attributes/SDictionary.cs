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
			// Leave the serialized lists untouched whenever deserializing them again would reproduce exactly
			// this dictionary. That deliberately tolerates duplicate / extra rows the inspector is mid-edit
			// on (e.g. a key just typed onto a value that already exists) — the drawer flags those rows
			// instead of us silently deleting them here, which would also invalidate the bound editor fields
			// and throw. Rebuild from the live dictionary only when runtime code has mutated it directly.
			if (ListsEncodeDict()) return;
			_keys.Clear();
			_values.Clear();
			foreach (KeyValuePair<TKey, TValue> kv in this) {
				_keys.Add(kv.Key);
				_values.Add(kv.Value);
			}
		}

		/// <summary>True when running <see cref="OnAfterDeserialize"/> on the current lists would rebuild an
		/// identical dictionary (last write wins, null keys skipped) — i.e. the lists still faithfully encode
		/// this dictionary even if they carry duplicate or surplus rows.</summary>
		private bool ListsEncodeDict() {
			int n = Math.Min(_keys.Count, _values.Count);
			var reconstructed = new Dictionary<TKey, TValue>(n);
			for (int i = 0; i < n; i++) {
				TKey k = _keys[i];
				if (k == null) continue;
				reconstructed[k] = _values[i];
			}
			if (reconstructed.Count != Count) return false;
			foreach (KeyValuePair<TKey, TValue> kv in this) {
				if (!reconstructed.TryGetValue(kv.Key, out TValue v)) return false;
				if (!EqualityComparer<TValue>.Default.Equals(v, kv.Value)) return false;
			}
			return true;
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
