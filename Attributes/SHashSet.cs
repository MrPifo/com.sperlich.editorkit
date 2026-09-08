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
			_items.Clear();
			_items.AddRange(this);
		}

		public void OnAfterDeserialize() {
			Clear();
			foreach (T item in _items) {
				if (item != null) Add(item);
			}
		}
	}
}
