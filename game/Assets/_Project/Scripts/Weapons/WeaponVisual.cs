using UnityEngine;

namespace StickmanFight.Weapons
{
    /// <summary>
    /// Процедурный вид оружия (ADR-2): меч — линия, молот — рукоять + блок, пистолет — короткий ствол.
    /// Материал создаётся в Awake, чтобы пережить сборку APK.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        private LineRenderer _main;
        private LineRenderer _detail;
        private Material _material;

        public void Build(WeaponConfig config)
        {
            if (_main != null) return;

            _material = new Material(Shader.Find("Sprites/Default")) { name = "WeaponLine" };
            _main = MakeLine("Main", 2);
            _detail = MakeLine("Detail", config.kind == WeaponKind.Hammer ? 4 : 2);

            float half = config.visualSize.x * 0.5f;
            _main.startColor = _main.endColor = config.color;
            _main.SetPosition(0, new Vector3(-half, 0f, 0f));
            _main.SetPosition(1, new Vector3(half, 0f, 0f));
            _main.widthMultiplier = config.visualSize.y;

            _detail.startColor = _detail.endColor = config.color;
            switch (config.kind)
            {
                case WeaponKind.Hammer:
                    _detail.SetPosition(0, new Vector3(half - 0.1f, 0.16f, 0f));
                    _detail.SetPosition(1, new Vector3(half - 0.1f, -0.16f, 0f));
                    _detail.SetPosition(2, new Vector3(half, -0.16f, 0f));
                    _detail.SetPosition(3, new Vector3(half, 0.16f, 0f));
                    _detail.loop = true;
                    _detail.widthMultiplier = config.visualSize.y * 0.8f;
                    break;
                case WeaponKind.Pistol:
                    _detail.SetPosition(0, new Vector3(-half * 0.3f, -0.12f, 0f));
                    _detail.SetPosition(1, new Vector3(-half * 0.3f, -0.02f, 0f));
                    _detail.widthMultiplier = config.visualSize.y * 1.1f;
                    break;
                default:
                    _detail.positionCount = 2;
                    _detail.SetPosition(0, new Vector3(-half * 0.9f, 0.07f, 0f));
                    _detail.SetPosition(1, new Vector3(-half * 0.9f, -0.07f, 0f));
                    _detail.widthMultiplier = config.visualSize.y * 1.4f;
                    break;
            }
        }

        private LineRenderer MakeLine(string lineName, int points)
        {
            var go = new GameObject(lineName);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _material;
            lr.useWorldSpace = false;
            lr.positionCount = points;
            lr.numCapVertices = 2;
            lr.sortingOrder = 20;
            return lr;
        }
    }
}
