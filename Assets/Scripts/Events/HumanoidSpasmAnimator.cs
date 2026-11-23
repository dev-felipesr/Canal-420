using UnityEngine;

namespace Canal420.Events
{
    public enum SpasmAnimMode
    {
        Idle = 0,
        LurkPeek = 1,
        TwitchDash = 2,
        FrenziedLunge = 3,
        DeadStillBrokenNeck = 4,
        VisceralImpactSnap = 5
    }

    public class HumanoidSpasmAnimator : MonoBehaviour
    {
        private static readonly int AnimModeParamHash = Animator.StringToHash("AnimMode");
        private static readonly int StateIdleHash = Animator.StringToHash("Idle");
        private static readonly int StateLurkPeekHash = Animator.StringToHash("LurkPeek");
        private static readonly int StateRunRetreatHash = Animator.StringToHash("RunRetreat");
        private static readonly int StateFrenziedLungeHash = Animator.StringToHash("FrenziedLunge");
        private static readonly int StateDeadStillBrokenNeckHash = Animator.StringToHash("DeadStillBrokenNeck");
        private static readonly int StateVisceralAttackLungeHash = Animator.StringToHash("VisceralAttackLunge");

        [SerializeField] private SpasmAnimMode mode = SpasmAnimMode.Idle;

        private Animator skeletalAnimator;
        private Transform hips;
        private Transform head;
        private Transform neck;
        private Transform shoulders;
        private Transform torso;
        private Transform armLeft;
        private Transform forearmLeft;
        private Transform handLeft;
        private Transform armRight;
        private Transform forearmRight;
        private Transform handRight;
        private Transform legLeft;
        private Transform shinLeft;
        private Transform legRight;
        private Transform shinRight;
        private Transform mouth;
        private Transform hairDrape;

        private Quaternion hipsBaseRot;
        private Quaternion headBaseRot;
        private Quaternion neckBaseRot;
        private Quaternion shouldersBaseRot;
        private Quaternion torsoBaseRot;
        private Quaternion armLeftBaseRot;
        private Quaternion forearmLeftBaseRot;
        private Quaternion handLeftBaseRot;
        private Quaternion armRightBaseRot;
        private Quaternion forearmRightBaseRot;
        private Quaternion handRightBaseRot;
        private Quaternion legLeftBaseRot;
        private Quaternion shinLeftBaseRot;
        private Quaternion legRightBaseRot;
        private Quaternion shinRightBaseRot;
        private Quaternion hairDrapeBaseRot;

        private Vector3 hipsBasePos;
        private Vector3 headBasePos;
        private Vector3 neckBasePos;
        private Vector3 shouldersBasePos;
        private Vector3 torsoBasePos;
        private Vector3 armLeftBasePos;
        private Vector3 armRightBasePos;
        private Vector3 legLeftBasePos;
        private Vector3 legRightBasePos;
        private Vector3 mouthBaseScale = Vector3.one;

        private bool cached = false;
        private float seed;
        private int enabledFrame = -1;

        public SpasmAnimMode CurrentMode => mode;
        public Animator SkeletalAnimator
        {
            get
            {
                if (skeletalAnimator == null) skeletalAnimator = GetComponent<Animator>();
                return skeletalAnimator;
            }
        }

        private bool ShouldDriveViaSkeletalAnimator()
        {
            return Application.isPlaying
                && skeletalAnimator != null
                && skeletalAnimator.runtimeAnimatorController != null
                && skeletalAnimator.isActiveAndEnabled;
        }

        private static int GetStateHashForMode(SpasmAnimMode animMode)
        {
            switch (animMode)
            {
                case SpasmAnimMode.LurkPeek: return StateLurkPeekHash;
                case SpasmAnimMode.TwitchDash: return StateRunRetreatHash;
                case SpasmAnimMode.FrenziedLunge: return StateFrenziedLungeHash;
                case SpasmAnimMode.DeadStillBrokenNeck: return StateDeadStillBrokenNeckHash;
                case SpasmAnimMode.VisceralImpactSnap: return StateVisceralAttackLungeHash;
                default: return StateIdleHash;
            }
        }

        private void Awake()
        {
            seed = Mathf.Abs((name + transform.position.ToString()).GetHashCode() * 0.00137f) % 10f;
            CacheJoints();
        }

