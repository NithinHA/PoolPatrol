using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// A calm(er) stretch of the arena: spawns trickle in from <see cref="SpawnSectionSO.EnemyBucket"/>
    /// for a fixed <see cref="Duration"/>, regulated by a live threat budget. Advances to the next
    /// section once the timer runs out, regardless of how many enemies are still alive.
    ///
    /// Two control loops shape the feel: a difficulty ramp (<see cref="DifficultyCurve"/>) that biases
    /// selection from easy toward hard enemies over the section, and a threat budget
    /// (<see cref="MinThreatBudget"/>..<see cref="MaxThreatBudget"/>) that holds spawns whenever the
    /// on-screen threat is already at or above the current target - so a screen full of tough enemies
    /// naturally quiets down until the player thins it out.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Spawning/Cooldown Section", fileName = "CooldownSection")]
    public class CooldownSectionSO : SpawnSectionSO
    {
        [Header("Cooldown")]
        [Tooltip("How long this section lasts, in seconds. The arena advances once this elapses.")]
        [Min(0f)] public float Duration = 20f;

        [Tooltip("How much of the progress bar this section fills, relative to every other cooldown " +
                  "section's weight (not normalized to any total - only the ratio between sections " +
                  "matters). Lets a long cooldown consume a small sliver of the bar, or a short one " +
                  "consume a large chunk, independent of its real-time Duration.")]
        [Min(0.01f)] public float ProgressWeight = 10f;

        [Header("Difficulty Ramp")]
        [Tooltip("Difficulty pressure (Y, 0..1) over the section's normalized time (X, 0..1). Drives " +
                 "both enemy selection bias and the threat budget lerp. Default linear 0 -> 1: starts " +
                 "easy, ends hard. Shape it for a mid-section lull, a late spike, etc.")]
        public AnimationCurve DifficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Threat Budget")]
        [Tooltip("Target on-screen threat (sum of alive enemies' ThreatCost) at pressure 0. Spawning " +
                 "pauses while current threat is at or above the target, so the section self-regulates.")]
        [Min(0f)] public float MinThreatBudget = 3f;

        [Tooltip("Target on-screen threat at pressure 1. The live target lerps between Min and Max by " +
                 "the DifficultyCurve, so late in the section the arena tolerates a heavier crowd.")]
        [Min(0f)] public float MaxThreatBudget = 10f;

        /// <summary>Difficulty pressure at a given normalized time (0..1) through the section.</summary>
        public float PressureAt(float normalizedTime) =>
            Mathf.Clamp01(DifficultyCurve.Evaluate(Mathf.Clamp01(normalizedTime)));

        /// <summary>Target on-screen threat for the given pressure (0..1).</summary>
        public float ThreatBudgetAt(float pressure01) =>
            Mathf.Lerp(MinThreatBudget, MaxThreatBudget, Mathf.Clamp01(pressure01));
    }
}
