/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015 Stefan Schlupek
* All rights reserved
*/
using UnityEngine;
using System.Collections;
using System;

namespace Spout{
[Serializable]
	[ExecuteInEditMode]
	public class SpoutReceiver : MonoBehaviour {
		
		[SerializeField]
		private string _sharingName;

		private Texture2D _texture;
		// leave false in this version!
		//private bool debugConsole = false;

		private bool _receiverIsReady;

		//If you have want to prevent the Console warnings about not existing Senders  just turn on the StartupDelay and specify the frame delay
		private  bool _startupDelay = true;
		//how many frames of delay 
		private  int _startupFramesDelay = 0;


		private int _startUpFrameCount;


		private string _tempName;


		// Use this for initialization
		void Awake () {
		//	Debug.Log("SpoutReceiver.Awake");
		//if(debugConsole) Spout2.instance.initDebugConsole();

		}
		
		void Start()
		{

		}
		void OnEnable(){
			//Debug.Log("SpoutReceiver.OnEnable");
			//Add the listener immediately to get the events when Unity starts and the Spout plugin initialize. 
			//Otherwise the external Spout Senders have to start afterwards to trigger the native plugin events
			Spout.instance.addListener(TexShared,TexStopped);

			#if UNITY_EDITOR
			
			if(!Application.isPlaying){
				UnityEditor.EditorApplication.update -= _Update;
				UnityEditor.EditorApplication.update += _Update;
			}
			#endif

			_receiverIsReady = false;
			_startUpFrameCount = 0;
			if(!_startupDelay)_ForceTextureUpdate();
			
		}

		void OnDisable(){
			//Debug.Log("SpoutReceiver.OnDisable");
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= _Update;
			#endif
			_receiverIsReady = false;
			_startUpFrameCount = 0;

			Spout.removeListener(TexShared,TexStopped);
			texture = null;
			GC.Collect();
		}

		void OnDestroy(){
			//Debug.Log("SpoutReceiver.OnDestroy");
		}
		

		void Update(){
			_Update();
		}

		// Update is called once per frame
		void _Update () {
			//if(texture == null)_createNullTexture();
			if(!_startupDelay)return;
			if(!_receiverIsReady){
				_startUpFrameCount++;
				if( _startUpFrameCount < _startupFramesDelay)return;
				_ForceTextureUpdate();
			}
			
		}

		private void _ForceTextureUpdate(){
			//Debug.Log("SpoutReceiver._ForceTextureUpdate");

			//Little hack to force an update of the Texture
			_tempName = _sharingName;
			sharingName = Spout.fakeName;
			sharingName = _tempName;
			_receiverIsReady = true;
		}

		public void TexShared(TextureInfo texInfo)
		{
			//Debug.Log("SpoutReceiver.texShared");
			if(sharingName == "" || sharingName == texInfo.name || sharingName == "Any")
			{
				//Debug.Log("SpoutReceiver.texShared:"+texInfo.name);
				texture = texInfo.getTexture();
			}
		}
		
		public void  TexStopped(TextureInfo texInfo)
		{
			//Debug.Log("SpoutReceiver.texStopped:"+texInfo.name);
			if(texInfo.name == _sharingName)
			{		
				//Debug.Log("SpoutReceiver.texStopped:"+texInfo.name);
				texture = Spout.nullTexture;

				
			}
			else if(sharingName == "Any" && Spout.instance.activeSenders.Count > 0)
			{
				texture = Spout.instance.activeSenders[Spout.instance.activeSenders.Count-1].getTexture();
			}
		}
		
		public Texture2D texture
		{
			get { return _texture; }
			set {
				_texture = value;
				if(_texture == null) _texture = Spout.nullTexture;
				if(GetComponent<Renderer>() != null)
				{
					GetComponent<Renderer>().sharedMaterial.mainTexture = _texture;
				}
			}
		}
		[SerializeField]
		public string sharingName
		{
			get { return _sharingName; }
			set {
				if(_sharingName == value && sharingName != "Any") return;
				_sharingName = value;
				//Debug.Log("sharingName:"+_sharingName);
				if(sharingName == "Any")
				{ 
					if(Spout.instance.activeSenders != null && Spout.instance.activeSenders.Count > 0)
					{
						texture = Spout.instance.activeSenders[Spout.instance.activeSenders.Count-1].getTexture();
					}
				}else
				{
					//Debug.Log ("Set sharing name :"+sharingName);
					TextureInfo texInfo = Spout.instance.getTextureInfo(sharingName);
					if(texInfo != null) {
						texture = texInfo.getTexture ();
					}
					else
					{
						if(sharingName != Spout.fakeName)Debug.LogWarning ("Sender "+sharingName+" does not exist");
						texture = Spout.nullTexture;
						/*
						texture = new Texture2D(32,32);
						//new Texture2D(32,32,TextureFormat.RGBA32,true,true);
						texture.hideFlags = HideFlags.HideAndDontSave;
						*/
					}
					
				}

			}
		}


		
		
	}
}
