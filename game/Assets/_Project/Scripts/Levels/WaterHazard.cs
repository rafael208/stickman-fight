using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.Levels
{
    /// <summary>
    /// Вода под ареной: персонаж, упавший ниже уровня, немедленно выбывает.
    /// Это и есть «арена — участник боя» из дизайн-документа: край платформы опасен.
    /// </summary>
    public class WaterHazard : MonoBehaviour
    {
        public float lethalY = -4.2f;

        private StickmanCharacter[] _characters;

        private void Start()
        {
            _characters = Object.FindObjectsByType<StickmanCharacter>(FindObjectsSortMode.None);
        }

        private void FixedUpdate()
        {
            if (_characters == null) return;

            foreach (var c in _characters)
            {
                if (c == null || c.IsDead || c.Rig == null || c.Rig.Torso == null) continue;
                if (c.Rig.Torso.position.y > lethalY) continue;

                Debug.Log($"[Hazard] {c.name} упал в воду (y={c.Rig.Torso.position.y:0.00})");
                c.ApplyDamage(9999f, Vector2.zero, c.Rig.Torso.position);
            }
        }
    }
}
