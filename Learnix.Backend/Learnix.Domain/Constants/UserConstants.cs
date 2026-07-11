namespace Learnix.Domain.Constants;

public static class UserConstants
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int BioMaxLength = 500;
    public const int AvatarUrlMaxLength = 500;
    public const int GoogleIdMaxLength = 100;

    /// <summary>
    /// How long a deleted account is kept before it is considered gone for good. Deletion is soft
    /// (<c>ISoftDeletable</c>), so the row survives and an admin can bring the account back within this
    /// window — which is what the deletion email promises the user.
    /// </summary>
    public const int AccountRecoveryWindowDays = 30;
}
