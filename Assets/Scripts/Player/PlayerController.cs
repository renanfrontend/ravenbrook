using UnityEngine;

namespace Ravenbrook
{
    /// <summary>
    /// Jogador em terceira pessoa (câmera sobre o ombro): andar, correr, mirar, virar 180°,
    /// interagir, lanterna e estados de saúde BEM / CUIDADO / PERIGO.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float Hp = 100, MaxHp = 100;
        public bool Dead;
        public Inventory Inv = new Inventory();
        public WeaponSystem Weapons;
        public ProceduralBody Body;
        public Light Flashlight;
        public bool Aiming { get; private set; }
        public bool Running { get; private set; }
        public Interactable Focus { get; private set; }

        // usado pelo -autotest
        public Vector2 SimMove; public bool SimAim, SimRun;

        CharacterController cc;
        Light fill;
        float vy, invuln, grabbedT, stepDist, turnT = -1, turnFrom;
        Vector3 knock;

        public Vector3 Chest => transform.position + Vector3.up * 1.35f;
        GameManager Gm => GameManager.I;
        bool Auto => Gm.AutoTestMode;
        public bool CanAct => Gm.State == GameState.Playing && !Gm.OverwatchOpen && !Dead && grabbedT <= 0f;

        public string StatusKey => Hp > 66 ? "fine" : Hp > 33 ? "caution" : "danger";
        public Color StatusColor => Hp > 66 ? new Color(0.35f, 0.9f, 0.4f) : Hp > 33 ? new Color(0.95f, 0.72f, 0.15f) : new Color(0.95f, 0.2f, 0.15f);

        public static PlayerController Spawn(Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject("Jogador");
            go.layer = Layers.Player;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var p = go.AddComponent<PlayerController>();
            p.Setup();
            return p;
        }

