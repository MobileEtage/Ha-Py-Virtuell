using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Fenderrio.ImageWarp
{
	[CustomEditor( typeof(NativeTextureRenderer) )]
	public class NativeTextureRendererEditor : Editor {

		private NativeTextureRenderer m_spriteRenderer;
		private NativeTextureRenderer spriteRenderer {
			get {
				if (m_spriteRenderer == null)
				{
					m_spriteRenderer = (NativeTextureRenderer) target;
				}
				return m_spriteRenderer;
			}
		}

		private void OnEnable()
		{
			spriteRenderer.MeshRendererCache.hideFlags = HideFlags.HideInInspector;
			spriteRenderer.MeshFilterCache.hideFlags = HideFlags.HideInInspector;
		}

		public override void OnInspectorGUI ()
		{
			// Draw default inspector content
//			base.OnInspectorGUI();

			EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_texture"));
			EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_colour"));

			serializedObject.ApplyModifiedProperties ();

			if (GUI.changed)
			{
				spriteRenderer.UpdateMesh ();
			}
		}
	}
}