        private void OnEnable()
        {
            enabledFrame = Time.frameCount;
            CacheJoints();
            SyncAnimatorModeParameter(snapImmediate: mode != SpasmAnimMode.Idle);
        }

        public void SetMode(SpasmAnimMode newMode)
        {
            SetMode(newMode, immediate: false);
        }

        public void SetMode(SpasmAnimMode newMode, bool immediate)
        {
            CacheJoints();
            SpasmAnimMode previousMode = mode;
            bool changed = previousMode != newMode;
            mode = newMode;

            bool shouldSnapImmediate = immediate
                || enabledFrame == Time.frameCount
                || newMode == SpasmAnimMode.VisceralImpactSnap
                || (previousMode == SpasmAnimMode.Idle && newMode == SpasmAnimMode.TwitchDash);

            SyncAnimatorModeParameter(snapImmediate: shouldSnapImmediate);

            if (changed && mode == SpasmAnimMode.Idle)
            {
                ResetPose();
            }
        }

        private void SyncAnimatorModeParameter(bool snapImmediate = false)
        {
            if (ShouldDriveViaSkeletalAnimator())
            {
                skeletalAnimator.SetInteger(AnimModeParamHash, (int)mode);
                if (snapImmediate)
                {
                    skeletalAnimator.Play(GetStateHashForMode(mode), 0, 0f);
                    skeletalAnimator.Update(0f);
                    SyncConnectedJointPositions();
                }
            }
        }

        public void ResetPose()
        {
            CacheJoints();
            if (hips != null)
            {
                hips.localPosition = hipsBasePos;
                hips.localRotation = hipsBaseRot;
            }
            if (torso != null)
            {
                torso.localPosition = torsoBasePos;
                torso.localRotation = torsoBaseRot;
            }
            if (shoulders != null)
            {
                shoulders.localPosition = shouldersBasePos;
                shoulders.localRotation = shouldersBaseRot;
            }
            if (armLeft != null)
            {
                armLeft.localPosition = armLeftBasePos;
                armLeft.localRotation = armLeftBaseRot;
            }
            if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot;
            if (handLeft != null) handLeft.localRotation = handLeftBaseRot;
            if (armRight != null)
            {
                armRight.localPosition = armRightBasePos;
                armRight.localRotation = armRightBaseRot;
            }
            if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot;
            if (handRight != null) handRight.localRotation = handRightBaseRot;
            if (legLeft != null)
            {
                legLeft.localPosition = legLeftBasePos;
                legLeft.localRotation = legLeftBaseRot;
            }
            if (shinLeft != null) shinLeft.localRotation = shinLeftBaseRot;
            if (legRight != null)
            {
                legRight.localPosition = legRightBasePos;
                legRight.localRotation = legRightBaseRot;
            }
            if (shinRight != null) shinRight.localRotation = shinRightBaseRot;
            if (neck != null)
            {
                neck.localPosition = neckBasePos;
                neck.localRotation = neckBaseRot;
            }
            if (head != null)
            {
                head.localPosition = headBasePos;
                head.localRotation = headBaseRot;
            }
            if (mouth != null) mouth.localScale = mouthBaseScale;
            if (hairDrape != null) hairDrape.localRotation = hairDrapeBaseRot;

            if (ShouldDriveViaSkeletalAnimator())
            {
                skeletalAnimator.SetInteger(AnimModeParamHash, (int)mode);
                if (mode == SpasmAnimMode.Idle)
                {
                    skeletalAnimator.Play(StateIdleHash, 0, 0f);
                    skeletalAnimator.Update(0f);
                }
            }
        }

        private float proceduralTime = 0f;

