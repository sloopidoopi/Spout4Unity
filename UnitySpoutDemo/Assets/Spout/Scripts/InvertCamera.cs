//http://docs.unity3d.com/412/Documentation/ScriptReference/Camera.OnPreCull.html
using UnityEngine;
using System.Collections;

namespace Spout{

	[RequireComponent (typeof(Camera))]
	[ExecuteInEditMode]
	public class InvertCamera : MonoBehaviour {
		//public Camera camera;
		void Start () {
			//camera = get
		}
		
		void OnPreCull () {
			camera.ResetWorldToCameraMatrix();
			camera.ResetProjectionMatrix();
			camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1));
		}
		
		void OnPreRender () {
			GL.SetRevertBackfacing(true);
		}
		
		void OnPostRender () {
			GL.SetRevertBackfacing(false);
		}
		
	}

}