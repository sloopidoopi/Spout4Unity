/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015 Stefan Schlupek
* All rights reserved
*/
using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace Spout{
	[CustomEditor(typeof(Spout))]
	[CanEditMultipleObjects]
	[Serializable]
	public class SpoutEditor : Editor {
		
		private Spout _target;
		private SerializedProperty enabled_Prop;

		void OnEnable()
		{
			//Debug.Log("SpoutEditor.OnEnable");
			if(target != _target) _target = target as Spout;
			enabled_Prop = serializedObject.FindProperty("_isEnabledInEditor");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();

			if(!EditorApplication.isPlaying){
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField(enabled_Prop,new GUIContent("Editor Enabled","Editor Enabled"));
				serializedObject.ApplyModifiedProperties();
				if (EditorGUI.EndChangeCheck ()) {
					if(enabled_Prop.boolValue){
						if(_target.enabled && _target.gameObject.activeInHierarchy)_target.OnEnable();
					}else{
						_target.OnDisable();
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
