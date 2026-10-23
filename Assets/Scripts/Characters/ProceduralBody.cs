using UnityEngine;

namespace Ravenbrook
{
    public enum BodyKind { Human, Zombie, Runner, Brute }

    /// <summary>
    /// Corpo articulado (quadril, tronco, cabeça, braços, pernas) animado por código.
    /// Usa o modelo do Blender/Sketchfab em Resources/Modelos se existir
    /// (com peças chamadas Tronco, Cabeca, BracoE, BracoD, PernaE, PernaD); senão monta com primitivas.
    /// </summary>
    public class ProceduralBody : MonoBehaviour
    {
        public Transform Root, Torso, Head, ArmL, ArmR, LegL, LegR, Hand;
        public BodyKind Kind;
        public float Speed, AimPitch;
        public bool Aim;

        float scale = 1f, phase, flinch, attackT = -1f, deathK, deathTarget, headRoll, seed;
        bool rigid;
        Animator animator;

        public bool IsLying => deathK > 0.5f;

        public static ProceduralBody Create(Transform parent, BodyKind kind, float scale, int seed)
        {
            var go = new GameObject("Corpo");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<ProceduralBody>();
            b.Kind = kind; b.scale = scale; b.seed = seed * 1.37f;
            b.Build(seed);
            return b;
        }

