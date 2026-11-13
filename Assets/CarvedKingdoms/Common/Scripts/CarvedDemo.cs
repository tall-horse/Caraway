using UnityEngine;

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Runs the armies in the demo scenes. Each child of this object is one company. Every few seconds one company
    /// attacks and the next one is hit.
    /// </summary>
    public class CarvedDemo : MonoBehaviour
    {
        public float every = 2.5f;
        float _clock;

        void Update()
        {
            _clock += Time.deltaTime;
            if (_clock < every || transform.childCount == 0) return;
            _clock = 0f;
            int k = Random.Range(0, transform.childCount);
            foreach (var u in transform.GetChild(k).GetComponentsInChildren<CarvedUnit>()) u.Play("Attack");
            foreach (var u in transform.GetChild((k + 1) % transform.childCount).GetComponentsInChildren<CarvedUnit>()) u.Play("Hit");
        }
    }
}
