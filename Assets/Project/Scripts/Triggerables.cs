using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Assets.Project.Scripts
{
	public class Triggerables: MonoBehaviour
	{
		[SerializeField] private UnityEvent onTrigger;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
			{
				onTrigger.Invoke();
            }
        }

        // Use this for initialization
        void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}