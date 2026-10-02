using System;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Single owner of "is the gameplay simulation frozen" (doc §2 — touching the Goddess pauses
    /// enemies, projectiles, the player, hazards and every other moving object).
    ///
    /// Pause requests are reference-counted by <i>source</i> object, so two systems can hold the
    /// game paused at once (e.g. the Goddess shop and a settings menu) and the game only resumes
    /// once the last one releases. Nothing outside this service should touch
    /// <c>Time.timeScale</c>.
    /// </summary>
    public interface IPauseService : IService
    {
        /// <summary>True while at least one source holds a pause.</summary>
        bool IsPaused { get; }

        /// <summary>Raised whenever the paused state flips. Arg: the new <see cref="IsPaused"/>.</summary>
        event Action<bool> OnPauseChanged;

        /// <summary>
        /// Freezes the simulation on behalf of <paramref name="source"/>. Re-requesting from the
        /// same source is a no-op, so callers never need to track whether they already paused.
        /// </summary>
        void Pause(object source);

        /// <summary>Releases <paramref name="source"/>'s pause. The game resumes once none remain.</summary>
        void Resume(object source);

        /// <summary>Drops every held pause. Call on scene teardown so a stale holder cannot freeze the next run.</summary>
        void ClearAll();
    }
}
