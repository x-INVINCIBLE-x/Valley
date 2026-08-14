public static class UserOnboardingData
{
    public static string PlayerName { get; private set; }
    public static int Age { get; private set; }
    public static string Gender { get; private set; }
    public static bool IsDataSet { get; private set; }

    /// <summary>Called by OnboardingUIController when the user hits Submit.</summary>
    public static void SetData(string playerName, int age, string gender)
    {
        PlayerName = playerName;
        Age = age;
        Gender = gender;
        IsDataSet = true;
    }

    public static void Clear()
    {
        PlayerName = null;
        Age = 0;
        Gender = null;
        IsDataSet = false;
    }
}