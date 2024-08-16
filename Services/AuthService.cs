using FirebaseAdmin.Auth;


    public class AuthService
    {
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

        public async Task<UserRecord> GetUserByIdAsync(string uid)
        {
            return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
        }

        //--- Additional methods for authentication to be added later.
    }
