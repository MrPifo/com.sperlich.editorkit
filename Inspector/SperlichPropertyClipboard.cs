using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Copy / Paste for any serialized property, through Unity's own (internal) property clipboard. Values
	/// copied here paste into native inspector fields and the other way round.
	/// </summary>
	internal static class SperlichPropertyClipboard {

		private static readonly Type ClipboardType = typeof(Editor).Assembly.GetType("UnityEditor.Clipboard");
		private static readonly MethodInfo SetMethod = Find("SetSerializedProperty");
		private static readonly MethodInfo GetMethod = Find("GetSerializedProperty");
		private static readonly MethodInfo HasMethod = Find("HasSerializedProperty");

		private static bool Available => SetMethod != null && GetMethod != null && HasMethod != null;

		private static MethodInfo Find(string name) =>
			ClipboardType?.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		public static void AppendCopyPaste(DropdownMenu menu, SerializedProperty prop, bool editable, Action afterPaste) {
			if (!Available) return;
			SerializedProperty target = prop.Copy();

			menu.AppendAction("Copy", _ => Invoke(SetMethod, target), DropdownMenuAction.AlwaysEnabled);
			menu.AppendAction("Paste", _ => {
				Invoke(GetMethod, target);
				target.serializedObject.ApplyModifiedProperties();
				afterPaste?.Invoke();
			}, _ => editable && CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
			menu.AppendSeparator();
		}

		private static bool CanPaste() {
			try { return (bool)HasMethod.Invoke(null, null); } catch { return false; }
		}

		private static void Invoke(MethodInfo method, SerializedProperty prop) {
			try { method.Invoke(null, new object[] { prop }); }
			catch (Exception e) { Debug.LogException(e.InnerException ?? e); }
		}
	}
}