        void Setup()
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.875f, 0);
            cc.stepOffset = 0.3f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f;
            Body = ProceduralBody.Create(transform, BodyKind.Human, 1f, 1);
            Weapons = gameObject.AddComponent<WeaponSystem>();
            Weapons.Init(this);

            var fl = new GameObject("Lanterna");
            fl.transform.SetParent(transform, false);
            fl.transform.localPosition = new Vector3(0.18f, 1.45f, 0.25f);
            Flashlight = fl.AddComponent<Light>();
            Flashlight.type = LightType.Spot;
            Flashlight.range = 24f; Flashlight.spotAngle = 58f; Flashlight.innerSpotAngle = 25f;
            Flashlight.intensity = 2.4f; Flashlight.color = new Color(1f, 0.95f, 0.86f);
            Flashlight.renderMode = LightRenderMode.ForcePixel;

            // luz fraca de preenchimento para o personagem não sumir no escuro
            var f = new GameObject("Preenchimento");
            f.transform.SetParent(transform, false);
            f.transform.localPosition = new Vector3(0, 2.2f, -0.6f);
            fill = f.AddComponent<Light>();
            fill.type = LightType.Point; fill.range = 3.5f; fill.intensity = 0.35f; fill.color = new Color(0.7f, 0.8f, 1f);
            fill.shadows = LightShadows.None;
            ApplyQuality();
        }

        public void ApplyQuality()
        {
            if (Flashlight == null) return;
            Flashlight.shadows = Settings.Quality == 0 ? LightShadows.None : Settings.Quality == 1 ? LightShadows.Hard : LightShadows.Soft;
            Flashlight.shadowBias = 0.03f; Flashlight.shadowNormalBias = 0.3f;
        }

        void Update()
        {
            var cam = ThirdPersonCamera.I;
            float dt = Time.deltaTime;
            invuln -= dt; grabbedT -= dt;
            bool act = CanAct;

            Vector2 inp = Vector2.zero;
            if (act)
            {
                inp = Auto ? SimMove : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (inp.sqrMagnitude > 1) inp.Normalize();
            }
            Aiming = act && (Auto ? SimAim : Input.GetMouseButton(1)) && !Weapons.Reloading;
            Running = act && !Aiming && inp.sqrMagnitude > 0.01f && (Auto ? SimRun : Input.GetKey(KeyCode.LeftShift));

            // virada rápida de 180°
            if (act && !Aiming && turnT < 0 && Input.GetKeyDown(KeyCode.Q)) { turnT = 0; turnFrom = cam.Yaw; }
            if (turnT >= 0)
            {
                turnT += dt / 0.28f;
                cam.Yaw = turnFrom + 180f * Mathf.SmoothStep(0, 1, Mathf.Clamp01(turnT));
                transform.rotation = Quaternion.Euler(0, cam.Yaw, 0);
                if (turnT >= 1) turnT = -1;
            }

            float limp = Hp > 66 ? 1f : Hp > 33 ? 0.88f : 0.68f;
            float speed = (Aiming ? 1.1f : Running ? 4.6f : inp.y < -0.1f ? 1.8f : 2.4f) * limp;
            Quaternion yawRot = Quaternion.Euler(0, cam.Yaw, 0);
            Vector3 move = yawRot * new Vector3(inp.x, 0, inp.y) * speed;

            if (Aiming || turnT >= 0) transform.rotation = Quaternion.Slerp(transform.rotation, yawRot, dt * 18f);
            else if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), dt * 10f);

            vy = cc.isGrounded ? -2f : vy - 20f * dt;
            knock = Vector3.MoveTowards(knock, Vector3.zero, dt * 12f);
            if (cc.enabled) cc.Move((move + knock + Vector3.up * vy) * dt);

            float hs = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;
            Body.Speed = Dead ? 0 : hs;
            Body.Aim = Aiming && !Dead;
            Body.AimPitch = cam.Pitch;

            // passos fazem barulho (os zumbis ouvem)
            if (cc.isGrounded && hs > 0.3f)
            {
                stepDist += hs * dt;
                if (stepDist > (Running ? 1.45f : 1.0f))
                {
                    stepDist = 0;
                    Sfx.PlayAt("step", transform.position, Running ? 0.7f : 0.4f, Random.Range(0.85f, 1.1f), 15f);
                    Noise.Emit(transform.position, Running ? 9f : 3.5f);
                }
            }

            // lanterna segue a mira
            if (Flashlight != null)
            {
                Vector3 target = cam.transform.position + cam.transform.forward * 20f;
                Flashlight.transform.rotation = Quaternion.Slerp(Flashlight.transform.rotation,
                    Quaternion.LookRotation(target - Flashlight.transform.position), dt * 14f);
                if (act && Input.GetKeyDown(KeyCode.L)) { Flashlight.enabled = !Flashlight.enabled; Sfx.Play("empty", 0.5f); }
            }

            FindFocus();
            if (act && Focus != null && Input.GetKeyDown(KeyCode.E)) Focus.Interact(this);

            Sfx.Heart = Dead ? 0 : Hp > 66 ? 0 : Hp > 33 ? 0.35f : 0.85f;
        }

        void FindFocus()
        {
            Focus = null;
            if (!CanAct) return;
            float best = float.MaxValue;
            Vector3 fwd = transform.forward;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.CanInteract) continue;
                Vector3 d = it.Point - Chest;
                float dist = d.magnitude;
                if (dist > it.Range) continue;
                Vector3 flat = new Vector3(d.x, 0, d.z);
                float ang = flat.sqrMagnitude < 0.01f ? 0 : Vector3.Angle(fwd, flat);
                if (ang > 75f) continue;
                float score = dist + ang * 0.01f;
                if (score < best) { best = score; Focus = it; }
            }
        }

        public void Damage(float dmg, Vector3 from, bool grab)
        {
            if (Dead || invuln > 0) return;
            invuln = 1.1f;
            ThirdPersonCamera.I.Shake(0.5f);
            if (PostFX.I != null) PostFX.I.HitFlash = 0.8f;
            Gm.UI.HitFlash();
            Body.Flinch(1f);
            Vector3 away = transform.position - from; away.y = 0;
            knock = away.normalized * (grab ? 3f : 5f);
            Sfx.PlayAt("bite", transform.position, 0.9f);
            Sfx.PlayAt("hurt", transform.position, 0.8f, Random.Range(0.9f, 1.05f));
            if (Cheats.God) return;
            Hp = Mathf.Max(0, Hp - dmg);
            grabbedT = grab ? 0.8f : 0.2f;
            if (Hp <= 0) Die();
        }

        public void Heal(float amount) { Hp = Mathf.Min(MaxHp, Hp + amount); }

        void Die()
        {
            Dead = true;
            Body.Die();
            Sfx.PlayAt("zdie", transform.position, 0.6f, 1.6f);
            Gm.OnPlayerDied();
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
        }
    }
}
