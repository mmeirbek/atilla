using System;
using System.Collections.Generic;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// Walks the player to a target like a person who knows the way: it plans a route on a half-metre grid, asks the
    /// physics world which cells a body can stand in (yurts, carts, animals and people all block), and follows that
    /// route through the real character controller. Used by the editor validation to prove every world job can be
    /// finished on foot; never added in normal play.
    /// </summary>
    public sealed class AutoPilot : MonoBehaviour
    {
        private const float Speed = 4f;
        private const float Cell = .5f;
        private const float MinX = -25f, MaxX = 29f, MinZ = -25f, MaxZ = 25f;
        private const float BodyRadius = .45f;

        public Func<Vector3?> Target;
        /// <summary>True when the last planning found no way to the target at all.</summary>
        public bool NoRoute { get; private set; }

        private CharacterController controller;
        private PlayerVisual visual;
        private readonly List<Vector3> route = new();
        private Vector3 plannedFor = new(9999f, 0f, 9999f);
        private float nextPlan;
        private bool[,] blocked;
        private float blockedAt = -99f;
        private int width, depth;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            visual = GetComponent<PlayerVisual>();
            width = Mathf.CeilToInt((MaxX - MinX) / Cell);
            depth = Mathf.CeilToInt((MaxZ - MinZ) / Cell);
        }

        private void Update()
        {
            var target = Target?.Invoke();
            if (!target.HasValue || controller == null || !controller.enabled) return;
            var goal = new Vector3(target.Value.x, 0f, target.Value.z);
            // Re-plan when the goal moves (next pickup, the horse) and every so often, since a yurt can rise mid-job.
            if ((goal - plannedFor).sqrMagnitude > .25f || Time.time > nextPlan) Plan(goal);
            if (route.Count == 0) return;

            var here = new Vector3(transform.position.x, 0f, transform.position.z);
            while (route.Count > 1 && (route[0] - here).sqrMagnitude < .2f * .2f) route.RemoveAt(0);
            var to = route[0] - here;
            if (route.Count == 1 && to.magnitude < .25f) return;
            var direction = to.normalized;
            controller.Move((direction * Speed + Vector3.down * 2f) * Time.deltaTime);
            if (visual != null) visual.SetMovement(direction, true);
        }

        // ------------------------------------------------------------ planning

        private void Plan(Vector3 goal)
        {
            plannedFor = goal;
            nextPlan = Time.time + 1f;
            if (Time.time - blockedAt > 2f) Survey();
            route.Clear();
            var start = ToCell(transform.position);
            var end = NearestOpen(ToCell(goal));
            NoRoute = !Search(start, end);
            if (route.Count > 0) route[route.Count - 1] = new Vector3(goal.x, 0f, goal.z);
        }

        /// <summary>Marks every cell where a body would overlap a solid collider other than the player's own.</summary>
        private void Survey()
        {
            blockedAt = Time.time;
            blocked ??= new bool[width, depth];
            var own = GetComponentsInChildren<Collider>();
            var hits = new Collider[8];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                {
                    var centre = new Vector3(MinX + (x + .5f) * Cell, 1f, MinZ + (z + .5f) * Cell);
                    int count = Physics.OverlapCapsuleNonAlloc(centre + Vector3.down * .45f, centre + Vector3.up * .4f, BodyRadius, hits, ~0, QueryTriggerInteraction.Ignore);
                    bool solid = false;
                    for (int i = 0; i < count && !solid; i++)
                        if (Array.IndexOf(own, hits[i]) < 0 && !(hits[i] is MeshCollider && hits[i].name == "Ground")) solid = true;
                    blocked[x, z] = solid;
                }
        }

        private Vector2Int ToCell(Vector3 world) => new(
            Mathf.Clamp(Mathf.FloorToInt((world.x - MinX) / Cell), 0, width - 1),
            Mathf.Clamp(Mathf.FloorToInt((world.z - MinZ) / Cell), 0, depth - 1));

        private Vector3 ToWorld(Vector2Int cell) => new(MinX + (cell.x + .5f) * Cell, 0f, MinZ + (cell.y + .5f) * Cell);

        /// <summary>The goal itself may sit inside a solid thing (a post, the yurt site); aim at the closest free cell.</summary>
        private Vector2Int NearestOpen(Vector2Int cell)
        {
            if (!blocked[cell.x, cell.y]) return cell;
            for (int r = 1; r < 12; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                        int x = cell.x + dx, z = cell.y + dz;
                        if (x >= 0 && z >= 0 && x < width && z < depth && !blocked[x, z]) return new Vector2Int(x, z);
                    }
            return cell;
        }

        /// <summary>A* over the grid with diagonal steps that never cut a blocked corner.</summary>
        private bool Search(Vector2Int start, Vector2Int end)
        {
            var open = new SortedSet<(float f, int id)>();
            var cost = new Dictionary<int, float>();
            var from = new Dictionary<int, int>();
            int Id(Vector2Int c) => c.x * depth + c.y;
            Vector2Int Of(int id) => new(id / depth, id % depth);
            float H(Vector2Int c) => Vector2Int.Distance(c, end);
            int startId = Id(start), endId = Id(end);
            cost[startId] = 0f;
            open.Add((H(start), startId));
            int expanded = 0;
            while (open.Count > 0 && expanded++ < 40000)
            {
                var current = open.Min;
                open.Remove(current);
                if (current.id == endId) break;
                var c = Of(current.id);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dz);
                        if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= depth || blocked[n.x, n.y]) continue;
                        if (dx != 0 && dz != 0 && (blocked[c.x + dx, c.y] || blocked[c.x, c.y + dz])) continue;
                        float step = dx != 0 && dz != 0 ? 1.414f : 1f;
                        float next = cost[current.id] + step;
                        int nid = Id(n);
                        if (cost.TryGetValue(nid, out var known) && known <= next) continue;
                        if (cost.ContainsKey(nid)) open.Remove((known + H(n), nid));
                        cost[nid] = next;
                        from[nid] = current.id;
                        open.Add((next + H(n), nid));
                    }
            }
            if (!from.ContainsKey(endId) && startId != endId) return false;
            var cells = new List<Vector3>();
            for (int id = endId; id != startId; id = from[id]) cells.Add(ToWorld(Of(id)));
            cells.Reverse();
            route.AddRange(cells);
            return true;
        }
    }
}
