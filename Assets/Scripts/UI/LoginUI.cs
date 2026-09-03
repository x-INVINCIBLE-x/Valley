using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valley
{
    public class LoginUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LoginManager loginManager;
        [SerializeField] private TMP_Text accountNameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject accountPanel;

        [Header("Login State GameObjects")]
        [Tooltip("Enabled when the player successfully signs in.")]
        [SerializeField] private GameObject signedInObject;

        [Tooltip("Disabled when the player successfully signs in.")]
        [SerializeField] private GameObject signedOutObject;

        [Header("Settings")]
        [SerializeField] private string signedOutText = "Sign in with Google Play Games";
        [SerializeField] private string signingInText = "Signing in...";
        [SerializeField] private string signInFailedText = "Sign in failed";

        [SerializeField] private Button loginButton;

        private void Awake()
        {
            loginManager = LoginManager.Instance;
            Debug.Log("[LoginUI] Awake");
            if (loginButton == null)
            {
                Debug.LogError(
                    "[LoginUI] No Button found in children. " +
                    "Make sure the login button is under LoginUI."
                );
            }
            else
            {
                // Remove first in case this component gets enabled/disabled
                // multiple times.
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            }
        }

        private void OnEnable()
        {
            LoginManager.PlayerSignedIn += OnPlayerSignedIn;
            LoginManager.SignInFailed += OnSignInFailed;

            // Handle case where LoginManager was already signed in.
            RefreshLoginState();
        }

        private void OnDisable()
        {
            LoginManager.PlayerSignedIn -= OnPlayerSignedIn;
            LoginManager.SignInFailed -= OnSignInFailed;
        }

        private void OnDestroy()
        {
            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        }

        private void RefreshLoginState()
        {
            if (loginManager == null)
            {
                loginManager = LoginManager.Instance;
            }

            if (loginManager == null)
            {
                Debug.LogError("[LoginUI] LoginManager reference is missing.");
                SetSignedOutState();
                return;
            }

            if (loginManager.IsSignedIn)
            {
                OnPlayerSignedIn();
            }
            else
            {
                SetSignedOutState();
            }
        }

        private void OnLoginButtonClicked()
        {
            if (loginManager == null)
            {
                loginManager = LoginManager.Instance;
            }

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
                statusText.text = "Connected";

            if (loginButton != null)
                loginButton.gameObject.SetActive(false);

            if (accountPanel != null)
                accountPanel.SetActive(true);

            if (signedInObject != null)
                signedInObject.SetActive(true);

            if (signedOutObject != null)
                signedOutObject.SetActive(false);

            Debug.Log("[LoginUI] Successfully signed in.");
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

            if (signedInObject != null)
                signedInObject.SetActive(false);

            if (signedOutObject != null)
                signedOutObject.SetActive(true);
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

            if (signedInObject != null)
                signedInObject.SetActive(false);

            if (signedOutObject != null)
                signedOutObject.SetActive(true);
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