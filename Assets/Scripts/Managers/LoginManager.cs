using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Valley
{
    /// <summary>
    /// Handles Google Play Games authentication and Unity Authentication.
    ///
    /// Startup:
    ///     Google Play Games attempts automatic authentication.
    ///     If the player is already signed in, Unity Authentication is started automatically.
    ///
    /// Manual login:
    ///     If automatic authentication fails, the Login button can call
    ///     StartGooglePlayGamesLogin(), which manually starts the Google Play Games
    ///     sign-in flow.
    ///
    /// Successful flow:
    ///     Google Play Games
    ///         -> server-side authorization code
    ///         -> Unity Authentication
    ///         -> PlayerSignedIn
    /// </summary>
    [AddComponentMenu("Valley/Authentication/Login Manager")]
    public class LoginManager : MonoBehaviour
    {
        public static LoginManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool signInOnAwake = true;

        private bool m_UnityServicesInitialized;
        private bool m_GooglePlayGamesAuthenticating;
        private bool m_UnityAuthenticationInProgress;

        /// <summary>
        /// Fired when Unity Authentication successfully signs the player in.
        /// </summary>
        public static event Action PlayerSignedIn;

        /// <summary>
        /// Fired when any authentication step fails.
        /// </summary>
        public static event Action<string> SignInFailed;

        /// <summary>
        /// Returns true when the player is authenticated with Unity Authentication.
        /// Unity Authentication remains the source of truth for the game's login state.
        /// </summary>
        public bool IsSignedIn
        {
            get
            {
                return m_UnityServicesInitialized &&
                       AuthenticationService.Instance.IsSignedIn;
            }
        }

        public string DisplayName => GooglePlayGamesDisplayName;

#if UNITY_ANDROID

        /// <summary>
        /// Google Play Games display name.
        /// </summary>
        public string GooglePlayGamesDisplayName { get; private set; }

        /// <summary>
        /// Google Play Games user ID.
        /// </summary>
        public string GooglePlayGamesUserId { get; private set; }

#endif

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await InitializeUnityServicesAsync();

#if UNITY_ANDROID
            if (signInOnAwake)
            {
                InitializeGooglePlayGames();
            }
#endif
        }

        // ============================================================
        // UNITY SERVICES
        // ============================================================

        private async Task InitializeUnityServicesAsync()
        {
            if (m_UnityServicesInitialized)
                return;

            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    Log("Initializing Unity Services...");
                    await UnityServices.InitializeAsync();
                }

                m_UnityServicesInitialized = true;

                Log("Unity Services initialized.");
            }
            catch (Exception ex)
            {
                LogError($"Unity Services initialization failed: {ex.Message}");
                SignInFailed?.Invoke(ex.Message);
            }
        }

        // ============================================================
        // GOOGLE PLAY GAMES
        // ============================================================

