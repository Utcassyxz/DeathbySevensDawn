// 职责：按玩家与家具碰撞体之间的空隙判断触达，不从大贴图中心扩大半径。
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class PropReach
    {
        public static float Gap(GameObject prop, GameObject player)
        {
            if (prop == null || player == null)
                return float.PositiveInfinity;
            var a = prop.GetComponent<Collider2D>();
            var b = player.GetComponent<Collider2D>();
            if (a != null && b != null)
                return Mathf.Max(0, a.Distance(b).distance);
            var renderer = prop.GetComponent<Renderer>();
            Vector2 p = player.transform.position;
            float edge = renderer != null ? Vector2.Distance(p, renderer.bounds.ClosestPoint(p)) : Vector2.Distance(p, prop.transform.position);
            return Mathf.Max(0, edge - (b != null ? Mathf.Min(b.bounds.extents.x, b.bounds.extents.y) : .25f));
        }
    }
}
