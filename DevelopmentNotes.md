# Development Documentation 

This document explains the functionality of each part of the Bumble Bee Project, focusing on the authentication processes, data validation, and error handling.

## 1. Login Process

The login process authenticates users by verifying their credentials with Firebase Authentication and retrieving user information from Firestore. Here’s how it works:

### Steps:
1. **AccountController**:
   - The `Login` method is invoked when the user submits their login credentials.
   - It calls `LoginUserAsync` in `AuthService`, which uses Firebase Authentication to validate the credentials.
   - If successful, the user’s type is retrieved from Firestore, and the controller redirects the user to the appropriate success page based on their account type.

2. **AuthService**:
   - The `LoginUserAsync` method checks the user’s credentials using Firebase Authentication.
   - If authentication is successful, it returns the user’s UID; otherwise, it throws an exception which is caught by the controller to display an error message.

3. **FirestoreService**:
   - The `GetUserDocumentAsync` method is used to fetch the user’s document from Firestore, which contains information about the user type and other details.

### Code Involved:
- **`AccountController.cs`**: Handles `Login` action.
- **`AuthService.cs`**: Implements `LoginUserAsync` for credential verification.
- **`FirestoreService.cs`**: Retrieves user data from Firestore.

## 2. Registration Process

The registration process involves creating a new user in Firebase Authentication and saving user details in Firestore. The application supports three account types: **User**, **Donor**, and **Company**, each with distinct data validation requirements.

### Account Types:
- **User**
- **Donor**: Requires additional fields like `FullName`, `IdNumber`, `TaxNumber`, and `PhoneNumber`.
- **Company**: Requires additional fields like `CompanyName`, `ReferenceNumber`, `TaxNumber`, `Description`, and `PhoneNumber`.

### Steps:
1. **AccountController**:
   - Each registration type has a corresponding action (`Register`, `RegisterDonor`, or `RegisterCompany`) that validates fields and checks if `password` matches `confirmPassword`.
   - On validation failure, the controller sets `ViewBag.ErrorMessage` with an appropriate message and returns the user to the registration form to correct the inputs.

2. **AuthService**:
   - Implements methods for each account type registration (`RegisterUserAsync`, `RegisterDonorAsync`, `RegisterCompanyAsync`), where it validates password strength and interacts with Firebase Authentication.
   - If registration succeeds, user information is saved in Firestore.

3. **FirestoreService**:
   - The `AddDocumentAsync` method is used to save the user’s data to the `"users"` collection, categorizing by account type.

### Data Validation:
- **Email Validation**: Ensures correct email format.
- **Password Strength**: Enforces requirements for length, uppercase, lowercase, number, and special character.
- **Confirm Password**: Checks that `password` matches `confirmPassword` before proceeding with registration.
- **ViewBag.ErrorMessage Display**: Each registration view (i.e., `RegisterDonor.cshtml` and `RegisterCompany.cshtml`) includes an alert section to display error messages if validation fails. This enhances the user experience by providing specific feedback on what needs correction.

### Code Involved:
- **`AccountController.cs`**:
  - `Register`, `RegisterDonor`, and `RegisterCompany` actions handle registration logic, field validation, and error handling.
  - `ViewBag.ErrorMessage` is set if any issues arise, which is then displayed in the views.
- **`AuthService.cs`**:
  - Manages password validation and handles the registration process for different account types.
- **`FirestoreService.cs`**:
  - Stores validated user data into Firestore.

## 3. Error Handling with ViewBag.ErrorMessage

To provide feedback on validation errors or login/registration failures, the project uses `ViewBag.ErrorMessage`. This temporary data container sends messages from the controller to the view, allowing for customized user feedback directly within the page.

### Implementation in Views:
- **Error Display in `RegisterDonor` and `RegisterCompany`**:
  - In `RegisterDonor.cshtml` and `RegisterCompany.cshtml`, the views include an alert section that checks for `ViewBag.ErrorMessage`.
  - If `ViewBag.ErrorMessage` contains a value, it is displayed at the top of the form, ensuring users can see specific error messages when data validation fails.

### Example Usage in View:
```html
@if (!string.IsNullOrEmpty(ViewBag.ErrorMessage))
{
    <div class="alert alert-danger">
        @ViewBag.ErrorMessage
    </div>
}