#if UNITY_ANDROID

        /// <summary>
        /// Initializes Google Play Games and performs the automatic
        /// authentication attempt.
        ///
        /// IMPORTANT:
        /// This does NOT explicitly force the account chooser.
        /// It checks whether Google Play Games can automatically
        /// authenticate the player.
        /// </summary>
        private void InitializeGooglePlayGames()
        {
            if (!m_UnityServicesInitialized)
            {
                LogWarning("Unity Services are not initialized yet.");
                return;
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Log("Unity Authentication already signed in.");

                RefreshPlayerInformation();
                PlayerSignedIn?.Invoke();

                return;
            }

            PlayGamesPlatform.DebugLogEnabled = enableDebugLog;
            PlayGamesPlatform.Activate();

            Log("Starting automatic Google Play Games authentication...");

            PlayGamesPlatform.Instance.Authenticate(OnAutomaticAuthenticationFinished);
        }

        /// <summary>
        /// Called after the automatic Google Play Games authentication attempt.
        /// </summary>
        private void OnAutomaticAuthenticationFinished(SignInStatus status)
        {
            Log($"Automatic Google Play Games authentication result: {status}");

            if (status == SignInStatus.Success)
            {
                Log("Player is already signed into Google Play Games.");

                RefreshPlayerInformation();

                StartUnityAuthentication();

                return;
            }

            // Automatic authentication failed.
            // This is NOT treated as a fatal error because the player
            // may simply need to press the Login button.
            LogWarning(
                $"Automatic Google Play Games authentication did not succeed: {status}"
            );

            Log("Login button can now be used for manual sign-in.");
        }

        /// <summary>
        /// PUBLIC:
        /// Hook this method to your UI Login button.
        ///
        /// This performs a MANUAL Google Play Games authentication request,
        /// allowing Google Play Games to show its sign-in/account-selection UI.
        /// </summary>
        public void StartGooglePlayGamesLogin()
        {
            if (!m_UnityServicesInitialized)
            {
                LogWarning("Unity Services are not initialized yet.");
                return;
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Log("Player is already signed into Unity Authentication.");

                RefreshPlayerInformation();
                PlayerSignedIn?.Invoke();

                return;
            }

            if (m_GooglePlayGamesAuthenticating ||
                m_UnityAuthenticationInProgress)
            {
                LogWarning("Authentication is already in progress.");
                return;
            }

            PlayGamesPlatform.DebugLogEnabled = enableDebugLog;
            PlayGamesPlatform.Activate();

            m_GooglePlayGamesAuthenticating = true;

            Log("Starting manual Google Play Games authentication...");

            PlayGamesPlatform.Instance.ManuallyAuthenticate(
                OnManualAuthenticationFinished
            );
        }

        /// <summary>
        /// Called after the manual Google Play Games sign-in flow.
        /// </summary>
        private void OnManualAuthenticationFinished(SignInStatus status)
        {
            m_GooglePlayGamesAuthenticating = false;

            Log($"Manual Google Play Games authentication result: {status}");

            if (status != SignInStatus.Success)
            {
                LogWarning(
                    $"Google Play Games manual login failed: {status}"
                );

                SignInFailed?.Invoke(status.ToString());
                return;
            }

            Log("Manual Google Play Games login successful.");

            RefreshPlayerInformation();

            StartUnityAuthentication();
        }

        /// <summary>
        /// Gets the Google Play Games account information for UI.
        /// </summary>
        private void RefreshPlayerInformation()
        {
            if (!PlayGamesPlatform.Instance.IsAuthenticated())
            {
                LogWarning(
                    "Cannot refresh Google Play Games player information because " +
                    "Google Play Games is not authenticated."
                );

                return;
            }

            GooglePlayGamesDisplayName =
                PlayGamesPlatform.Instance.GetUserDisplayName();

            GooglePlayGamesUserId =
                PlayGamesPlatform.Instance.GetUserId();

            Log($"Google Play Games account: {GooglePlayGamesDisplayName}");
            Log($"Google Play Games ID: {GooglePlayGamesUserId}");
        }

        // ============================================================
        // UNITY AUTHENTICATION
        // ============================================================

        /// <summary>
        /// Starts the Unity Authentication sign-in using a fresh
        /// Google Play Games authorization code.
        /// </summary>
        private void StartUnityAuthentication()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Log("Unity Authentication is already signed in.");

                PlayerSignedIn?.Invoke();
                return;
            }

            if (m_UnityAuthenticationInProgress)
            {
                LogWarning("Unity Authentication is already in progress.");
                return;
            }

            if (!PlayGamesPlatform.Instance.IsAuthenticated())
            {
                LogWarning(
                    "Cannot start Unity Authentication because Google Play Games " +
                    "is not authenticated."
                );

                return;
            }

            Log("Requesting Google Play Games server-side authorization code...");

            PlayGamesPlatform.Instance.RequestServerSideAccess(
                true,
                OnServerSideAccessGranted
            );
        }

        /// <summary>
        /// Receives a fresh authorization code from Google Play Games.
        /// </summary>
        private void OnServerSideAccessGranted(string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                LogError("Google Play Games returned an empty authorization code.");

                SignInFailed?.Invoke("EmptyAuthorizationCode");
                return;
            }

            Log("Google Play Games authorization code received.");

            // IMPORTANT:
            // Do not save and reuse this code for future login attempts.
            _ = SignInWithGooglePlayGamesAsync(authCode);
        }

        /// <summary>
        /// Signs the player into Unity Authentication.
        ///
        /// Unity Authentication will either:
        ///     - sign into an existing Unity player linked to this
        ///       Google Play Games account
        ///     - or create a new Unity player
        /// </summary>
        private async Task SignInWithGooglePlayGamesAsync(string authCode)
        {
            if (m_UnityAuthenticationInProgress)
            {
                LogWarning("Unity Authentication request already in progress.");
                return;
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Log("Unity Authentication is already signed in.");

                PlayerSignedIn?.Invoke();
                return;
            }

            m_UnityAuthenticationInProgress = true;

            try
            {
                Log("Signing into Unity Authentication with Google Play Games...");

                await AuthenticationService.Instance
                    .SignInWithGooglePlayGamesAsync(authCode);

                string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();

                if (!string.IsNullOrEmpty(displayName))
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(displayName);
                }

                Log("Unity Authentication sign-in successful.");

                RefreshPlayerInformation();

                PlayerSignedIn?.Invoke();
            }
            catch (AuthenticationException ex)
            {
                LogError(
                    $"Unity Authentication failed. " +
                    $"ErrorCode: {ex.ErrorCode}, Message: {ex.Message}"
                );

                SignInFailed?.Invoke(ex.Message);
            }
            catch (RequestFailedException ex)
            {
                LogError(
                    $"Unity Services request failed. " +
                    $"ErrorCode: {ex.ErrorCode}, Message: {ex.Message}"
                );

                SignInFailed?.Invoke(ex.Message);
            }
            catch (Exception ex)
            {
                LogError($"Unexpected authentication error: {ex.Message}");

                SignInFailed?.Invoke(ex.Message);
            }
            finally
            {
                m_UnityAuthenticationInProgress = false;
            }
        }

#else

        /// <summary>
        /// Dummy implementation for non-Android platforms.
        /// </summary>
        public void StartGooglePlayGamesLogin()
        {
            LogWarning("Google Play Games login is only available on Android.");
        }

#endif

        // ============================================================
        // LOGGING
        // ============================================================

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[LoginManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[LoginManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[LoginManager] {message}");
        }
    }
}