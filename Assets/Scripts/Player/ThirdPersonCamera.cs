using UnityEngine;

namespace Ravenbrook
{
    /// <summary>Câmera sobre o ombro (aproxima ao mirar) e passeio lento na tela de título.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera I;
        public Transform Target;
        public float Yaw, Pitch = 6f;
        public Vector3 TitleCenter;

        Camera cam;
        float aimBlend, shake, titleT;

        void Awake()
        {
            I = this;
            cam = GetComponent<Camera>();
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }
        public void Kick(float degrees) { Pitch -= degrees; Yaw += Random.Range(-degrees, degrees) * 0.3f; }

        public void SnapBehind(Transform t)
        {
            Target = t;
            Yaw = t.eulerAngles.y; Pitch = 6f;
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (Target == null)
            {
                // tela de título: órbita lenta pelo saguão
                titleT += Time.unscaledDeltaTime;
                float a = titleT * 0.06f;
                Vector3 pos = TitleCenter + new Vector3(Mathf.Sin(a) * 6.5f, 2.3f + Mathf.Sin(titleT * 0.2f) * 0.3f, Mathf.Cos(a) * 6.5f);
                transform.position = pos;
                transform.rotation = Quaternion.LookRotation(TitleCenter + Vector3.up * 1.4f - pos);
                cam.fieldOfView = 55f;
                return;
            }

            var p = gm.Player;
            bool act = p != null && p.CanAct && !gm.AutoTestMode;
            if (act)
            {
                float sens = Settings.Sensitivity * (p.Aiming ? 0.6f : 1f);
                Yaw += Input.GetAxisRaw("Mouse X") * sens;
                Pitch -= Input.GetAxisRaw("Mouse Y") * sens;
            }
            Pitch = Mathf.Clamp(Pitch, -40f, 60f);

            float dt = Time.unscaledDeltaTime;
            aimBlend = Mathf.MoveTowards(aimBlend, p != null && p.Aiming ? 1 : 0, dt * 6f);
            float ease = aimBlend * aimBlend * (3 - 2 * aimBlend);

            Vector3 pivot = Target.position + Vector3.up * (p != null && p.Dead ? 0.6f : 1.55f);
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0);
            Vector3 off = Vector3.Lerp(new Vector3(0.5f, 0.12f, -2.7f), new Vector3(0.62f, 0.05f, -1.15f), ease);

            Vector3 shoulder = pivot + rot * new Vector3(off.x, off.y, 0);
            if (Physics.SphereCast(pivot, 0.18f, shoulder - pivot, out var h1, (shoulder - pivot).magnitude, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                shoulder = pivot + (shoulder - pivot).normalized * Mathf.Max(0, h1.distance - 0.05f);
            Vector3 back = rot * new Vector3(0, 0, off.z);
            Vector3 desired = shoulder + back;
            if (Physics.SphereCast(shoulder, 0.2f, back.normalized, out var h2, back.magnitude, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                desired = shoulder + back.normalized * Mathf.Max(0.15f, h2.distance - 0.05f);

            shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
            Vector3 sh = shake > 0 ? Random.insideUnitSphere * shake * 0.08f : Vector3.zero;
            transform.SetPositionAndRotation(desired + sh, rot);
            cam.fieldOfView = Mathf.Lerp(62f, 46f, ease);
        }
    }
}
