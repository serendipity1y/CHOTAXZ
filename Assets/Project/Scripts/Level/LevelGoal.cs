using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level
{
    public class LevelGoal : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string nextSceneName = ""; // Placeholder for next level
        [SerializeField] private GameObject completionEffect;

        private bool _isCompleted;

        private void OnTriggerEnter(Collider other)
        {
            if (_isCompleted) return;

            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController>() != null)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            _isCompleted = true;
            Debug.Log("<color=green>Level Completed!</color>");

            if (completionEffect != null)
            {
                Instantiate(completionEffect, transform.position, Quaternion.identity);
            }

            // In a real project, we would load the next scene
            // For now, we log it and provide a placeholder for the user
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log("Next scene name not set. Staying in current scene.");
            }
        }
    }
}
