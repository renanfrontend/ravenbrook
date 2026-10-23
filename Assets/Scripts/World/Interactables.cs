using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ravenbrook
{
    /// <summary>Qualquer coisa que o jogador usa com E.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
        public virtual Vector3 Point => transform.position;
        public virtual float Range => 2.1f;
        public virtual bool CanInteract => true;
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerController p);
    }

    /// <summary>Item no chão: erva, munição, chave, medalhão, arma ou documento.</summary>
    public class Pickup : Interactable
    {
        public string Id; public ItemId Item; public int Count = 1; public int Weapon = -1; public int Loaded; public int Doc = -1; public int Room;
        Transform glint; float t0;

        public string Label => Doc >= 0 ? Documents.Title(Doc) : Weapon >= 0 ? Loc.Get(WeaponSystem.Defs[Weapon].Key) : ItemDB.Name(Item) + (Count > 1 ? " ×" + Count : "");
        public override string Prompt => (Doc >= 0 ? Loc.Get("read") : Loc.Get("take")) + ": " + Label;
        public override Vector3 Point => transform.position + Vector3.up * 0.3f;
        public override float Range => 2.3f;

        void Start()
        {
            t0 = Random.value * 10f;
            var g = new GameObject("brilho");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Vector3.up * 0.35f;
            g.AddComponent<MeshFilter>().sharedMesh = Mats.Prim(PrimitiveType.Quad);
            g.AddComponent<MeshRenderer>().sharedMaterial = Mats.Glow(new Color(1f, 0.95f, 0.7f, 0.9f));
            glint = g.transform;
        }

        void Update()
        {
            if (glint == null || GameManager.I.Cam == null) return;
            float k = Mathf.Pow(Mathf.Abs(Mathf.Sin((Time.time + t0) * 1.6f)), 6f);
            glint.localScale = Vector3.one * (0.06f + 0.22f * k);
            glint.rotation = GameManager.I.Cam.transform.rotation;
        }

        public override void Interact(PlayerController p)
        {
            var gm = GameManager.I;
            if (Doc >= 0)
            {
                gm.Flags.Add(Id);
                gm.ReadDocument(Doc);
                Destroy(gameObject);
                return;
            }
            if (Weapon >= 0)
            {
                p.Weapons.Give((WeaponId)Weapon, Loaded);
                Done(gm, Loc.F("picked", Label));
                return;
            }
            int left = p.Inv.Add(Item, Count);
            if (left == Count) { gm.Toast(Loc.Get("inv_full")); Sfx.Play("locked", 0.5f); return; }
            string label = ItemDB.Name(Item) + (Count - left > 1 ? " ×" + (Count - left) : "");
            if (left > 0) { Count = left; gm.Toast(Loc.F("picked", label)); Sfx.Play("pickup"); return; }
            Done(gm, Loc.F("picked", label));
            if (ItemDB.Get(Item).Kind == ItemKind.Medal) gm.OnMedalFound(Item);
        }

        void Done(GameManager gm, string msg)
        {
            gm.Flags.Add(Id);
            gm.Toast(msg);
            Sfx.Play("pickup");
            gm.Level.Pickups.Remove(this);
            Destroy(gameObject);
        }
    }

    /// <summary>Porta comum, trancada por chave ou portão de metal ligado à estátua.</summary>
    public class Door : Interactable
    {
        public string Id; public ItemId Key = ItemId.None; public bool Gate; public bool Open; public int CellX, CellR;
        Transform panel; NavMeshObstacle obstacle; Collider panelCol;
        float angle, targetAngle, lift;

        public override Vector3 Point => transform.position + Vector3.up * 1.2f;
        public override float Range => 2.4f;
        public override bool CanInteract => !Open;
        public bool Locked => !Open && (Gate || Key != ItemId.None);
        public override string Prompt => Gate ? Loc.Get("examine") : Loc.Get("open");

        /// <summary>
        /// O objeto da porta já vem girado pelo LevelBuilder: no espaço local, a abertura
        /// vai de -width/2 a +width/2 no eixo X e a parede é o plano Z = 0.
        /// </summary>
        public void Build(float width, Material mat, Material trim, bool startOpen)
        {
            var hinge = new GameObject("Dobradica").transform;
            hinge.SetParent(transform, false);
            if (Gate)
            {
                var g = Mats.Part(hinge, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(width, 2.6f, 0.12f), mat, "Portao");
                for (int i = 0; i < 9; i++)
                    Mats.Part(g.transform, PrimitiveType.Cube, new Vector3(0, -0.45f + i * 0.11f, 0.6f), new Vector3(1f, 0.025f, 0.3f), trim, "friso");
                panelCol = g.AddComponent<BoxCollider>();
            }
            else
            {
                // dobradiça na lateral esquerda da abertura; o painel gira em torno dela
                hinge.localPosition = new Vector3(-width * 0.5f, 0, 0);
                var g = Mats.Part(hinge, PrimitiveType.Cube, new Vector3(width * 0.5f, 1.2f, 0), new Vector3(width - 0.04f, 2.4f, 0.08f), mat, "Painel");
                Mats.Part(g.transform, PrimitiveType.Sphere, new Vector3(0.4f, 0, 0.9f), new Vector3(0.05f, 0.025f, 0.8f), trim, "macaneta");
                Mats.Part(g.transform, PrimitiveType.Sphere, new Vector3(0.4f, 0, -0.9f), new Vector3(0.05f, 0.025f, 0.8f), trim, "macaneta");
                if (Key != ItemId.None)
                {
                    var c = ItemDB.Get(Key).Color;
                    foreach (float z in new[] { 0.6f, -0.6f })
                        Mats.Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, z), new Vector3(0.25f, 0.02f, 0.11f),
                            Mats.Emissive(c * 0.6f, c * 0.35f), "emblema", new Vector3(90, 0, 0));
                }
                panelCol = g.AddComponent<BoxCollider>();
            }
            panel = hinge;
            foreach (var t in GetComponentsInChildren<Transform>()) t.gameObject.layer = Layers.Door;

            var ob = new GameObject("BloqueioNavMesh");
            ob.transform.SetParent(transform, false);
            ob.layer = Layers.Door;
            obstacle = ob.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0, 1.2f, 0);
            obstacle.size = new Vector3(width + 0.4f, 2.4f, 0.8f);
            obstacle.carving = true;
            if (startOpen) { Open = true; SetOpenImmediate(); }
        }

        void SetOpenImmediate()
        {
            obstacle.enabled = false;
            panelCol.enabled = false;
            if (Gate) { lift = 2.5f; panel.localPosition = Vector3.up * lift; }
            else { angle = targetAngle = 100f; ApplyAngle(); }
        }

        public override void Interact(PlayerController p)
        {
            var gm = GameManager.I;
            if (Gate) { gm.Toast(Loc.Get("gate_closed")); Sfx.PlayAt("locked", Point, 0.8f); return; }
            if (Key != ItemId.None)
            {
                if (!p.Inv.Has(Key)) { gm.Toast(Loc.F("locked", ItemDB.Name(Key))); Sfx.PlayAt("locked", Point, 0.8f); return; }
                p.Inv.Take(Key, 1);
                gm.Toast(Loc.F("unlocked", ItemDB.Name(Key)));
            }
            OpenFrom(p.transform.position);
        }

        public void OpenFrom(Vector3 from)
        {
            if (Open) return;
            Open = true;
            GameManager.I.Flags.Add(Id);
            obstacle.enabled = false;
            panelCol.enabled = false;
            if (Gate) { Sfx.PlayAt("gate", transform.position, 1f, 1f, 60f); return; }
            // ângulo positivo leva o painel para -Z: abre para longe do jogador
            targetAngle = transform.InverseTransformPoint(from).z > 0 ? 100f : -100f;
            Sfx.PlayAt("door", transform.position, 0.9f);
        }

        void ApplyAngle() => panel.localRotation = Quaternion.Euler(0, angle, 0);

        void Update()
        {
            if (!Open) return;
            if (Gate)
            {
                if (lift < 2.5f) { lift = Mathf.MoveTowards(lift, 2.5f, Time.deltaTime * 1.1f); panel.localPosition = Vector3.up * lift; }
                return;
            }
            if (Mathf.Abs(angle - targetAngle) > 0.1f)
            {
                angle = Mathf.MoveTowardsAngle(angle, targetAngle, Time.deltaTime * 160f);
                ApplyAngle();
            }
        }
    }

    public class Typewriter : Interactable
    {
        public override string Prompt => Loc.Get("save");
        public override void Interact(PlayerController p) => GameManager.I.SaveGame();
        public override Vector3 Point => transform.position + Vector3.up * 0.9f;
    }

    public class ItemBoxObj : Interactable
    {
        public override string Prompt => Loc.Get("item_box");
        public override void Interact(PlayerController p) => GameManager.I.OpenItemBox();
        public override Vector3 Point => transform.position + Vector3.up * 0.6f;
    }

    /// <summary>Estátua do saguão com três encaixes para os medalhões.</summary>
    public class Statue : Interactable
    {
        public readonly GameObject[] Sockets = new GameObject[3];
        public override string Prompt => Loc.Get("examine") + ": " + Loc.Get("statue");
        public override Vector3 Point => transform.position + Vector3.up * 1.1f;
        public override float Range => 2.8f;
        public override void Interact(PlayerController p) => GameManager.I.UseStatue(p);

        public void Refresh()
        {
            var f = GameManager.I.Flags;
            for (int i = 0; i < 3; i++) if (Sockets[i]) Sockets[i].SetActive(f.Contains("medal_" + i));
        }
    }

    /// <summary>Lâmpada que pisca de vez em quando.</summary>
    public class FlickerLight : MonoBehaviour
    {
        Light l; float baseI, next; Renderer bulb; Color baseEmit; MaterialPropertyBlock mpb;
        void Start()
        {
            l = GetComponent<Light>(); baseI = l.intensity; next = Time.time + Random.Range(2f, 8f);
            bulb = transform.parent ? transform.parent.GetComponentInChildren<Renderer>() : null;
            mpb = new MaterialPropertyBlock();
            if (bulb && bulb.sharedMaterial.HasProperty("_EmissionColor")) baseEmit = bulb.sharedMaterial.GetColor("_EmissionColor");
        }
        void Update()
        {
            float k = 1f;
            if (Time.time > next)
            {
                k = Random.value < 0.5f ? 0.05f : Random.Range(0.3f, 0.8f);
                if (Time.time > next + Random.Range(0.05f, 0.4f)) next = Time.time + Random.Range(1.5f, 9f);
            }
            l.intensity = baseI * k;
            if (bulb) { mpb.SetColor("_EmissionColor", baseEmit * k); bulb.SetPropertyBlock(mpb); }
        }
    }
}
