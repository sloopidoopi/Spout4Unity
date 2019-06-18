//http://docs.unity3d.com/412/Documentation/ScriptReference/Camera.OnPreCull.html
using UnityEngine;
using System.Collections;

namespace Spout{

	[RequireComponent (typeof(Camera))]
	[ExecuteInEditMode]
	public class InvertCamera : MonoBehaviour {
        //public Camera camera;
        
        protected bool invertCulling = true;
        protected Camera m_cam;
        private void Awake()
        {
            m_cam = GetComponent<Camera>();
        }
        void Start () {
			//camera = get
		}
        private void OnEnable()
        {
            m_cam = GetComponent<Camera>();
        }
        private void OnDisable()
        {
            if (m_cam == null) return;
            m_cam.ResetWorldToCameraMatrix();
            m_cam.ResetProjectionMatrix();
        }

        void OnPreCull () {
            //return;
            if (m_cam == null || !enabled) return;
            m_cam.ResetWorldToCameraMatrix();
            m_cam.ResetProjectionMatrix();
            m_cam.projectionMatrix = m_cam.projectionMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1));
		}
		
		void OnPreRender () {
          
            if (enabled)
            {
                if (invertCulling) GL.invertCulling = true;
            }
           
        }
		
		void OnPostRender () {
                     
                GL.invertCulling = false;
           
        }
		
	}

}