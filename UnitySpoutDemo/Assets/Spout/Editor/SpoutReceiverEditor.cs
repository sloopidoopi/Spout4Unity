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
	[CustomEditor(typeof(SpoutReceiver))]
	[CanEditMultipleObjects]
	[Serializable]
	public class SpoutReceiverEditor : Editor {

		SpoutReceiver receiver;
		
		[SerializeField]
		private int _popupIndex;
		
		string[] options;
		
		string currentName;

		
		void OnEnable()
		{

			if(receiver == null)
			{
				receiver = target as SpoutReceiver;
				Spout.instance.addListener(texShared,senderStopped);

				updateOptions();
			}


			//Debug.Log ("Enable popup Index : "+popupIndex);
		}

		void OnDisable(){
			//if(target == null)return;
			Spout.removeListener(texShared,senderStopped);
		}
		
		public void texShared(TextureInfo texInfo)
		{
			//Debug.Log ("Editor : senderStarted :"+texInfo.name);
			updateOptions();
		}
		
		public void senderStopped(TextureInfo texInfo)
		{
			//Debug.Log ("Editor : senderStopped :"+texInfo.name);
			updateOptions();
		}
		
		void updateOptions(bool assignNewName = true)
		{
			
			string oldSharingName = receiver.sharingName;
			int newPopupIndex = 0;
			
			bool found = false;
			
			options = new string[Spout.instance.activeSenders.Count+2];
			options[0] = "Any";
			
			for(int i=0;i<Spout.instance.activeSenders.Count;i++)
			{
				string currentName = Spout.instance.activeSenders[i].name;
				options[i+1] = currentName;
				if(currentName == oldSharingName && oldSharingName != "Any") 
				{
					found = true;
					newPopupIndex = i+1;
				}
			}
			
			options[options.Length-1] = "Other (specify)";
			
			if(!found)
			{
				newPopupIndex =  (oldSharingName != "Any")?options.Length-1:0;
			}
			
			popupIndex = newPopupIndex;
			setNameFromPopup();
			
		}
		
		public override void OnInspectorGUI()
		{
			//if(target == null)return;
			//if(Spout2.instance == null)return;
			//serializedObject.Update();

			int selectedIndex = EditorGUILayout.Popup("Select sender",_popupIndex,options);
			
			if(selectedIndex == options.Length-1)//other
			{
				_popupIndex = selectedIndex; //silent update
				receiver.sharingName = EditorGUILayout.TextField("Sender name",receiver.sharingName);
			} else
			{
		 		popupIndex = selectedIndex;
			}
			
			//receiver.debugConsole =  EditorGUILayout.Toggle("Debug Console",receiver.debugConsole);

			serializedObject.ApplyModifiedProperties();
		}
		
		void setNameFromPopup()
		{
			if(popupIndex < options.Length -1)
			{
				receiver.sharingName = options[popupIndex];
			}
		}
		
		int popupIndex
		{
			get { return _popupIndex;}
			set {
				if(popupIndex == value) return;
				_popupIndex = value;
				setNameFromPopup();
			}
		}
	}
}
