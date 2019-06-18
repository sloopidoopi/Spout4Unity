/* 
 * Spout4Unity
* Copyright © 2014-2015 Benjamin Kuperberg
* Copyright © 2015-2019 Stefan Schlupek
* All rights reserved
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Spout
{
    [ExecuteInEditMode,RequireComponent(typeof(Camera))]
    public class CameraSettingsSynchronizer : MonoBehaviour
    {
       
        protected Camera m_cam;
        protected List<Camera> m_cams;
       
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

#if UNITY_EDITOR

            if (enabled) { 

                 if (!Application.isPlaying)
                {
                    _UpdateChildCameras();

                }
            }
#endif
        }

        private void OnEnable()
        {
            m_cam = GetComponent<Camera>();
            m_cams = GetComponentsInChildren<Camera>(true)?.ToList();
            
        }


        private void OnDisable()
        {
            
        }

        private Vector3 m_pos,m_scale;
        private Quaternion m_rot;

       

        private void _UpdateChildCameras()
        {
            if (m_cams == null || m_cam == null) return;

            m_cams.ForEach(c => {
                var rt = c.targetTexture;
                m_pos = c.transform.position;
                m_rot = c.transform.rotation;
                m_scale = c.transform.localScale;
                c.CopyFrom(m_cam);//fast way to copy all settings in one call, but we have to restore some settings by hand
                c.targetTexture = rt;
                c.transform.position = m_pos;
                c.transform.rotation = m_rot;
                c.transform.localScale = m_scale;
                
            });

        }

    }
}