        private void LateUpdate()
        {
            CacheJoints();

            // At runtime with a bound skeletal AnimatorController, Unity's Animator evaluates the real .anim
            // AnimationClip bone rotations right before LateUpdate. We then run exact forward-kinematic coupling
            // across sibling upper-body bones so every character scale (Margaret 1.0, Child 0.69, Cian 1.03) stays seamless.
            if (ShouldDriveViaSkeletalAnimator())
            {
                SyncConnectedJointPositions();
                return;
            }

            if (mode == SpasmAnimMode.Idle)
                return;

            if (Application.isPlaying && Time.timeScale <= 0.001f)
                return;

            proceduralTime += Time.deltaTime;
            float t = proceduralTime + seed;

            switch (mode)
            {
                case SpasmAnimMode.LurkPeek:
                {
                    float breath = Mathf.Sin(t * 1.35f);
                    if (head != null) head.localRotation = headBaseRot * Quaternion.Euler(2.5f + breath * 0.9f, 0f, -5.5f + breath * 1.1f);
                    if (neck != null) neck.localRotation = neckBaseRot * Quaternion.Euler(1.5f, 0f, -2.0f + breath * 0.5f);
                    if (torso != null) torso.localRotation = torsoBaseRot * Quaternion.Euler(1.2f + breath * 0.8f, 0f, breath * 0.4f);
                    if (shoulders != null) shoulders.localRotation = shouldersBaseRot * Quaternion.Euler(breath * 0.8f, 0f, 0f);
                    if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot * Quaternion.Euler(-12f, 0f, 0f);
                    if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot * Quaternion.Euler(-12f, 0f, 0f);
                    if (legLeft != null) legLeft.localRotation = legLeftBaseRot;
                    if (shinLeft != null) shinLeft.localRotation = shinLeftBaseRot;
                    if (legRight != null) legRight.localRotation = legRightBaseRot;
                    if (shinRight != null) shinRight.localRotation = shinRightBaseRot;
                    if (hairDrape != null) hairDrape.localRotation = hairDrapeBaseRot * Quaternion.Euler(breath * 1.5f, 0f, -breath * 1.2f);
                    break;
                }

                case SpasmAnimMode.TwitchDash:
                {
                    float stride = Mathf.Sin(t * 14.5f);
                    float bob = Mathf.Abs(Mathf.Cos(t * 14.5f));
                    if (head != null) head.localRotation = headBaseRot * Quaternion.Euler(5f + bob * 3f, 0f, -4f);
                    if (torso != null) torso.localRotation = torsoBaseRot * Quaternion.Euler(11f + bob * 2f, stride * 5f, stride * 1.5f);
                    if (armLeft != null) armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(-18f - stride * 32f, 0f, -5f);
                    if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot * Quaternion.Euler(-38f - Mathf.Max(0f, -stride) * 26f, 0f, 0f);
                    if (armRight != null) armRight.localRotation = armRightBaseRot * Quaternion.Euler(-18f + stride * 32f, 0f, 5f);
                    if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot * Quaternion.Euler(-38f - Mathf.Max(0f, stride) * 26f, 0f, 0f);
                    if (legLeft != null) legLeft.localRotation = legLeftBaseRot * Quaternion.Euler(stride * 38f, 0f, 0f);
                    if (shinLeft != null) shinLeft.localRotation = shinLeftBaseRot * Quaternion.Euler(Mathf.Max(0f, stride) * 52f + 10f, 0f, 0f);
                    if (legRight != null) legRight.localRotation = legRightBaseRot * Quaternion.Euler(-stride * 38f, 0f, 0f);
                    if (shinRight != null) shinRight.localRotation = shinRightBaseRot * Quaternion.Euler(Mathf.Max(0f, -stride) * 52f + 10f, 0f, 0f);
                    break;
                }

                case SpasmAnimMode.FrenziedLunge:
                {
                    float twitchZ = Mathf.Sin(t * 47f) * 14f + Mathf.Cos(t * 31f) * 6f;
                    float twitchX = Mathf.Sin(t * 39f) * 9f;
                    if (head != null) head.localRotation = headBaseRot * Quaternion.Euler(twitchX, Mathf.Cos(t * 41f) * 8f, twitchZ);
                    if (neck != null) neck.localRotation = neckBaseRot * Quaternion.Euler(-6f, 0f, twitchZ * 0.35f);
                    if (torso != null) torso.localRotation = torsoBaseRot * Quaternion.Euler(12f + Mathf.Sin(t * 24f) * 3f, 0f, Mathf.Cos(t * 29f) * 3f);
                    if (shoulders != null) shoulders.localRotation = shouldersBaseRot * Quaternion.Euler(6f, 0f, Mathf.Sin(t * 34f) * 4f);
                    if (armLeft != null) armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(-74f + Mathf.Sin(t * 26f) * 12f, -12f, -10f);
                    if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot * Quaternion.Euler(-32f, 0f, 0f);
                    if (armRight != null) armRight.localRotation = armRightBaseRot * Quaternion.Euler(-78f - Mathf.Cos(t * 28f) * 12f, 12f, 10f);
                    if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot * Quaternion.Euler(-34f, 0f, 0f);
                    if (mouth != null)
                    {
                        float screamOpen = 1.65f + Mathf.Abs(Mathf.Sin(t * 24f)) * 0.35f;
                        mouth.localScale = new Vector3(mouthBaseScale.x * 1.15f, mouthBaseScale.y * screamOpen, mouthBaseScale.z);
                    }
                    break;
                }

                case SpasmAnimMode.DeadStillBrokenNeck:
                {
                    float microTension = Mathf.Sin(t * 19f) * 0.45f;
                    if (head != null) head.localRotation = headBaseRot * Quaternion.Euler(9f + microTension * 0.4f, -4f, -22f - microTension);
                    if (neck != null) neck.localRotation = neckBaseRot * Quaternion.Euler(5f, 0f, -6f);
                    if (torso != null) torso.localRotation = torsoBaseRot * Quaternion.Euler(4f, 0f, -2.5f);
                    if (shoulders != null) shoulders.localRotation = shouldersBaseRot * Quaternion.Euler(4f, 0f, -4.5f);
                    if (armLeft != null) armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(-22f, -4f, -5f);
                    if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot * Quaternion.Euler(-24f, 0f, 0f);
                    if (armRight != null) armRight.localRotation = armRightBaseRot * Quaternion.Euler(-18f, 4f, 6f);
                    if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot * Quaternion.Euler(-20f, 0f, 0f);
                    if (mouth != null)
                    {
                        mouth.localScale = new Vector3(mouthBaseScale.x * 1.04f, mouthBaseScale.y * 1.28f, mouthBaseScale.z);
                    }
                    if (hairDrape != null) hairDrape.localRotation = hairDrapeBaseRot * Quaternion.Euler(4f, 0f, -8f);
                    break;
                }

                case SpasmAnimMode.VisceralImpactSnap:
                {
                    if (head != null) head.localRotation = headBaseRot * Quaternion.Euler(-8f, 2f, -12f);
                    if (neck != null) neck.localRotation = neckBaseRot * Quaternion.Euler(11f, 0f, -4f);
                    if (torso != null) torso.localRotation = torsoBaseRot * Quaternion.Euler(12f, 0f, 1.5f);
                    if (shoulders != null) shoulders.localRotation = shouldersBaseRot * Quaternion.Euler(8f, 0f, 2.0f);
                    if (armLeft != null) armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(-66f, -12f, -10f);
                    if (forearmLeft != null) forearmLeft.localRotation = forearmLeftBaseRot * Quaternion.Euler(-28f, 0f, 0f);
                    if (armRight != null) armRight.localRotation = armRightBaseRot * Quaternion.Euler(-70f, 12f, 10f);
                    if (forearmRight != null) forearmRight.localRotation = forearmRightBaseRot * Quaternion.Euler(-30f, 0f, 0f);
                    if (mouth != null)
                    {
                        mouth.localScale = new Vector3(mouthBaseScale.x * 1.18f, mouthBaseScale.y * 1.32f, mouthBaseScale.z);
                    }
                    if (hairDrape != null) hairDrape.localRotation = hairDrapeBaseRot * Quaternion.Euler(-14f, 0f, 6f);
                    break;
                }
            }

            SyncConnectedJointPositions();
        }

