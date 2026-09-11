using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>
	/// A <see cref="HashSet{T}"/> Unity can serialize. Backed by a <c>[SerializeField]</c> list that the
	/// inspector edits; the live set is rebuilt on deserialize. Duplicate / null entries are dropped
	/// silently (the editor drawer flags them first).
	///
	/// <code>
	/// [SInspector] class Tags : MonoBehaviour { public SHashSet&lt;string&gt; ids = new(); }
	/// </code>
	/// </summary>
	[Serializable]
	public class SHashSet<T> : HashSet<T>, ISerializationCallbackReceiver {

		[SerializeField] private List<T> _items = new();

		public SHashSet() { }
		public SHashSet(IEnumerable<T> source) : base(source) { }

		public void OnBeforeSerialize() {
			// Leave the serialized list untouched whenever deserializing it again would reproduce exactly this
			// set. That deliberately tolerates duplicate / extra rows the inspector is mid-edit on — the
			// drawer flags those instead of us silently deleting them here (which would also invalidate the
			// bound editor fields and throw). Rebuild from the live set only when runtime code mutated it.
			if (ListEncodesSet()) return;
			_items.Clear();
			_items.AddRange(this);
		}

		/// <summary>True when running <see cref="OnAfterDeserialize"/> on the current list would rebuild an
		/// identical set — i.e. the list still faithfully encodes this set even with duplicate / surplus
		/// rows.</summary>
		private bool ListEncodesSet() {
			var reconstructed = new HashSet<T>();
			foreach (T item in _items) {
				if (item != null) reconstructed.Add(item);
			}
			if (reconstructed.Count != Count) return false;
			foreach (T item in this) {
				if (!reconstructed.Contains(item)) return false;
			}
			return true;
		}

		public void OnAfterDeserialize() {
			Clear();
			foreach (T item in _items) {
				if (item != null) Add(item);
			}
		}
	}
}
