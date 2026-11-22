using StickmanFight.Art;
using UnityEngine;

namespace StickmanFight.Weapons
{
    /// <summary>
    /// Процедурный вид оружия из простых фигур с чёрной обводкой (ADR-2):
    /// меч — рукоять + гарда + клинок; молот — рукоять + массивная голова; пистолет — корпус + ствол + рукоять.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        private static Sprite _white;
        private bool _built;

        private static readonly Color Outline = new Color(0.05f, 0.06f, 0.12f, 1f);
        private static readonly Color Steel = new Color(0.86f, 0.90f, 0.96f, 1f);
        private static readonly Color SteelDark = new Color(0.62f, 0.68f, 0.78f, 1f);
        private static readonly Color Wood = new Color(0.42f, 0.28f, 0.17f, 1f);
        private static readonly Color Iron = new Color(0.45f, 0.50f, 0.60f, 1f);
        private static readonly Color Gunmetal = new Color(0.20f, 0.23f, 0.31f, 1f);
        private static readonly Color GunDark = new Color(0.13f, 0.15f, 0.21f, 1f);

        public void Build(WeaponConfig config)
        {
            if (_built) return;
            _built = true;

            if (_white == null) _white = SpriteFactory.Solid(Color.white);

            switch (config.kind)
            {
                case WeaponKind.Hammer: BuildHammer(); break;
                case WeaponKind.Pistol: BuildPistol(); break;
                default: BuildSword(); break;
            }
        }

        private void BuildSword()
        {
            Piece("handle", -0.40f, -0.24f, -0.035f, 0.035f, Wood, 20);
            Piece("guard", -0.27f, -0.18f, -0.11f, 0.11f, GunDark, 20);
            Piece("blade", -0.18f, 0.42f, -0.045f, 0.045f, Steel, 20);
            Piece("edge", 0.30f, 0.44f, -0.02f, 0.02f, SteelDark, 21);
        }

        private void BuildHammer()
        {
            Piece("handle", -0.44f, 0.04f, -0.035f, 0.035f, Wood, 20);
            Piece("head", 0.00f, 0.30f, -0.16f, 0.16f, Iron, 20);
            Piece("band", 0.09f, 0.21f, -0.17f, 0.17f, GunDark, 21);
        }

        private void BuildPistol()
        {
            Piece("body", -0.16f, 0.14f, -0.05f, 0.08f, Gunmetal, 20);
            Piece("barrel", 0.10f, 0.36f, -0.015f, 0.045f, GunDark, 21);
            Piece("grip", -0.17f, -0.04f, -0.17f, -0.02f, Wood, 21);
        }

        /// <summary>Фигура с обводкой: тёмный прямоугольник чуть больше + цветной сверху.</summary>
        private void Piece(string name, float x0, float x1, float y0, float y1, Color fill, int order)
        {
            Quad(name + "_outline", x0 - 0.018f, x1 + 0.018f, y0 - 0.018f, y1 + 0.018f, Outline, order - 1);
            Quad(name, x0, x1, y0, y1, fill, order);
        }

        private void Quad(string name, float x0, float x1, float y0, float y1, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            go.transform.localScale = new Vector3(Mathf.Max(0.01f, x1 - x0), Mathf.Max(0.01f, y1 - y0), 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _white;
            sr.color = color;
            sr.sortingOrder = order;
        }
    }
}