        private void SyncConnectedJointPositions()
        {
            Quaternion deltaHips = hips != null
                ? hips.localRotation * Quaternion.Inverse(hipsBaseRot)
                : Quaternion.identity;

            Vector3 currentTorsoPos = torsoBasePos;
            if (torso != null)
            {
                currentTorsoPos = hips != null
                    ? hipsBasePos + deltaHips * (torsoBasePos - hipsBasePos)
                    : torsoBasePos;
                torso.localPosition = currentTorsoPos;
            }
            if (hips != null)
            {
                if (legLeft != null)
                {
                    legLeft.localPosition = hipsBasePos + deltaHips * (legLeftBasePos - hipsBasePos);
                }
                if (legRight != null)
                {
                    legRight.localPosition = hipsBasePos + deltaHips * (legRightBasePos - hipsBasePos);
                }
            }

            Quaternion deltaTorso = torso != null
                ? torso.localRotation * Quaternion.Inverse(torsoBaseRot)
                : deltaHips;

            Vector3 currentShouldersPos = currentTorsoPos + deltaTorso * (shouldersBasePos - torsoBasePos);
            if (shoulders != null)
            {
                shoulders.localPosition = currentShouldersPos;
            }

            Quaternion deltaShoulders = shoulders != null
                ? shoulders.localRotation * Quaternion.Inverse(shouldersBaseRot)
                : deltaTorso;

            if (armLeft != null)
            {
                armLeft.localPosition = currentShouldersPos + deltaShoulders * (armLeftBasePos - shouldersBasePos);
            }
            if (armRight != null)
            {
                armRight.localPosition = currentShouldersPos + deltaShoulders * (armRightBasePos - shouldersBasePos);
            }

            Vector3 currentNeckPos = currentShouldersPos + deltaShoulders * (neckBasePos - shouldersBasePos);
            if (neck != null)
            {
                neck.localPosition = currentNeckPos;
            }
            if (head != null)
            {
                Quaternion deltaNeck = neck != null
                    ? neck.localRotation * Quaternion.Inverse(neckBaseRot)
                    : deltaShoulders;
                head.localPosition = currentNeckPos + deltaNeck * (headBasePos - neckBasePos);
            }
        }

