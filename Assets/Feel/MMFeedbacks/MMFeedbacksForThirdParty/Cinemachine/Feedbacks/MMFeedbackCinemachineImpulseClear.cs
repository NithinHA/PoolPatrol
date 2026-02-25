using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using Unity.Cinemachine;

namespace MoreMountains.FeedbacksForThirdParty
{
    [AddComponentMenu("")]
    [FeedbackPath("Camera/Cinemachine Impulse Clear")]
    [FeedbackHelp("This feedback lets you trigger a Cinemachine Impulse clear, stopping instantly any impulse that may be playing.")]
    public class MMFeedbackCinemachineImpulseClear : MMFeedback
    {
        /// sets the inspector color for this feedback
        #if UNITY_EDITOR
        public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.CameraColor; } }
        #endif

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (Active)
            {
                // In Cinemachine v3, ImpulseManager singleton is removed.
                // Impulses must be cancelled per-source. This clears all active impulse envelopes.
                CinemachineImpulseManager.Instance.Clear();
            }
        }
    }
}
