using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    public class LTCms : Singleton<LTCms>
    {
        void Awake()
        {
            Application.targetFrameRate = 72;
            Debug.Log("CMS API | LTCMS | LTCMS START CALLED " + Instance + " : " + (Instance == this));
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
