using UnityEngine;

namespace Ravenbrook
{
    public enum WeaponId { Knife, Pistol, Shotgun, Magnum }

    public class WeaponDef
    {
        public WeaponId Id; public string Key; public ItemId Ammo; public int Mag;
        public float Damage; public int Pellets = 1; public float Spread; public float Rate; public float Reload;
        public string Sound; public float Power; public float Noise; public bool Pierce; public float Kick;
    }

    /// <summary>Faca, pistola, escopeta e magnum. Mira com dispersão que fecha com o tempo (estilo RE).</summary>
    public class WeaponSystem : MonoBehaviour
    {
        public static readonly WeaponDef[] Defs =
        {
            new WeaponDef { Id = WeaponId.Knife,   Key = "w_knife",   Ammo = ItemId.None,  Mag = 0,  Damage = 22,  Rate = 0.5f,  Power = 0.6f },
            new WeaponDef { Id = WeaponId.Pistol,  Key = "w_pistol",  Ammo = ItemId.AmmoP, Mag = 12, Damage = 24,  Spread = 0.5f, Rate = 0.26f, Reload = 1.5f, Sound = "pistol",  Power = 1f, Noise = 20, Kick = 1.2f },
            new WeaponDef { Id = WeaponId.Shotgun, Key = "w_shotgun", Ammo = ItemId.AmmoS, Mag = 6,  Damage = 14,  Pellets = 9, Spread = 5.5f, Rate = 0.95f, Reload = 2.3f, Sound = "shotgun", Power = 3f, Noise = 28, Kick = 4f },
            new WeaponDef { Id = WeaponId.Magnum,  Key = "w_magnum",  Ammo = ItemId.AmmoM, Mag = 6,  Damage = 160, Spread = 0.3f, Rate = 1.0f,  Reload = 2.6f, Sound = "magnum",  Power = 5f, Noise = 32, Kick = 6f, Pierce = true },
        };

        public bool[] Owned = { true, true, false, false };
        public int[] Mag = { 0, 12, 0, 0 };
        public int Current = 1;
        public float KnifeDur = 100f;
        public bool Reloading { get; private set; }
        public float SpreadDeg { get; private set; }
        public float HitMarker;

        public WeaponDef Def => Defs[Current];
        PlayerController p;
        float nextFire, reloadEnd, bloom;
        readonly GameObject[] models = new GameObject[4];
        readonly Transform[] muzzles = new Transform[4];
        Light flash;

        public void Init(PlayerController owner)
        {
            p = owner;
            BuildModels();
            var lg = new GameObject("Clarao");
            lg.transform.SetParent(transform, false);
            flash = lg.AddComponent<Light>();
            flash.type = LightType.Point; flash.range = 7f; flash.intensity = 3f; flash.color = new Color(1f, 0.75f, 0.4f);
            flash.shadows = LightShadows.None; flash.enabled = false;
            Select(1);
        }

        public int Reserve(int w) => Defs[w].Ammo == ItemId.None ? 0 : p.Inv.Count(Defs[w].Ammo);

        public void Give(WeaponId w, int loaded)
        {
            int i = (int)w;
            Owned[i] = true;
            Mag[i] = Mathf.Max(Mag[i], loaded);
            Select(i);
        }

        public void Select(int i)
        {
            if (i < 0 || i >= Defs.Length || !Owned[i]) return;
            if (i != Current) Reloading = false;
            Current = i;
            for (int k = 0; k < models.Length; k++) if (models[k]) models[k].SetActive(k == i);
        }

        public void Cycle()
        {
            for (int k = 1; k <= Defs.Length; k++)
            {
                int i = (Current + k) % Defs.Length;
                if (Owned[i]) { Select(i); Sfx.Play("empty", 0.4f); return; }
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            HitMarker = Mathf.MoveTowards(HitMarker, 0, Time.unscaledDeltaTime * 4f);
            if (flash.enabled && Time.time > nextFire - Def.Rate + 0.05f) flash.enabled = false;

            if (Reloading && Time.time >= reloadEnd) FinishReload();

            // dispersão: abre ao começar a mirar/andar/atirar e fecha parada
            bool moving = p.Body.Speed > 0.3f;
            if (!p.Aiming) bloom = 4f;
            bloom = Mathf.MoveTowards(bloom, moving ? 2.2f : 0f, dt * 3.5f);
            SpreadDeg = Def.Spread + bloom;

            if (!p.CanAct || GameManager.I.AutoTestMode) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Select(3);
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel > 0.01f || wheel < -0.01f) Cycle();
            if (Input.GetKeyDown(KeyCode.R)) StartReload();
            if (Input.GetKeyDown(KeyCode.F)) Knife();
            if (Input.GetMouseButtonDown(0))
            {
                if (Current == 0) Knife();
                else if (p.Aiming) Fire();
            }
        }

