using System;
using UnityEngine;

namespace UI.Transition
{
    /// <summary>
    /// A full-screen transition visual that can cover the screen, then recede to reveal whatever is
    /// behind it. <see cref="SceneTransitionController"/> drives one of these around a scene load, so
    /// the effect itself can be swapped (sprite wave, shader wipe, …) without touching the controller.
    /// </summary>
    public abstract class ScreenTransitionEffect : MonoBehaviour
    {
        /// <summary>
        /// Covers the screen. <paramref name="screenOrigin"/> is the pixel position the transition
        /// conceptually starts from (usually the button that was pressed); effects may ignore it.
        /// Invokes <paramref name="onComplete"/> once the screen is fully covered — that is the point
        /// at which it is safe to swap scenes.
        /// </summary>
        public abstract void Cover(Vector2 screenOrigin, Action onComplete);

        /// <summary>Recedes to reveal the scene behind, then hides itself.</summary>
        public abstract void Reveal(Action onComplete = null);
    }
}
