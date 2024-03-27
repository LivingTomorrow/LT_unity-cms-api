using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    public class DontDestroy : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}

