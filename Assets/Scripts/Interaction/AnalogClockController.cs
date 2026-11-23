using UnityEngine;

namespace Canal420.Interaction
{
    /// <summary>
    /// Controla ponteiros mecânicos de um relógio analógico de 12 horas em tempo real
    /// e permite sincronização de horário pelo DayManager ao mudar os dias do jogo.
    /// </summary>
    public class AnalogClockController : MonoBehaviour
    {
        [Header("Pivôs dos Ponteiros")]
        [SerializeField] private Transform hourHandPivot;
        [SerializeField] private Transform minuteHandPivot;
        [SerializeField] private Transform secondHandPivot;

        [Header("Horário Inicial e Modo")]
        [SerializeField] private int initialHour24 = 17;
        [SerializeField] private int initialMinute = 40;
        [SerializeField] private float initialSecond = 0f;
        [SerializeField] private bool isRunning = true;
        [SerializeField] private bool quartzDiscreteSecond = true;
        [SerializeField] private float timeScaleMultiplier = 1f;
        [SerializeField] private bool dialFacesNegativeZ = true;

        private float elapsedSeconds;
        private int lastTickSecond = -1;

        public bool IsRunning => isRunning;

        private void Awake()
        {
            elapsedSeconds = 0f;
            lastTickSecond = -1;
            ApplyHandsImmediate();
        }

        private void OnEnable()
        {
            ApplyHandsImmediate();
        }

        private void Update()
        {
            if (!isRunning)
                return;

            elapsedSeconds += Time.deltaTime * Mathf.Max(0f, timeScaleMultiplier);

            if (quartzDiscreteSecond)
            {
                int wholeSec = Mathf.FloorToInt(elapsedSeconds);
                if (wholeSec == lastTickSecond)
                    return;

                lastTickSecond = wholeSec;
            }

            ApplyHandsImmediate();
        }

        public void ConfigureClock(
            Transform hourPivot,
            Transform minPivot,
            Transform secPivot,
            int hour24,
            int minute,
            float second,
            bool running,
            bool discreteSecond = true,
            bool negativeZFace = true)
        {
            hourHandPivot = hourPivot;
            minuteHandPivot = minPivot;
            secondHandPivot = secPivot;
            quartzDiscreteSecond = discreteSecond;
            dialFacesNegativeZ = negativeZFace;
            SetClockTime(hour24, minute, second, running);
        }

        public void SetClockTime(int hour24, int minute, float second, bool running)
        {
            initialHour24 = ((hour24 % 24) + 24) % 24;
            initialMinute = ((minute % 60) + 60) % 60;
            initialSecond = Mathf.Repeat(second, 60f);
            isRunning = running;
            elapsedSeconds = 0f;
            lastTickSecond = -1;
            ApplyHandsImmediate();
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
        }

        public void GetDisplayedTime(out int hour24, out int minute, out int second)
        {
            double baseTotal = initialHour24 * 3600.0 + initialMinute * 60.0 + initialSecond;
            double adv = quartzDiscreteSecond ? Mathf.Floor(elapsedSeconds) : elapsedSeconds;
            double total = (baseTotal + adv) % 86400.0;
            if (total < 0.0) total += 86400.0;

            hour24 = (int)(total / 3600.0) % 24;
            minute = (int)((total % 3600.0) / 60.0);
            second = (int)(total % 60.0);
        }

        public void ApplyHandsImmediate()
        {
            double baseTotal = initialHour24 * 3600.0 + initialMinute * 60.0 + initialSecond;
            double adv = isRunning
                ? (quartzDiscreteSecond ? Mathf.Floor(elapsedSeconds) : elapsedSeconds)
                : 0.0;

            double totalSeconds = baseTotal + adv;
            float sec = (float)(totalSeconds % 60.0);
            if (sec < 0f) sec += 60f;

            double totalMinutes = totalSeconds / 60.0;
            float min = (float)(totalMinutes % 60.0);
            if (min < 0f) min += 60f;

            double totalHours12 = (totalSeconds / 3600.0) % 12.0;
            if (totalHours12 < 0.0) totalHours12 += 12.0;

            float dirSign = dialFacesNegativeZ ? -1f : 1f;

            if (hourHandPivot != null)
            {
                float hDeg = dirSign * ((float)totalHours12 * 30f);
                hourHandPivot.localRotation = Quaternion.Euler(0f, 0f, hDeg);
            }

            if (minuteHandPivot != null)
            {
                float mDeg = dirSign * (min * 6f);
                minuteHandPivot.localRotation = Quaternion.Euler(0f, 0f, mDeg);
            }

            if (secondHandPivot != null)
            {
                float sDeg = dirSign * (sec * 6f);
                secondHandPivot.localRotation = Quaternion.Euler(0f, 0f, sDeg);
            }
        }
    }
}