        private void CacheJoints()
        {
            if (cached) return;
            cached = true;

            skeletalAnimator = GetComponent<Animator>();
            hips = transform.Find("Bone_Hips");
            head = transform.Find("Cabeca_Silhueta");
            neck = transform.Find("Pescoco_Inclinado");
            shoulders = transform.Find("Ombros_Curvados");
            torso = transform.Find("Torso_Esguio");
            armLeft = transform.Find("Braco_Esq");
            armRight = transform.Find("Braco_Dir");
            legLeft = transform.Find("Perna_Esq");
            legRight = transform.Find("Perna_Dir");

            if (armLeft != null)
            {
                forearmLeft = armLeft.Find("Antebraco_Esq");
                if (forearmLeft != null) handLeft = forearmLeft.Find("Mao_Esq");
            }
            if (armRight != null)
            {
                forearmRight = armRight.Find("Antebraco_Dir");
                if (forearmRight != null) handRight = forearmRight.Find("Mao_Dir");
            }
            if (legLeft != null) shinLeft = legLeft.Find("Canela_Esq");
            if (legRight != null) shinRight = legRight.Find("Canela_Dir");
            if (head != null)
            {
                mouth = head.Find("Boca_Escura");
                hairDrape = head.Find("Cabelo_Caido_Esq");
            }

            if (hips != null)
            {
                hipsBaseRot = hips.localRotation;
                hipsBasePos = hips.localPosition;
            }
            if (head != null)
            {
                headBaseRot = head.localRotation;
                headBasePos = head.localPosition;
            }
            if (neck != null)
            {
                neckBaseRot = neck.localRotation;
                neckBasePos = neck.localPosition;
            }
            if (shoulders != null)
            {
                shouldersBaseRot = shoulders.localRotation;
                shouldersBasePos = shoulders.localPosition;
            }
            if (torso != null)
            {
                torsoBaseRot = torso.localRotation;
                torsoBasePos = torso.localPosition;
            }
            if (armLeft != null)
            {
                armLeftBaseRot = armLeft.localRotation;
                armLeftBasePos = armLeft.localPosition;
            }
            if (forearmLeft != null) forearmLeftBaseRot = forearmLeft.localRotation;
            if (handLeft != null) handLeftBaseRot = handLeft.localRotation;
            if (armRight != null)
            {
                armRightBaseRot = armRight.localRotation;
                armRightBasePos = armRight.localPosition;
            }
            if (forearmRight != null) forearmRightBaseRot = forearmRight.localRotation;
            if (handRight != null) handRightBaseRot = handRight.localRotation;
            if (legLeft != null)
            {
                legLeftBaseRot = legLeft.localRotation;
                legLeftBasePos = legLeft.localPosition;
            }
            if (shinLeft != null) shinLeftBaseRot = shinLeft.localRotation;
            if (legRight != null)
            {
                legRightBaseRot = legRight.localRotation;
                legRightBasePos = legRight.localPosition;
            }
            if (shinRight != null) shinRightBaseRot = shinRight.localRotation;
            if (mouth != null) mouthBaseScale = mouth.localScale;
            if (hairDrape != null) hairDrapeBaseRot = hairDrape.localRotation;
        }
    }
}
