using BumbleBeeWebApp.Services;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class AuthService
{
    private readonly FirestoreService _firestoreService;
    private readonly FirebaseAuth _firebaseAuth;

    public AuthService(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
        _firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    public bool ValidatePasswordStrength(string password)
    {
        // Password must be at least 8 characters long, with one uppercase, one lowercase, one number, and one special character
        var hasUppercase = new Regex(@"[A-Z]+");
        var hasLowercase = new Regex(@"[a-z]+");
        var hasDigit = new Regex(@"[0-9]+");
        var hasSpecialChar = new Regex(@"[\W]+");

        return password.Length >= 8 && hasUppercase.IsMatch(password) && hasLowercase.IsMatch(password) &&
               hasDigit.IsMatch(password) && hasSpecialChar.IsMatch(password);
    }
    
    public async Task<string> RegisterUserAsync(string email, string password, string type)
    {
        if (!ValidatePasswordStrength(password))
            throw new ArgumentException("Password does not meet strength requirements.");

        var userRecordArgs = new UserRecordArgs()
        {
            Email = email,
            Password = password,
        };
        UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

        var userInfo = new 
        {
            Email = email,
            Type = type,
            Uid = userRecord.Uid
        };

        //await _firestoreService.AddDocumentAsync("users", userInfo);  this creates duplicate records

        return userRecord.Uid;
    }

    public async Task<string> RegisterDonorAsync(string email, string password, string fullName, string idNumber, string taxNumber, string phoneNumber)
    {
        var userId = await RegisterUserAsync(email, password, "Donor");

        var donorInfo = new
        {
            Email = email,
            Type = "Donor",
            Uid = userId,
            FullName = fullName,
            IdNumber = idNumber,
            TaxNumber = taxNumber,
            PhoneNumber = phoneNumber
        };

        await _firestoreService.AddDocumentAsync("users", donorInfo);

        await SendEmailVerificationAsync(email, userId);

        return userId;
    }

    public async Task<string> RegisterAdministratorAsync(string email, string password, string fullName, string userRole)
    {
        var userId = await RegisterUserAsync(email, password, userRole);

        var adminInfor = new
        {
            Email = email,
            FullName = fullName,
            Type = userRole,
            Uid = userId
        };

        await _firestoreService.AddDocumentAsync("users", adminInfor);

        await SendEmailVerificationAsync(email, userId);

        return userId;
    }

    public async Task<string> RegisterCompanyAsync(string email, string password, string fullName, string description, string phoneNumber)
    {
        var userId = await RegisterUserAsync(email, password, "Company");

        var companyInfo = new
        {
            Email = email,
            Type = "Company",
            Uid = userId,
            FullName = fullName,
            JobRole = description,
            PhoneNumber = phoneNumber
        };

        await _firestoreService.AddDocumentAsync("users", companyInfo);

        await SendEmailVerificationAsync(email, userId);

        return userId;
    }

    public async Task SendEmailVerificationAsync(string email, string userId)
    {
        string verificationLink = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(email);
        await SendEmailAsync(email, verificationLink);
    }

    private async Task SendEmailAsync(string email, string verificationLink)
    {
        string subject = "BumbleBee Email Verification";
        string body = $"Please verify your email by clicking the following link: <a href='{verificationLink}'>Verify Email</a>";
        await Task.CompletedTask;
    }

    public async Task<string> LoginUserAsync(string email, string password)
    {
        var firebaseClient = new FirebaseRestService("AIzaSyCGItelKihS1fQC0C7Tj8v-5s0KoRc_IuM");

        try 
        {
            var response = await firebaseClient.LoginUserAsync(email, password);

            return response.LocalId;
        }
        catch(Exception ex) 
        {
            throw new Exception($"Login failed: {ex.Message}");
        }

        //var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

        //if (userRecord != null)
        //{
        //    return userRecord.Uid;
        //}

        //throw new Exception("Login failed. Invalid email or password.");
    }

    public async Task<DocumentSnapshot> GetUserDocumentAsync(string userId)
    {
        return await _firestoreService.GetUserDocumentAsync(userId);
    }

    public async Task<DocumentSnapshot> GetCompanyDocumentByUserIdAsync(string userId)
    {
        var companiesCollection = _firestoreService.GetCollectionAsync("companies");

        var companyDocuments = await companiesCollection;

        foreach (var companyDoc in companyDocuments.Documents)
        {
            var companyData = companyDoc.ConvertTo<Dictionary<string, object>>();

            if (companyData.TryGetValue("UID", out var uid) && uid.ToString() == userId)
            {
                return companyDoc;
            }
        }

        return null;
    }
    
    // Delete function for removing admin from firebase Auth
    public async Task DeleteUserFromAuthAsync(string userId)
    {
        await FirebaseAuth.DefaultInstance.DeleteUserAsync(userId);
    }

    //---   Old function that will not be in use later on (Still used for example in Home Controller)
    public async Task<string> CreateUserAsync(string email, string password)
    {
        var userRecordArgs = new UserRecordArgs()
        {
            Email = email,
            Password = password,
        };
        UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
        return userRecord.Uid;
    }

    //--- Currently not in use, could be usefull later on 
    public async Task<UserRecord> GetUserByIdAsync(string uid)
    {
        return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
    }

    public async Task<string> GeneratePasswordResetLinkAsync(string email)
    {
        try
        {
            // Generate the password reset link
            string resetLink = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(email);
            Console.WriteLine($"Generated password reset link: {resetLink}");
            return resetLink;
        }
        catch (FirebaseAuthException ex)
        {
            // Handle Firebase-specific exceptions
            Console.WriteLine($"FirebaseAuthException: {ex.Message}");
            throw new Exception($"Failed to generate password reset link: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Handle general exceptions
            Console.WriteLine($"Exception: {ex.Message}");
            throw new Exception($"An unexpected error occurred: {ex.Message}");
        }
    }

}
