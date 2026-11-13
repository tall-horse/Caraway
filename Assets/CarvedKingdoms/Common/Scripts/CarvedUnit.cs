using UnityEngine;

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Plays the clips of a Carved Kingdoms unit. Each unit has Idle, March, Attack, Hit and eight Death clips, such as
    /// Death_Front and Death_FrontLeft. Workers also have Work, and animals have Graze.
    /// Idle, March, Work and Graze loop. After Attack or Hit the unit goes back to Idle.
    /// To play a clip from code, call <c>unit.GetComponent&lt;CarvedUnit&gt;().Play("Attack")</c>.
    /// </summary>
    public class CarvedUnit : MonoBehaviour
    {
        [Tooltip("The clip it starts with")]
        public string startClip = "Idle";
        [Tooltip("Start each unit at another moment of its Idle, so a company does not move as one")]
        public bool randomStart = true;
        [Tooltip("Plays the start clip again this many seconds after it ends. 0 plays it once. The Overview scenes show each clip this way.")]
        public float repeat = 0f;

        Animator _animator;
        float _since;

        void Awake()
        {
            // The Animator sits on the model, one level below the prefab root.
            _animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(startClip) && Has(startClip))
                _animator.Play(startClip, 0, randomStart ? Random.value : 0f);
        }

        void Update()
        {
            if (repeat <= 0f || _animator == null || string.IsNullOrEmpty(startClip)) return;
            // An Attack goes back to Idle by itself, and a fall stays down. Either way, the clip is over.
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(startClip) && (state.loop || state.normalizedTime < 1f)) { _since = 0f; return; }
            _since += Time.deltaTime;
            if (_since < repeat) return;
            _since = 0f;
            _animator.Play(startClip, 0, 0f);
        }

        /// <summary>Plays a clip by name, such as Idle, March, Attack, Hit, Work, Graze or Death_Front. It blends in
        /// briefly.</summary>
        public void Play(string clip, float blend = 0.08f)
        {
            if (_animator != null && Has(clip)) _animator.CrossFadeInFixedTime(clip, blend, 0);
        }

        public bool Has(string clip)
        {
            return _animator != null && _animator.HasState(0, Animator.StringToHash(clip));
        }
    }
}
