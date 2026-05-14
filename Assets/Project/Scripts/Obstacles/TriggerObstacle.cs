using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Assets.Project.Scripts.Obstacles
{
	public class TriggerObstacle: MonoBehaviour
	{
		
		public void DissableObject() { 
		
			this.gameObject.SetActive(false);
        }

		public void EnableObject() {
		
			this.gameObject.SetActive(true);
        }


		public void eblan() 
		{
		Debug.Log("eblan");
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