        public void StartReload()
        {
            var d = Def;
            if (d.Ammo == ItemId.None || Reloading || Mag[Current] >= d.Mag) return;
            if (!Cheats.InfiniteAmmo && Reserve(Current) <= 0) { GameManager.I.Toast(Loc.Get("no_ammo")); return; }
            Reloading = true;
            reloadEnd = Time.time + d.Reload;
            Sfx.PlayAt("reload", transform.position, 0.8f, 1.6f / d.Reload);
        }

        void FinishReload()
        {
            Reloading = false;
            var d = Def;
            int need = d.Mag - Mag[Current];
            int got = Cheats.InfiniteAmmo ? need : p.Inv.Take(d.Ammo, need);
            Mag[Current] += got;
        }

        public void Fire()
        {
            var d = Def;
            if (Time.time < nextFire || Reloading || d.Ammo == ItemId.None) return;
            nextFire = Time.time + d.Rate;
            if (Mag[Current] <= 0)
            {
                Sfx.PlayAt("empty", transform.position, 0.7f);
                if (Reserve(Current) > 0 || Cheats.InfiniteAmmo) StartReload();
                else GameManager.I.Toast(Loc.Get("no_ammo"));
                return;
            }
            if (!Cheats.InfiniteAmmo) Mag[Current]--;

            var cam = GameManager.I.Cam.transform;
            Vector3 muzzle = muzzles[Current] ? muzzles[Current].position : p.Chest;
            for (int i = 0; i < d.Pellets; i++)
            {
                Vector2 r = Random.insideUnitCircle * SpreadDeg;
                Vector3 dir = Quaternion.AngleAxis(r.x, cam.up) * Quaternion.AngleAxis(r.y, cam.right) * cam.forward;
                Shoot(cam.position, dir, d, muzzle);
            }

            Sfx.PlayAt(d.Sound, muzzle, 1f, Random.Range(0.95f, 1.05f), 45f);
            Noise.Emit(p.transform.position, d.Noise);
            flash.transform.position = muzzle; flash.enabled = true;
            bloom += d.Kick * 0.8f;
            ThirdPersonCamera.I.Kick(d.Kick * 0.5f);
            ThirdPersonCamera.I.Shake(d.Kick * 0.06f);
        }

        void Shoot(Vector3 origin, Vector3 dir, WeaponDef d, Vector3 muzzle)
        {
            const float range = 60f;
            Vector3 end = origin + dir * range;
            if (d.Pierce)
            {
                var hits = Physics.RaycastAll(origin, dir, range, Layers.ShootMask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    end = h.point;
                    if (!HitSomething(h, dir, d)) break; // parede para a bala
                }
            }
            else if (Physics.Raycast(origin, dir, out var h, range, Layers.ShootMask, QueryTriggerInteraction.Ignore))
            {
                end = h.point;
                HitSomething(h, dir, d);
            }
            if (d.Pellets == 1 || Random.value < 0.35f) Fx.Tracer(muzzle, end);
        }

        /// <summary>Devolve true se acertou um inimigo (a bala perfurante continua).</summary>
        bool HitSomething(RaycastHit h, Vector3 dir, WeaponDef d)
        {
            var zone = h.collider.GetComponent<HitZone>();
            if (zone != null && zone.Owner != null && !zone.Owner.Dead)
            {
                zone.Owner.TakeHit(d.Damage * zone.Mult, h.point, dir, d.Power, zone.Head);
                HitMarker = 1f;
                return true;
            }
            if (zone != null) return true;
            Fx.Spark(h.point, h.normal);
            Fx.Decal(h.point, h.normal, 0.09f, Mats.BulletHole());
            return false;
        }

