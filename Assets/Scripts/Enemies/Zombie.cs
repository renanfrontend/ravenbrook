using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ravenbrook
{
    public enum ZType { Walker, Runner, Brute }

    /// <summary>Colisor que recebe tiros (corpo ou cabeça).</summary>
    public class HitZone : MonoBehaviour
    {
        public Zombie Owner; public float Mult = 1f; public bool Head;
    }

    /// <summary>Barulho de tiros e passos: zumbis no raio vão investigar.</summary>
    public static class Noise
    {
        public static void Emit(Vector3 pos, float radius)
        {
            foreach (var z in Zombie.All)
                if (!z.Dead && (z.transform.position - pos).sqrMagnitude < radius * radius) z.Hear(pos);
        }
    }

    /// <summary>
    /// Zumbi comum (lento), corredor e o Brutamonte (perseguidor que não morre, só cai de joelhos).
    /// Usa NavMesh para andar; vê, ouve, agarra e morde.
    /// </summary>
    public class Zombie : MonoBehaviour
    {
        public static readonly List<Zombie> All = new List<Zombie>();
        public static int Chasing;

        public ZType Type;
        public string Id;
        public bool Dead;

        enum S { Dormant, Rising, Idle, Investigate, Chase, Attack, Stagger, Dead }
        S state;
        NavMeshAgent agent;
        ProceduralBody body;
        CapsuleCollider col;
        SphereCollider headCol;
        float hp, scale, stateT, lostT, nextGroan, nextRepath, nextLos, attackCd, staggerDur, staggerAcc, chaseSpeed, nextStomp;
        bool seen, hitDone;
        Vector3 home, target, lastKnown;

        GameManager Gm => GameManager.I;

        public static Zombie Spawn(ZType type, Vector3 pos, float yaw, string id, bool dormant, Transform parent)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 2.5f, NavMesh.AllAreas)) pos = hit.position;
            var go = new GameObject((type == ZType.Brute ? "Brutamonte_" : "Zumbi_") + id);
            go.layer = Layers.Enemy;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var z = go.AddComponent<Zombie>();
            z.Setup(type, id, dormant);
            return z;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Setup(ZType type, string id, bool dormant)
        {
            Type = type; Id = id; home = transform.position;
            int seed = id.GetHashCode();
            var rnd = new System.Random(seed);
            scale = type == ZType.Brute ? 1.3f : 0.93f + (float)rnd.NextDouble() * 0.12f;
            body = ProceduralBody.Create(transform, type == ZType.Brute ? BodyKind.Brute : type == ZType.Runner ? BodyKind.Runner : BodyKind.Zombie, scale, seed);

            col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.85f * scale, 0); col.height = 1.65f * scale; col.radius = 0.3f * scale;
            var hz = gameObject.AddComponent<HitZone>(); hz.Owner = this; hz.Mult = 1f;
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;

            var head = new GameObject("CabecaHit"); head.layer = Layers.Enemy;
            head.transform.SetParent(body.Head != null ? body.Head : transform, false);
            head.transform.localPosition = body.Head != null ? new Vector3(0, 0.17f, 0) : new Vector3(0, 1.62f * scale, 0);
            headCol = head.AddComponent<SphereCollider>(); headCol.radius = 0.15f;
            var hh = head.AddComponent<HitZone>(); hh.Owner = this; hh.Head = true; hh.Mult = type == ZType.Brute ? 1.3f : 2.6f;

            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f * scale; agent.height = 1.8f * scale;
            agent.angularSpeed = 260f; agent.acceleration = 10f; agent.stoppingDistance = 0.9f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 30 + rnd.Next(40);

            switch (type)
            {
                case ZType.Walker: hp = 105; chaseSpeed = 0.8f + (float)rnd.NextDouble() * 0.5f; break;
                case ZType.Runner: hp = 70; chaseSpeed = 3.3f; break;
                case ZType.Brute: hp = 99999; chaseSpeed = 2.2f; break;
            }
            nextGroan = Time.time + 2f + (float)rnd.NextDouble() * 6f;
            if (dormant) { state = S.Dormant; body.SetLying(); agent.isStopped = true; }
            else state = S.Idle;
        }

        void Set(S s) { state = s; stateT = 0; hitDone = false; }

        public void Hear(Vector3 pos)
        {
            if (Dead || state == S.Dormant) return;
            if (state == S.Idle || state == S.Investigate) { lastKnown = pos; Set(S.Investigate); }
            else if (state == S.Chase && !seen) lastKnown = pos;
        }

        public void ReturnHome()
        {
            if (Dead) return;
            agent.Warp(home);
            Set(S.Idle);
        }

        bool CanSee(PlayerController p, float dist)
        {
            if (p.Dead) return false;
            float range = Type == ZType.Brute ? 40f : 14f;
            if (dist > range) return false;
            Vector3 eye = transform.position + Vector3.up * 1.6f * scale;
            Vector3 to = p.Chest - eye;
            Vector3 flat = new Vector3(to.x, 0, to.z);
            if (dist > 3f && Vector3.Angle(transform.forward, flat) > 75f) return false;
            if (Physics.Raycast(eye, to.normalized, out var h, to.magnitude, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        void Update()
        {
            if (Dead) return;
            var p = Gm.Player;
            bool frozen = Cheats.FreezeEnemies || p == null || Gm.State == GameState.Title;
            if (frozen || !agent.isOnNavMesh)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                body.Speed = 0;
                return;
            }
            float dt = Time.deltaTime;
            stateT += dt; attackCd -= dt;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (Time.time >= nextLos) { nextLos = Time.time + 0.2f; seen = CanSee(p, dist); if (seen) lastKnown = p.transform.position; }
            bool playerSafe = Gm.Level != null && Gm.Level.IsSafe(p.transform.position);

            switch (state)
            {
                case S.Dormant:
                    agent.isStopped = true;
                    if (dist < 3.2f && !p.Dead) { Set(S.Rising); body.Rise(); Sfx.PlayAt("groan2", transform.position, 1f); }
                    break;
                case S.Rising:
                    agent.isStopped = true;
                    if (stateT > 1.4f) Set(S.Chase);
                    break;
                case S.Idle:
                    agent.speed = 0.35f; agent.isStopped = false;
                    if (stateT > 5f + (Id.Length % 3))
                    {
                        stateT = 0;
                        Vector3 r = home + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f));
                        if (NavMesh.SamplePosition(r, out var nh, 1.5f, NavMesh.AllAreas)) agent.SetDestination(nh.position);
                    }
                    if (seen || (Type == ZType.Brute && !playerSafe)) Alert();
                    break;
                case S.Investigate:
                    agent.speed = chaseSpeed * 0.8f; agent.isStopped = false;
                    if (Time.time >= nextRepath) { nextRepath = Time.time + 0.5f; agent.SetDestination(lastKnown); }
                    if (seen) Alert();
                    else if (!agent.pathPending && agent.remainingDistance < 1.2f && stateT > 1f) { if (stateT > 4f) Set(S.Idle); }
                    break;
                case S.Chase:
                    UpdateChase(p, dist, playerSafe);
                    break;
                case S.Attack:
                    agent.isStopped = true;
                    Face(p.transform.position, dt * 8f);
                    if (!hitDone && stateT > 0.45f)
                    {
                        hitDone = true;
                        float reach = Type == ZType.Brute ? 2.1f : 1.7f;
                        if (dist < reach && !p.Dead)
                            p.Damage(Type == ZType.Brute ? 34f : Type == ZType.Runner ? 14f : 20f, transform.position, Type != ZType.Brute);
                    }
                    if (stateT > 1.0f) { attackCd = Type == ZType.Runner ? 0.9f : 1.4f; Set(S.Chase); }
                    break;
                case S.Stagger:
                    agent.isStopped = true;
                    if (stateT > staggerDur)
                    {
                        if (Type == ZType.Brute && body.IsLying) { body.Rise(); stateT = staggerDur - 1.2f; Sfx.PlayAt("roar", transform.position, 1f, 1f, 45f); }
                        else Set(S.Chase);
                    }
                    break;
            }

            body.Speed = agent.isStopped ? 0f : agent.velocity.magnitude;

            if (Time.time > nextGroan)
            {
                nextGroan = Time.time + Random.Range(4f, 9f) * (state == S.Chase ? 0.5f : 1f);
                if (Type == ZType.Brute) Sfx.PlayAt("roar", transform.position, 0.5f, Random.Range(0.8f, 0.95f), 35f);
                else if (state != S.Dormant) Sfx.PlayAt("groan" + Random.Range(1, 4), transform.position, 0.9f, Random.Range(0.85f, 1.15f), 22f);
            }
            if (Type == ZType.Brute && body.Speed > 0.5f && Time.time > nextStomp)
            {
                nextStomp = Time.time + 0.55f;
                Sfx.PlayAt("stomp", transform.position, 0.9f, Random.Range(0.9f, 1.05f), 40f);
                if (dist < 12f) ThirdPersonCamera.I.Shake(0.15f * (1 - dist / 12f));
            }
        }

        void Alert()
        {
            if (state != S.Chase) Set(S.Chase);
            lostT = 0;
        }

        void UpdateChase(PlayerController p, float dist, bool playerSafe)
        {
            agent.speed = chaseSpeed; agent.isStopped = false;
            if (Type == ZType.Brute && playerSafe)
            {
                // o Brutamonte não entra na sala segura: volta a rondar
                if (Time.time >= nextRepath) { nextRepath = Time.time + 3f; agent.SetDestination(home); }
                return;
            }
            if (seen || Type == ZType.Brute) lostT = 0; else lostT += Time.deltaTime;
            if (lostT > 7f) { Set(S.Investigate); return; }
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.25f;
                agent.SetDestination(seen || Type == ZType.Brute ? p.transform.position : lastKnown);
            }
            float reach = Type == ZType.Brute ? 1.9f : 1.35f;
            if (dist < reach && attackCd <= 0f && !p.Dead) { Set(S.Attack); body.Attack(); }
            else if (dist < 3f) Face(p.transform.position, Time.deltaTime * 5f);
        }

        void Face(Vector3 pos, float k)
        {
            Vector3 d = pos - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), k);
        }

        public void TakeHit(float dmg, Vector3 point, Vector3 dir, float power, bool head)
        {
            if (Dead) return;
            Fx.Blood(point, -dir * 0.3f + dir * 0.7f, head ? 22 : 12);
            Sfx.PlayAt("zhit", point, 0.8f, Random.Range(0.9f, 1.15f));
            body.Flinch(power);
            if (state == S.Dormant) { body.Rise(); Set(S.Rising); }

            if (Type == ZType.Brute)
            {
                staggerAcc += dmg;
                if (staggerAcc >= 280f && state != S.Stagger)
                {
                    staggerAcc = 0; staggerDur = 5f; Set(S.Stagger);
                    body.Die(); // cai de joelhos/costas por alguns segundos
                    Sfx.PlayAt("roar", transform.position, 1f, 0.8f, 45f);
                }
                else if (state == S.Idle || state == S.Investigate) Alert();
                return;
            }

            hp -= dmg;
            if (hp <= 0) { Die(dir); return; }
            if ((head && power >= 1f) || power >= 3f || dmg >= 45f)
            {
                staggerDur = 0.5f + power * 0.12f;
                Set(S.Stagger);
                if (power >= 3f && agent.isOnNavMesh) agent.Move(dir.normalized * 0.35f);
            }
            else if (state == S.Idle || state == S.Investigate) Alert();
        }

        void Die(Vector3 dir)
        {
            Dead = true; state = S.Dead;
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            col.enabled = false; headCol.enabled = false;
            Face(transform.position - dir, 1f);
            body.Speed = 0; body.Die();
            Sfx.PlayAt("zdie", transform.position, 0.9f, Random.Range(0.9f, 1.1f));
            Invoke(nameof(Pool), 1.2f);
            Gm.OnZombieKilled(this);
        }

        void Pool()
        {
            var lvl = Gm.Level;
            Fx.Decal(transform.position + Vector3.up * 0.01f - transform.forward * 0.6f, Vector3.up, 1.6f, Mats.Splat(Id.GetHashCode() & 7), lvl != null ? lvl.transform : null);
        }

        /// <summary>Quantos estão perseguindo o jogador (para a música de tensão).</summary>
        public static void CountChasing()
        {
            int n = 0;
            foreach (var z in All)
                if (!z.Dead && (z.state == S.Chase || z.state == S.Attack)) n += z.Type == ZType.Brute ? 3 : 1;
            Chasing = n;
        }
    }
}
