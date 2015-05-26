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
	public class SpoutSender : MonoBehaviour {

        //according to dxgiformat.h :
        //tested with DXGI_FORMAT_R8G8B8A8_UNORM (ATI Card)
        public enum TextureFormat { DXGI_FORMAT_R32G32B32A32_FLOAT = 2, DXGI_FORMAT_R10G10B10A2_UNORM = 24, DXGI_FORMAT_R8G8B8A8_UNORM = 28, DXGI_FORMAT_B8G8R8A8_UNORM=87 }
		public string sharingName = "UnitySender";
		public Texture texture;
        public TextureFormat textureFormat = TextureFormat.DXGI_FORMAT_R8G8B8A8_UNORM;
		public bool debugConsole = false;
		
		private bool senderIsCreated;

		private Camera _cam;

		//make this public if you want
		//It's better you set this always to true!
		//There are problems with creating a sender at OnEnable at Editor/App Start time so we have a little delay hack that calls the CreateSender at Update()
		private  bool StartupDelay = true;
		// if there are problems you can increase this value 
		private  int _startupFramesDelay = 3;

		private int _startUpFrameCount;

		private int _createAttempts= 5;
		private int _attempts= 0;

		#pragma warning disable 414
		//In  EditMode we have to force the camera to render.But as this is called 100 times per second beware of your performance, so we only render at a specific interval
		private int _editorUpdateFrameInterval = 10;
		#pragma warning restore 414

		private int _frameCounter;


		void Awake () {
			//Debug.Log("SpoutSender.Awake");
			//if(debugConsole) Spout2.instance.initDebugConsole();
			#if UNITY_EDITOR
			_cam = GetComponent<Camera>() as Camera;
			#endif
		}
		
		void Start()
		{

		}

		void OnEnable(){

			//Debug.Log("SpoutSender.OnEnable");


			#if UNITY_EDITOR
			
			if(!Application.isPlaying){
				UnityEditor.EditorApplication.update -= _Update;
				UnityEditor.EditorApplication.update += _Update;
			}
			#endif
			if(debugConsole) Spout.instance.initDebugConsole();
			_startUpFrameCount = 0;
			_attempts = 0;

			//Problem with creatingSender and disabled hosting Gameobject.(You always have to call OnEnable twice to get a Sender created)
			//It's better to do the real createSender call at OnUpdate.
			if(!StartupDelay)_CreateSender();

		}

		void OnDisable(){
			Debug.Log("SpoutSender.OnDisable");

			//we can't call  Spout2.instance because on Disable is also called when scene is destroyed.  
			//so a nother instance of Spout could be generated in the moment when the Spout2 instance is destroyed!


			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= _Update;
			#endif

			_CloseSender();
		}

		void Update(){
			_Update();
		}

		void _Update()
		{
			//Debug.Log("SpoutSender.Update");
			if (texture == null) return;

			//in  EditMode we have to force the camera to render.But as this is called 100 times per second beware of your performance, so we only render at a special interval
			#if UNITY_EDITOR
			if(!Application.isPlaying){
				_frameCounter++;
				_frameCounter %= _editorUpdateFrameInterval;
				if(Spout.isEnabledInEditor){
					if(_cam != null){
						if(_frameCounter == 0) 	_cam.Render();			
					}
				}
			}
			#endif

			if(senderIsCreated)
			{
				Spout.instance.UpdateSender(sharingName,texture);
				//Debug.Log("Update sender :"+updateSenderResult);
			}
			else
			{

				//this is the delay  

				if(StartupDelay){
					_startUpFrameCount++;
					if( _startUpFrameCount < _startupFramesDelay)return;
					if(_attempts> _createAttempts)return;
					_CreateSender();
				}

			}
		}

		void OnDestroy()
		{

		}

		
		private void _CreateSender(){
			//Debug.Log("SpoutSender._CreateSender");

			if (texture == null) return;
			if(!Spout.isInit)return;
			if(!Spout.instance.enabled)return;
			#if UNITY_EDITOR
			if(!Application.isPlaying && !Spout.isEnabledInEditor )return;
			#endif


			//Debug.Log("SpoutSender._CreateSender");

			if (!senderIsCreated) {
					Debug.Log ("Sender is not created, creating one");
                    senderIsCreated = Spout.instance.CreateSender(sharingName, texture,(int) textureFormat);
			}

			_attempts++;
			if(_attempts > _createAttempts) Debug.LogWarning(String.Format("There are problems with creating the sender {0}. Please check your settings or restart Unity.",sharingName));

            if (_cam != null)
            {
                if (_cam.targetTexture == null || _cam.targetTexture != texture)
                {
                    Debug.LogWarning("Your Camera has no Target Texture or the texture that the Spout Sender uses is different!");
                    if (texture != null) _cam.targetTexture = (RenderTexture)texture;
                }
            }

			Spout.instance.OnSenderStopped -= OnSenderStoppedDelegate;
			Spout.instance.OnSenderStopped += OnSenderStoppedDelegate;

			Spout.instance.OnAllSendersStopped-=OnAllSendersStoppedDelegate;
			Spout.instance.OnAllSendersStopped+=OnAllSendersStoppedDelegate;

			Spout.instance.OnEnabled-= _OnSpoutEnabled;
			Spout.instance.OnEnabled+= _OnSpoutEnabled;
		}
		
		private void _OnSpoutEnabled(){
			//Debug.Log("SpoutSender._OnSpoutEnabled");
			if(enabled){
				//force a reconnection
				enabled = !enabled;
				enabled = !enabled;
			}
		}
		
		private void _CloseSender(){
			Debug.Log("SpoutSender._CloseSender:"+sharingName);
			if(senderIsCreated) Spout.CloseSender(sharingName);
			_CloseSenderCleanUpData();
		}
		
		private void OnSenderStoppedDelegate(object sender, TextureShareEventArgs e){
			//Debug.Log("SpoutSender.OnSenderStoppedDelegate:"+e.sharingName);
			if(e.sharingName == sharingName){
				_CloseSenderCleanUpData();
			}
		}

		private void OnAllSendersStoppedDelegate(){
			_CloseSenderCleanUpData();
		}

		private void _CloseSenderCleanUpData(){
			senderIsCreated = false;
		}
	}
}