        public void Knife()
        {
            if (Time.time < nextFire) return;
            if (KnifeDur <= 0 && !Cheats.UnbreakableKnife) { GameManager.I.Toast(Loc.Get("knife_broken")); nextFire = Time.time + 0.5f; return; }
            var d = Defs[0];
            nextFire = Time.time + d.Rate;
            p.Body.Flinch(-0.6f);
            Sfx.PlayAt("knife", transform.position, 0.8f, Random.Range(0.9f, 1.1f));
            Vector3 c = p.Chest + p.transform.forward * 0.9f;
            Zombie best = null; float bd = 9f; Collider bc = null;
            foreach (var col in Physics.OverlapSphere(c, 0.9f, 1 << Layers.Enemy, QueryTriggerInteraction.Ignore))
            {
                var z = col.GetComponent<HitZone>();
                if (z == null || z.Owner == null || z.Owner.Dead) continue;
                float dist = Vector3.Distance(c, col.bounds.center);
                if (dist < bd) { bd = dist; best = z.Owner; bc = col; }
            }
            if (best == null) return;
            var hz = bc.GetComponent<HitZone>();
            best.TakeHit(d.Damage * (hz.Head ? 1.5f : 1f), bc.bounds.center, p.transform.forward, d.Power, false);
            HitMarker = 1f;
            if (!Cheats.UnbreakableKnife)
            {
                KnifeDur -= 6f;
                if (KnifeDur <= 0) { KnifeDur = 0; GameManager.I.Toast(Loc.Get("knife_broken")); }
            }
        }

        // ---------------- modelos das armas na mão ----------------

        void BuildModels()
        {
            var hand = p.Body.Hand;
            var dark = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.5f, 0.7f);
            var steel = Mats.Lit(new Color(0.6f, 0.62f, 0.65f), 0.7f, 0.9f);
            var wood = Mats.Lit(new Color(0.3f, 0.18f, 0.1f), 0.3f, 0f, Mats.Wood());
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject(Defs[i].Key);
                g.layer = Layers.Player;
                g.transform.SetParent(hand, false);
                g.transform.localRotation = Quaternion.Euler(90, 0, 0); // cano segue o braço
                models[i] = g;
                var m = new GameObject("Boca").transform;
                m.SetParent(g.transform, false);
                muzzles[i] = m;
            }
            // faca
            Mats.Part(models[0].transform, PrimitiveType.Cube, new Vector3(0, 0, 0.02f), new Vector3(0.03f, 0.035f, 0.11f), dark);
            Mats.Part(models[0].transform, PrimitiveType.Cube, new Vector3(0, 0.005f, 0.15f), new Vector3(0.008f, 0.03f, 0.17f), steel);
            // pistola
            Mats.Part(models[1].transform, PrimitiveType.Cube, new Vector3(0, -0.05f, -0.01f), new Vector3(0.035f, 0.11f, 0.05f), dark, "cabo", new Vector3(-15, 0, 0));
            Mats.Part(models[1].transform, PrimitiveType.Cube, new Vector3(0, 0.02f, 0.06f), new Vector3(0.04f, 0.045f, 0.2f), dark, "ferrolho");
            muzzles[1].localPosition = new Vector3(0, 0.02f, 0.17f);
            // escopeta
            Mats.Part(models[2].transform, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0.3f), new Vector3(0.035f, 0.33f, 0.035f), dark, "cano", new Vector3(90, 0, 0));
            Mats.Part(models[2].transform, PrimitiveType.Cube, new Vector3(0, 0f, 0.28f), new Vector3(0.05f, 0.05f, 0.16f), wood, "telha");
            Mats.Part(models[2].transform, PrimitiveType.Cube, new Vector3(0, -0.02f, -0.12f), new Vector3(0.05f, 0.09f, 0.3f), wood, "coronha", new Vector3(-8, 0, 0));
            muzzles[2].localPosition = new Vector3(0, 0.03f, 0.64f);
            // magnum
            Mats.Part(models[3].transform, PrimitiveType.Cube, new Vector3(0, -0.06f, -0.02f), new Vector3(0.04f, 0.12f, 0.05f), wood, "cabo", new Vector3(-15, 0, 0));
            Mats.Part(models[3].transform, PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0.03f), new Vector3(0.06f, 0.03f, 0.06f), steel, "tambor", new Vector3(90, 0, 0));
            Mats.Part(models[3].transform, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0.16f), new Vector3(0.025f, 0.1f, 0.025f), steel, "cano", new Vector3(90, 0, 0));
            muzzles[3].localPosition = new Vector3(0, 0.03f, 0.27f);
            muzzles[0].localPosition = new Vector3(0, 0, 0.2f);
            foreach (var g in models)
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Layers.Player;
        }
    }
}
