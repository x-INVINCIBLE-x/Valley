using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valley
{
    public class LoginUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LoginManager loginManager;
        [SerializeField] private Button loginButton;
        [SerializeField] private TMP_Text accountNameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject accountPanel;

        [Header("Settings")]
        [SerializeField] private string signedOutText = "Sign in with Google Play Games";
        [SerializeField] private string signingInText = "Signing in...";
        [SerializeField] private string signInFailedText = "Sign in failed";

        private void OnEnable()
        {
            LoginManager.PlayerSignedIn += OnPlayerSignedIn;
            LoginManager.SignInFailed += OnSignInFailed;
        }

        private void Start()
        {
            SetSignedOutState();
        }

        private void OnDisable()
        {
            LoginManager.PlayerSignedIn -= OnPlayerSignedIn;
            LoginManager.SignInFailed -= OnSignInFailed;
        }

        public void OnLoginButtonClicked()
        {
            if (loginManager == null)
            {
                Debug.LogError("[LoginUI] LoginManager reference is missing.");
                return;
            }

            SetSigningInState();
            loginManager.StartGooglePlayGamesLogin();
        }

        private void OnPlayerSignedIn()
        {
            if (loginManager == null)
                return;

            string displayName = loginManager.GooglePlayGamesDisplayName;

            if (string.IsNullOrEmpty(displayName))
                displayName = "Google Play Games";

            if (accountNameText != null)
                accountNameText.text = displayName;

            if (statusText != null)
                statusText.text = $"Signed in as {displayName}";

            if (loginButton != null)
                loginButton.gameObject.SetActive(false);

            if (accountPanel != null)
                accountPanel.SetActive(true);
        }

        private void OnSignInFailed(string error)
        {
            if (loginButton != null)
            {
                loginButton.gameObject.SetActive(true);
                loginButton.interactable = true;
            }

            if (statusText != null)
                statusText.text = signInFailedText;

            Debug.LogWarning($"[LoginUI] Sign in failed: {error}");
        }

        private void SetSignedOutState()
        {
            if (loginButton != null)
            {
                loginButton.gameObject.SetActive(true);
                loginButton.interactable = true;
            }

            if (accountPanel != null)
                accountPanel.SetActive(false);

            if (accountNameText != null)
                accountNameText.text = string.Empty;

            if (statusText != null)
                statusText.text = signedOutText;
        }

        private void SetSigningInState()
        {
            if (loginButton != null)
                loginButton.interactable = false;

            if (statusText != null)
                statusText.text = signingInText;
        }
    }
}