        void Build(int s)
        {
            Root = new GameObject("Root").transform;
            Root.SetParent(transform, false);
            Root.gameObject.layer = gameObject.layer;
            Root.localScale = Vector3.one * scale;

            string model = Kind == BodyKind.Human ? "Jogador" : Kind == BodyKind.Brute ? "Brutamonte" : "Zumbi";
            var prefab = ModelLibrary.Get(model);
            if (prefab != null)
            {
                var inst = Instantiate(prefab, Root, false);
                ModelLibrary.StripColliders(inst);
                Torso = Find(inst.transform, "Tronco"); Head = Find(inst.transform, "Cabeca");
                ArmL = Find(inst.transform, "BracoE"); ArmR = Find(inst.transform, "BracoD");
                LegL = Find(inst.transform, "PernaE"); LegR = Find(inst.transform, "PernaD");
                if (Torso == null || ArmL == null || ArmR == null || LegL == null || LegR == null)
                {
                    rigid = true; // modelo inteiro (ex.: Sketchfab): só balanço e animação própria
                    ModelLibrary.FitTo(inst.transform, Root, Kind == BodyKind.Brute ? 2.1f : 1.8f);
                    animator = inst.GetComponentInChildren<Animator>();
                    var anim = inst.GetComponentInChildren<Animation>();
                    if (anim != null && anim.clip != null) { anim.wrapMode = WrapMode.Loop; anim.Play(); }
                }
                else if (Head != null && Head.parent != Torso)
                {
                    // o Blender exporta as peças soltas: prende cabeça e braços no tronco para acompanhar a inclinação
                    Head.SetParent(Torso, true); ArmL.SetParent(Torso, true); ArmR.SetParent(Torso, true);
                }
            }
            else BuildPrimitive(s);

            SetLayer(Root, gameObject.layer);
            var rnd = new System.Random(s);
            headRoll = Kind == BodyKind.Zombie || Kind == BodyKind.Runner ? (float)(rnd.NextDouble() * 30 - 15) : 0f;

            Hand = new GameObject("Mao").transform;
            Hand.SetParent(ArmR != null ? ArmR : Root, false);
            Hand.localPosition = ArmR != null ? new Vector3(0, -0.6f, 0.03f) : new Vector3(0.25f, 1.05f, 0.35f);
            foreach (var r in Root.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = Kind == BodyKind.Human ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name || t.name.StartsWith(name + ".")) return t;
            foreach (Transform c in t) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }

        Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false); t.localPosition = pos;
            return t;
        }

        void BuildPrimitive(int s)
        {
            var rnd = new System.Random(s);
            Color skin, shirt, pants, shoes = new Color(0.06f, 0.05f, 0.05f), hair;
            switch (Kind)
            {
                case BodyKind.Human:
                    skin = new Color(0.86f, 0.68f, 0.55f); shirt = new Color(0.12f, 0.17f, 0.3f); pants = new Color(0.1f, 0.11f, 0.17f); hair = new Color(0.16f, 0.1f, 0.07f);
                    break;
                case BodyKind.Brute:
                    skin = new Color(0.08f, 0.08f, 0.08f); shirt = new Color(0.13f, 0.14f, 0.13f); pants = new Color(0.1f, 0.1f, 0.1f); hair = skin;
                    break;
                default:
                    Color[] shirts = { new Color(0.55f, 0.52f, 0.45f), new Color(0.45f, 0.12f, 0.1f), new Color(0.18f, 0.25f, 0.4f), new Color(0.3f, 0.34f, 0.28f), new Color(0.12f, 0.15f, 0.26f), new Color(0.4f, 0.32f, 0.22f) };
                    Color[] pantsC = { new Color(0.15f, 0.17f, 0.25f), new Color(0.25f, 0.22f, 0.18f), new Color(0.1f, 0.1f, 0.1f), new Color(0.28f, 0.28f, 0.3f) };
                    shirt = shirts[rnd.Next(shirts.Length)]; pants = pantsC[rnd.Next(pantsC.Length)];
                    float g = (float)rnd.NextDouble() * 0.1f;
                    skin = new Color(0.52f + g, 0.58f + g, 0.47f + g); hair = new Color(0.12f, 0.1f, 0.08f);
                    break;
            }
            var mSkin = Mats.Lit(skin, 0.25f); var mShirt = Mats.Lit(shirt, 0.1f); var mPants = Mats.Lit(pants, 0.1f);
            var mShoes = Mats.Lit(shoes, 0.3f); var mHair = Mats.Lit(hair, 0.2f); var mBlood = Mats.Lit(new Color(0.28f, 0.02f, 0.02f), 0.55f);

            bool brute = Kind == BodyKind.Brute;
            float w = brute ? 1.25f : 1f;
            // quadril (fixo) e pernas
            Mats.Part(Root, PrimitiveType.Cube, new Vector3(0, 0.98f, 0), new Vector3(0.36f * w, 0.2f, 0.22f * w), mPants, "Quadril");
            LegL = Pivot("PernaE", Root, new Vector3(-0.11f * w, 0.95f, 0));
            LegR = Pivot("PernaD", Root, new Vector3(0.11f * w, 0.95f, 0));
            foreach (var leg in new[] { LegL, LegR })
            {
                Mats.Part(leg, PrimitiveType.Cube, new Vector3(0, -0.46f, 0), new Vector3(0.17f * w, 0.92f, 0.19f * w), mPants);
                Mats.Part(leg, PrimitiveType.Cube, new Vector3(0, -0.89f, 0.04f), new Vector3(0.18f * w, 0.12f, 0.29f * w), mShoes);
            }
            // tronco, cabeça e braços
            Torso = Pivot("Tronco", Root, new Vector3(0, 0.98f, 0));
            Mats.Part(Torso, PrimitiveType.Cube, new Vector3(0, 0.29f, 0), new Vector3(0.42f * w, 0.58f, 0.24f * w), mShirt);
            Head = Pivot("Cabeca", Torso, new Vector3(0, 0.55f, 0));
            Mats.Part(Head, PrimitiveType.Cylinder, new Vector3(0, 0.04f, 0), new Vector3(0.1f, 0.05f, 0.1f), mSkin);
            Mats.Part(Head, PrimitiveType.Sphere, new Vector3(0, 0.17f, 0), new Vector3(0.22f, 0.26f, 0.24f), mSkin);
            ArmL = Pivot("BracoE", Torso, new Vector3(-0.27f * w, 0.5f, 0));
            ArmR = Pivot("BracoD", Torso, new Vector3(0.27f * w, 0.5f, 0));
            foreach (var arm in new[] { ArmL, ArmR })
            {
                Mats.Part(arm, PrimitiveType.Cube, new Vector3(0, -0.2f, 0), new Vector3(0.13f * w, 0.42f, 0.13f * w), mShirt);
                Mats.Part(arm, PrimitiveType.Cube, new Vector3(0, -0.47f, 0), new Vector3(0.11f * w, 0.24f, 0.11f * w), Kind == BodyKind.Human ? mShirt : mSkin);
                Mats.Part(arm, PrimitiveType.Sphere, new Vector3(0, -0.62f, 0), new Vector3(0.1f, 0.11f, 0.1f) * w, brute ? mShoes : mSkin);
            }

            switch (Kind)
            {
                case BodyKind.Human:
                    var vest = Mats.Lit(new Color(0.07f, 0.07f, 0.08f), 0.2f);
                    Mats.Part(Torso, PrimitiveType.Cube, new Vector3(0, 0.33f, 0), new Vector3(0.45f, 0.4f, 0.27f), vest, "Colete");
                    Mats.Part(Torso, PrimitiveType.Cube, new Vector3(0.1f, 0.44f, 0.14f), new Vector3(0.07f, 0.08f, 0.01f), Mats.Lit(new Color(0.85f, 0.7f, 0.3f), 0.8f, 0.8f), "Distintivo");
                    Mats.Part(Root, PrimitiveType.Cube, new Vector3(0, 1.08f, 0), new Vector3(0.38f, 0.05f, 0.24f), vest, "Cinto");
                    Mats.Part(Head, PrimitiveType.Sphere, new Vector3(0, 0.25f, -0.015f), new Vector3(0.235f, 0.16f, 0.25f), mHair, "Cabelo");
                    Mats.Part(Head, PrimitiveType.Cube, new Vector3(0, 0.16f, -0.12f), new Vector3(0.16f, 0.16f, 0.04f), mHair, "Rabo");
                    break;
                case BodyKind.Brute:
                    // sobretudo, máscara de gás e lentes vermelhas
                    var coat = Mats.Lit(new Color(0.11f, 0.12f, 0.11f), 0.15f);
                    Mats.Part(Torso, PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.64f, 0.34f), coat, "Sobretudo");
                    Mats.Part(Root, PrimitiveType.Cube, new Vector3(0, 0.72f, 0), new Vector3(0.58f, 0.62f, 0.36f), coat, "Saia");
                    Mats.Part(Head, PrimitiveType.Sphere, new Vector3(0, 0.18f, 0), new Vector3(0.27f, 0.3f, 0.28f), Mats.Lit(new Color(0.07f, 0.07f, 0.07f), 0.5f), "Mascara");
                    var lens = Mats.Emissive(new Color(0.3f, 0.02f, 0.02f), new Color(1.2f, 0.1f, 0.05f));
                    Mats.Part(Head, PrimitiveType.Cylinder, new Vector3(-0.065f, 0.21f, 0.125f), new Vector3(0.07f, 0.01f, 0.07f), lens, "Lente", new Vector3(90, 0, 0));
                    Mats.Part(Head, PrimitiveType.Cylinder, new Vector3(0.065f, 0.21f, 0.125f), new Vector3(0.07f, 0.01f, 0.07f), lens, "Lente", new Vector3(90, 0, 0));
                    Mats.Part(Head, PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0.15f), new Vector3(0.09f, 0.05f, 0.09f), Mats.Lit(new Color(0.25f, 0.27f, 0.22f), 0.4f, 0.6f), "Filtro", new Vector3(70, 0, 0));
                    break;
                default:
                    var eye = Mats.Emissive(new Color(0.6f, 0.55f, 0.2f), new Color(0.5f, 0.42f, 0.08f));
                    Mats.Part(Head, PrimitiveType.Sphere, new Vector3(-0.05f, 0.19f, 0.105f), Vector3.one * 0.035f, eye, "Olho");
                    Mats.Part(Head, PrimitiveType.Sphere, new Vector3(0.05f, 0.19f, 0.105f), Vector3.one * 0.035f, eye, "Olho");
                    Mats.Part(Head, PrimitiveType.Cube, new Vector3(0, 0.09f, 0.11f), new Vector3(0.09f, 0.04f, 0.03f), mBlood, "Boca");
                    if (rnd.NextDouble() < 0.6) Mats.Part(Head, PrimitiveType.Sphere, new Vector3(0, 0.26f, -0.02f), new Vector3(0.23f, 0.13f, 0.24f), mHair, "Cabelo");
                    for (int i = 0; i < 3; i++)
                        Mats.Part(Torso, PrimitiveType.Cube, new Vector3((float)(rnd.NextDouble() - 0.5) * 0.3f, 0.15f + (float)rnd.NextDouble() * 0.35f, 0.122f),
                            new Vector3(0.08f + (float)rnd.NextDouble() * 0.1f, 0.06f + (float)rnd.NextDouble() * 0.12f, 0.01f), mBlood, "Sangue");
                    break;
            }
        }

        public void Flinch(float power) { flinch = Mathf.Clamp(flinch + 0.4f + power * 0.15f, 0, 1.5f); }
        public void Attack() { attackT = 0f; }
        public void Die() { deathTarget = 1f; }
        public void SetLying() { deathK = deathTarget = 1f; }
        public void Rise() { deathTarget = 0f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float spd = Speed / scale;
            float moveK = Mathf.Clamp01(spd / 2f);
            deathK = Mathf.MoveTowards(deathK, deathTarget, dt * (deathTarget > deathK ? 1.8f : 0.8f));
            float e = deathK * deathK * (3 - 2 * deathK);
            flinch = Mathf.MoveTowards(flinch, 0, dt * 3.5f);

            if (rigid)
            {
                phase += spd * 2.4f * dt;
                if (animator != null)
                    foreach (var p in animator.parameters)
                        if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) animator.SetFloat("Speed", Speed);
                Root.localRotation = Quaternion.Euler(-88 * e - flinch * 12f, 0, Mathf.Sin(phase) * 4f * moveK);
                Root.localPosition = new Vector3(0, Mathf.Abs(Mathf.Cos(phase)) * 0.04f * moveK * (1 - e) + 0.15f * e, 0);
                return;
            }

            float freq = Kind == BodyKind.Human ? 2.7f : Kind == BodyKind.Runner ? 2.6f : Kind == BodyKind.Brute ? 1.9f : 2.3f;
            phase += spd * freq * dt;
            float s = Mathf.Sin(phase);
            float legAmp = (Kind == BodyKind.Human ? 32f : Kind == BodyKind.Runner ? 42f : 24f) * moveK;
            float legL = s * legAmp, legR = -s * legAmp;
            Quaternion armL, armR;
            float torsoPitch = 0, torsoRoll = 0, headPitch = 0;
            float t = Time.time + seed;

            switch (Kind)
            {
                case BodyKind.Human:
                    if (Aim)
                    {
                        armR = Quaternion.Euler(-90 + AimPitch, 0, 0);
                        armL = Quaternion.Euler(-82 + AimPitch, 30, 0);
                        torsoPitch = AimPitch * 0.3f; headPitch = AimPitch * 0.4f;
                    }
                    else
                    {
                        armL = Quaternion.Euler(-legL * 0.8f, 0, -4);
                        armR = Quaternion.Euler(-legR * 0.8f, 0, 4);
                        torsoPitch = moveK * 5f + (spd > 3.5f ? 8f : 0f);
                    }
                    break;
                case BodyKind.Brute:
                    armL = Quaternion.Euler(-legL * 0.6f, 0, -8);
                    armR = Quaternion.Euler(-legR * 0.6f, 0, 8);
                    torsoPitch = 6; torsoRoll = s * 3f;
                    break;
                default:
                    float reach = Kind == BodyKind.Runner ? -55f : -78f;
                    armL = Quaternion.Euler(reach + Mathf.Sin(t * 1.7f) * 7f, 8, 0);
                    armR = Quaternion.Euler(reach + Mathf.Sin(t * 1.3f + 1f) * 7f, -8, 0);
                    torsoPitch = (Kind == BodyKind.Runner ? 24f : 10f) + Mathf.Sin(phase * 2f) * 3f;
                    torsoRoll = Mathf.Sin(phase) * 7f + Mathf.Sin(t * 0.7f) * 3f;
                    headPitch = -6f;
                    legR *= 0.75f;
                    break;
            }

            if (attackT >= 0f)
            {
                attackT += dt * 2.2f;
                float k = Mathf.Sin(Mathf.Clamp01(attackT) * Mathf.PI);
                armL = Quaternion.Slerp(armL, Quaternion.Euler(-105, 12, 0), k);
                armR = Quaternion.Slerp(armR, Quaternion.Euler(-105, -12, 0), k);
                torsoPitch += 25f * k;
                if (attackT >= 1f) attackT = -1f;
            }

            torsoPitch -= flinch * 22f;
            if (e > 0.01f)
            {
                armL = Quaternion.Slerp(armL, Quaternion.Euler(-150, 0, -30), e);
                armR = Quaternion.Slerp(armR, Quaternion.Euler(-150, 0, 30), e);
                legL = Mathf.Lerp(legL, -6, e); legR = Mathf.Lerp(legR, 4, e);
            }

            Torso.localRotation = Quaternion.Euler(torsoPitch, 0, torsoRoll);
            if (Head != null) Head.localRotation = Quaternion.Euler(headPitch, 0, headRoll + Mathf.Sin(t * 0.9f) * (Kind == BodyKind.Human ? 0 : 4));
            ArmL.localRotation = armL; ArmR.localRotation = armR;
            LegL.localRotation = Quaternion.Euler(legL, 0, 0);
            LegR.localRotation = Quaternion.Euler(legR, 0, 0);

            float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.035f * moveK;
            Root.localRotation = Quaternion.Euler(-88f * e, 0, 0);
            Root.localPosition = new Vector3(0, bob * (1 - e) + 0.14f * e * scale, 0);
        }
    }
}
