/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015 Stefan Schlupek
* All rights reserved
*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Spout{

	[ExecuteInEditMode]
	public class Spout : MonoBehaviour {

		#region public
		public  event EventHandler<TextureShareEventArgs> OnSenderStopped;
		public  event Action OnEnabled;
		public  event Action OnAllSendersStopped;

		public delegate void TextureSharedDelegate(TextureInfo texInfo);
		public  TextureSharedDelegate texSharedDelegate;
		public delegate void SenderStoppedDelegate(TextureInfo texInfo);
		public  SenderStoppedDelegate senderStoppedDelegate;

		public  List<TextureInfo> activeSenders;
		public  List<TextureInfo> activeLocalSenders;
		public HashSet<string> localSenderNames;


		public static bool isInit {
			get{return _isInit;}
		}
		public  static bool isEnabledInEditor{
			get{
				return _instance == null ? false : _instance._isEnabledInEditor;
				//return _isEnabledInEditor;
			} 
		}
		//You can use a fakeName of your choice .It's just to force an update in the Spout Receiver at start even if the 'offical' sharingName doesn't change.
		public static string fakeName = "SpoutIsSuperCoolAndMakesFun";
		#endregion

		#region private

		
		private IntPtr intptr_senderUpdate_delegate;
		private IntPtr intptr_senderStarted_delegate;
		private IntPtr intptr_senderStopped_delegate;
		
		// Use GCHandle to hold the delegate object in memory.
		private GCHandle handleSenderUpdate;
		private GCHandle handleSenderStarted;
		private GCHandle handleSenderStopped;

		#if UNITY_EDITOR
		//To get a reference to the GameView
		private static System.Reflection.Assembly assembly;
		private static System.Type gameviewType;
		private static UnityEditor.EditorWindow gameview;
		#endif

		#pragma warning disable 414
		[SerializeField]
		private static bool _isInit;
		[SerializeField]
		private bool _isEnabledInEditor;
		#pragma warning restore 414
		private static bool isReceiving;
		
		private  List<TextureInfo> newSenders;
		private  List<TextureInfo> stoppedSenders;

		[SerializeField]
		private static Spout _instance;


		#pragma warning disable 414
		//In  EditMode we have to force the camera to render.But as this is called 100 times per second beware of your performance, so we only render at a special interval
		private int _editorUpdateFrameInterval = 3;
		#pragma warning restore 414

		private int _frameCounter;
		#endregion

		[SerializeField]
		private static Texture2D _nullTexture ;
		public static Texture2D nullTexture{
			get {return _nullTexture;}
		}


		
		public static Spout instance
		{
			get
			{
				if(_instance == null){
					_instance = GameObject.FindObjectOfType<Spout>();
					if(_instance == null) {
						GameObject _go = new GameObject("Spout");			
						_instance = _go.AddComponent<Spout>();
					}
					DontDestroyOnLoad(_instance.gameObject);
				}
				
				return _instance;
			}
			private set{_instance = value;}
		}


		//
		public void Awake(){
			Debug.Log("Spout.Awake");
		
			if(_instance != null && _instance != this )
			{
				//Debug.Log("Spout.Awake.Destroy");
				#if UNITY_EDITOR
				DestroyImmediate(this.gameObject);
				#else
				Destroy(this.gameObject);
				#endif
				return;
			}

			newSenders = new List<TextureInfo>();
			stoppedSenders = new List<TextureInfo>();
			activeSenders = new List<TextureInfo>();
			activeLocalSenders = new List<TextureInfo>();
			localSenderNames = new HashSet<string>();

			_nullTexture = new Texture2D(32,32);
			_nullTexture.hideFlags = HideFlags.HideAndDontSave;

		}
		public void OnEnable(){
			//Debug.Log("Spout.OnEnable");
			if(_instance != null && _instance != this )
			{
				//Debug.Log("Spout.OnEnable.Destroy");
				#if UNITY_EDITOR
				DestroyImmediate(this.gameObject);
				#else
				Destroy(this.gameObject);
				#endif
				return;
			}

			newSenders = new List<TextureInfo>();
			stoppedSenders = new List<TextureInfo>();
			activeSenders = new List<TextureInfo>();
			activeLocalSenders = new List<TextureInfo>();
			localSenderNames = new HashSet<string>();
			
			#if UNITY_EDITOR
			assembly = typeof(UnityEditor.EditorWindow).Assembly;
			gameviewType = assembly.GetType( "UnityEditor.GameView" );
			gameview = UnityEditor.EditorWindow.GetWindow(gameviewType);
			#endif


#if UNITY_EDITOR
			if(_isEnabledInEditor || Application.isPlaying){
				_Enable();
			}
#else
			_Enable();
#endif
			

		}

		private void _Enable(){
			//Debug.Log("Spout._Enable");
			#if UNITY_EDITOR
			
			if(!Application.isPlaying){
				UnityEditor.EditorApplication.update -= _Update;
				UnityEditor.EditorApplication.update += _Update;
			}
			#endif
			
			
			_Init();
			//notify others so they can re-initialize
			if(OnEnabled != null) OnEnabled();
		}

		private void _Disable(){
			
		}

		private void _Init(){
			//Debug.Log("Spout._Init");
		
			initNative();
			//Debug.Log("isReceiving:+"+isReceiving);
		
			_startReceiving();
			_isInit = true;
			//if(debug) initDebugConsole();
		}




		
		void Update()
		{
			//if(_instance == null || _instance != this ) return;
			
			_Update();
		}	
		private void _Update()
		{	

			#if UNITY_EDITOR
			_frameCounter++;
			_frameCounter %= _editorUpdateFrameInterval;
			if(_frameCounter == 0){
				UnityEditor.SceneView.RepaintAll();
				gameview.Repaint();
			}
			#endif

			if(isReceiving){
				//Debug.Log("checkReceivers");
				checkReceivers();
			}

			lock(this){
			foreach(TextureInfo s in newSenders)
			{
				//Debug.Log("texSharedDelegate");
				activeSenders.Add (s);
				if(texSharedDelegate != null) texSharedDelegate(s);

			}
			
			newSenders.Clear();
			
			foreach(TextureInfo s in stoppedSenders)
			{
				foreach(TextureInfo t in activeSenders)
				{
					if(s.name == t.name)
					{
						activeSenders.Remove(t);
						break;
					}
				}
				
				//Debug.Log ("Stopped sender from Spout :"+s.name);
				if(senderStoppedDelegate != null) senderStoppedDelegate(s);
			}
			
			stoppedSenders.Clear();
			}//lock
		}

		public void OnDisable(){
			//Debug.Log("Spout.OnDisable");
			if(_instance != this)return;

			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= _Update;
			#endif

			StopAllLocalSenders();
			//Force the Plugin to check. Otherwise we don't get a SenderStopped delegate call
			Update();

			//Debug.Log("Spout.OnDisable.End");
		}
		
		void OnDestroy()
		{
			//Debug.Log("Spout.OnDestroy");
			if(_instance != this)return;
		
			if(_isInit){
			_CleanUpResources();
			}

			isReceiving = false;
			_isInit = false;
			newSenders = null;
			stoppedSenders = null;
			activeSenders = null;
			activeLocalSenders = null;
			localSenderNames = null;
			
			OnEnabled = null;
			OnSenderStopped= null;
			
			_instance = null;


			GC.Collect();//??

		}

		private void _CleanUpResources(){
			clean();
			
			_instance.texSharedDelegate = null;
			_instance.senderStoppedDelegate = null;
			
			_instance.handleSenderUpdate.Free(); 
			_instance.handleSenderStarted.Free();
			_instance.handleSenderStopped.Free();

			intptr_senderUpdate_delegate = IntPtr.Zero;
			intptr_senderStarted_delegate = IntPtr.Zero;
			intptr_senderStopped_delegate = IntPtr.Zero;
			

		}

		
		public void addListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback )
		{
			//	Debug.Log ("Spout.addListener");
			if(_instance == null)return;
			_instance.texSharedDelegate += sharedCallback;
			_instance.senderStoppedDelegate += stoppedCallback;
		}
		
		public static void removeListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback )
		{
			//	Debug.Log ("Spout.removeListener");
			if(_instance == null)return;
			_instance.texSharedDelegate -= sharedCallback;
			_instance.senderStoppedDelegate -= stoppedCallback;
		}	

		public  bool CreateSender(string sharingName, Texture tex, int texFormat = 1)
		{
			if(!enabled)return false;
			if(!_isInit)return false;
			#if UNITY_EDITOR
			if(!Application.isPlaying && !_isEnabledInEditor ) return false;
			#endif
			//Debug.Log("Spout.CreateSender");
			Debug.Log("Spout.CreateSender:"+sharingName+"::"+tex.GetNativeTexturePtr().ToInt32());
			bool result = createSenderNative(sharingName, tex.GetNativeTexturePtr(), texFormat);
			if (!result) Debug.LogWarning (String.Format("Spout sender creation with name {0} failed !",sharingName));
			if(result) {
				//Debug.Log (String.Format("Spout sender creation with name {0} success !",sharingName));
				localSenderNames.Add(sharingName);
			}
			return result;
		}
		
		public  bool UpdateSender(string sharingName, Texture tex)
		{
			if(enabled == false || gameObject.activeInHierarchy == false || _isInit == false)return false;
			//Debug.Log("Spout.UpdateSender:"+sharingName+"::"+tex.GetNativeTexturePtr().ToInt32());
			return updateSenderNative(sharingName, tex.GetNativeTexturePtr());
		}
		

		public  TextureInfo getTextureInfo (string sharingName)
		{
			if(activeSenders == null) return null;
			
			foreach(TextureInfo tex in activeSenders)
			{
				if(tex.name == sharingName) return tex;
			}
			
			if(sharingName != Spout.fakeName) Debug.Log (String.Format("sharing name {0} not found",sharingName));
			
			return null;
		}	
		
		//Imports
		[DllImport ("NativeSpoutPlugin", EntryPoint="init")]
		public  static extern bool initNative();
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="initDebugConsole")]
		private static extern void _initDebugConsole();
		
		[DllImport ("NativeSpoutPlugin")]
		private static extern void checkReceivers();
		
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="createSender")]
		private static extern bool createSenderNative (string sharingName, IntPtr texture, int texFormat);
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="updateSender")]
		private static extern bool updateSenderNative (string sharingName, IntPtr texture);
		
		[DllImport ("NativeSpoutPlugin", EntryPoint="closeSender")]
		public static extern bool CloseSender (string sharingName);
		
		[DllImport ("NativeSpoutPlugin")]
		private static extern void clean();

		
		[DllImport ("NativeSpoutPlugin", EntryPoint="startReceiving")]
		private static extern bool startReceivingNative(IntPtr senderUpdateHandler,IntPtr senderStartedHandler,IntPtr senderStoppedHandler);

			
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderUpdateDelegate(int numSenders);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderStartedDelegate(string senderName, IntPtr resourceView,int textureWidth, int textureHeight);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SpoutSenderStoppedDelegate(string senderName);

		
		public void initDebugConsole(){
			//check if multiple inits?
			_initDebugConsole();
		}



		private  void _startReceiving()
		{
			if(isReceiving)return;
			//Debug.Log("Spout.startReceiving");
			SpoutSenderUpdateDelegate senderUpdate_delegate = new SpoutSenderUpdateDelegate(SenderUpdate);
			handleSenderUpdate = GCHandle.Alloc(senderUpdate_delegate);
			 intptr_senderUpdate_delegate = Marshal.GetFunctionPointerForDelegate (senderUpdate_delegate);
			
			SpoutSenderStartedDelegate senderStarted_delegate = new SpoutSenderStartedDelegate(SenderStarted);
			handleSenderStarted = GCHandle.Alloc(senderStarted_delegate);
			 intptr_senderStarted_delegate = Marshal.GetFunctionPointerForDelegate (senderStarted_delegate);
			
			SpoutSenderStoppedDelegate senderStopped_delegate = new SpoutSenderStoppedDelegate(SenderStopped);
			handleSenderStopped = GCHandle.Alloc(senderStopped_delegate);
			 intptr_senderStopped_delegate = Marshal.GetFunctionPointerForDelegate (senderStopped_delegate);
			
			isReceiving = startReceivingNative(intptr_senderUpdate_delegate, intptr_senderStarted_delegate, intptr_senderStopped_delegate);
		}
		
		private  void SenderUpdate(int numSenders)
		{
			//Debug.Log("Sender update, numSenders : "+numSenders);
		}
		
		private  void SenderStarted(string senderName, IntPtr resourceView,int textureWidth, int textureHeight)
		{
			Debug.Log("Spout. Sender started, sender name : "+senderName);
			if(_instance == null || _instance.activeLocalSenders == null || _instance.newSenders == null)return;
			lock(this){
				TextureInfo texInfo = new TextureInfo(senderName);
				Debug.Log("resourceView:"+resourceView.ToInt32());
				texInfo.setInfos(textureWidth,textureHeight,resourceView);
				_instance.newSenders.Add(texInfo);
				if(_instance.localSenderNames.Contains(texInfo.name)){
				_instance.activeLocalSenders.Add(texInfo);
				//Debug.Log("activeLocalSenders.count:"+_instance.activeLocalSenders.Count);
				}
				Debug.Log("Spout.SenderStarted.End");
			}//lock
		}
		private  void SenderStopped(string senderName)
		{
			Debug.Log("Sender stopped, sender name : "+senderName);
			if(_instance == null || _instance.activeLocalSenders == null || _instance.stoppedSenders == null)return;
			lock(this){
				TextureInfo texInfo = new TextureInfo(senderName);
				
				_instance.stoppedSenders.Add (texInfo);

				_instance.localSenderNames.Remove(texInfo.name);

				if(_instance.activeLocalSenders.Contains(texInfo)){
					_instance.activeLocalSenders.Remove(texInfo);
				}
			}//lock
			//Debug.Log("localSenderNames.count:"+instance.localSenderNames.Count);
			//Debug.Log("activeLocalSenders.count:"+instance.activeLocalSenders.Count);
		}

		private void StopAllLocalSenders(){

			Debug.Log("Spout.StopAllLocalSenders()"); 
			if(_instance == null) return;
			foreach(TextureInfo t in _instance.activeLocalSenders)
			{	
				CloseSender(t.name);
				if(OnSenderStopped != null) OnSenderStopped(this,new TextureShareEventArgs(t.name));
				/*
				double i = 0;
				while(i< 100000000){
					i++;
				}
				*/
				 
			}
				
			if(OnAllSendersStopped != null) OnAllSendersStopped();

		}
		
	}

	public class TextureShareEventArgs : EventArgs
	{
		public string sharingName {get; set; }
		
		public TextureShareEventArgs(string myString)
		{
			this.sharingName = myString;
		}
	}

}


