﻿using UnityEngine;
using System;

namespace TouhouHeartstone.OldFrontend.SimpleAnimationSystem
{
    /// <summary>
    /// 带有关键帧的旋转和位移动画
    /// </summary>
    public class KeyframeAnimation : SimpleAnimationBase
    {
        float startTime;

        [SerializeField]
        public Keyframe[] keyframes;

        public override bool isFinish => _isFinish;
        private bool _isFinish;

        private Keyframe lastState = new Keyframe();

        /// <summary>
        /// 动画播放完毕的Callback
        /// </summary>
        public event Action OnAnimationFinish;

        public override void SetStartTime(float time)
        {
            startTime = time;
            lastState.position = target.transform.localPosition;
            lastState.rotation = target.transform.localRotation.eulerAngles;
            _isFinish = false;
        }

        public override void UpdateAnimation(float time)
        {
            float dt = time - startTime;
            for (int i = 0; i < keyframes.Length; i++)
            {
                var currentFrame = keyframes[i];
                if (currentFrame.time >= dt)
                {
                    Keyframe lastFrame = i > 0 ? keyframes[i - 1] : lastState;
                    var result = interport(lastFrame, currentFrame, dt);

                    target.transform.localPosition = result.position;
                    target.transform.localRotation = Quaternion.Euler(result.rotation);

                    break;
                }
            }

            if (dt > keyframes[keyframes.Length - 1].time)
            {
                target.transform.localPosition = keyframes[keyframes.Length - 1].position;
                target.transform.localRotation = Quaternion.Euler(keyframes[keyframes.Length - 1].rotation);

                _isFinish = true;
                OnAnimationFinish?.Invoke();
            }
        }

        protected Keyframe interport(Keyframe a, Keyframe b, float t)
        {
            if (a.time >= t) return a;
            if (b.time <= t) return b;

            Keyframe r = new Keyframe();
            r.time = t;
            var p = (t - a.time) / (b.time - a.time);

            var rot = standardizeRotation(a.rotation, b.rotation);

            r.rotation = Vector3.Lerp(rot, b.rotation, p);
            r.position = Vector3.Lerp(a.position, b.position, p);
            return r;
        }

        float standardizeRotation(float from, float to)
        {
            int sig = from > to ? -1 : 1;
            while(Mathf.Abs(from - to) > 180)
            {
                from += sig * 360;
            }

            return from;
        }

        Vector3 standardizeRotation(Vector3 from, Vector3 to)
        {
            var st = from;
            st.x = standardizeRotation(from.x, to.x);
            st.y = standardizeRotation(from.y, to.y);
            st.z = standardizeRotation(from.z, to.z);

            return st;
        }

        /// <summary>
        /// 设置动画末尾的位置信息
        /// </summary>
        /// <param name="pos"></param>
        public void SetEndPosition(Vector3 pos)
        {
            keyframes[keyframes.Length - 1].position = pos;
        }

        /// <summary>
        /// 设置动画末尾的旋转信息
        /// </summary>
        /// <param name="rot"></param>
        public void SetEndRotation(Vector3 rot)
        {
            keyframes[keyframes.Length - 1].rotation = rot;
        }
    }

    [Serializable]
    public class Keyframe
    {
        public float time;
        public Vector3 position;
        public Vector3 rotation;
    }
}