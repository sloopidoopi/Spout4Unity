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

	public class TextureInfo  {
		
		public string name;
		private int w;
		private int h;
		private IntPtr resourceView;
		
		private Texture2D tex;
		
		// Use this for initialization
		public TextureInfo (string name) {
			this.name = name;
			
		}
		
		public void setInfos(int width, int height, IntPtr resourceView){
			this.w = width;
			this.h = height;
			this.resourceView = resourceView;
		}
		
		public Texture2D getTexture()
		{
			if(resourceView == IntPtr.Zero)
			{
				Debug.LogWarning("ResourceView is null, returning empty texture");

				tex = null;
				//Resources.UnloadUnusedAssets();
				//GC.Collect();
				//There could be problems with creating a Texture2d at this point! 
				//tex = new Texture2D(64,64,TextureFormat.RGBA32,false,true);//new Texture2D(64,64);
				//tex.hideFlags = HideFlags.HideAndDontSave;

			}
			else
			{
				if(tex == null) {
					tex = Texture2D.CreateExternalTexture(w,h,TextureFormat.RGBA32,true,true,resourceView);
					/*
					Without setting the Hideflags there seems to be a reference floating in the scene which causes great trouble with [ExecuteInEditmode] at OnDestroy
					And we get some weired exception when enter PlayMode and there is an already open Spout sender outside from Unity
					*/
					tex.hideFlags = HideFlags.HideAndDontSave;

				}
			}

			return tex;


		}

		//Make it comparable for Linq

		public override bool Equals(object obj)
		{
			TextureInfo q = obj as TextureInfo;
			return q != null && q.name == this.name ;
		}
		
		public override int GetHashCode()
		{
			return this.name.GetHashCode() ^ this.name.GetHashCode();
		}



	}
}
