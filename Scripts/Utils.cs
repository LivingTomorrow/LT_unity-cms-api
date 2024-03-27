using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    public static class Utils
    {
        public static void DelayedCall(float delayTime, Action callback)
        {
            if (callback == null)
            {
                Debug.LogWarning("CMS API | Utils | DelayedCall : Callback is null. No action will be performed.");
                return;
            }

            if (delayTime <= 0f)
            {
                Debug.LogWarning("CMS API | Utils | DelayedCall : Delay time should be greater than zero.");
                return;
            }

            GameObject delayObject = new GameObject("DelayedCallObject");
            DelayedCallBehaviour behaviour = delayObject.AddComponent<DelayedCallBehaviour>();
            behaviour.Initialize(delayTime, callback);
        }

        private class DelayedCallBehaviour : MonoBehaviour
        {
            private Action callback;
            private float delayTime;

            public void Initialize(float delayTime, Action callback)
            {
                this.delayTime = delayTime;
                this.callback = callback;
                StartCoroutine(DelayCoroutine());
            }

            private System.Collections.IEnumerator DelayCoroutine()
            {
                yield return new WaitForSeconds(delayTime);
                callback?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}

