using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class AuthService
{
    private readonly FirestoreService _firestoreService;

    public AuthService(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
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

        await _firestoreService.AddDocumentAsync("users", userInfo);

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

        return userId;
    }

    public async Task<string> RegisterCompanyAsync(string email, string password, string companyName, string referenceNumber, string taxNumber, string description, string phoneNumber)
    {
        var userId = await RegisterUserAsync(email, password, "Company");
        

        var companyInfo = new 
        {
            Email = email,
            Type = "Company",
            Uid = userId,
            CompanyName = companyName,
            ReferenceNumber = referenceNumber,
            TaxNumber = taxNumber,
            Description = description,
            PhoneNumber = phoneNumber
        };

        await _firestoreService.AddDocumentAsync("users", companyInfo);

        return userId;
    }

    public async Task<string> LoginUserAsync(string email, string password)
    {
        var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

        if (userRecord != null)
        {
            return userRecord.Uid;
        }

        throw new Exception("Login failed. Invalid email or password.");
    }

    public async Task<DocumentSnapshot> GetUserDocumentAsync(string userId)
    {
        return await _firestoreService.GetUserDocumentAsync(userId);
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

    //--- Additional methods for authentication to be added later.
}
