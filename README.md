**Bumble Bee Foundation**
Bumble Bee Foundation is a web application built with ASP.NET Core and integrated with Firebase for backend services. This project is set up with a focus on secure handling of secrets and configuration for both development and production environments.

**Project Overview**
The Bumble Bee Foundation project leverages Firebase for the following services:

**Authentication:** Secure user authentication.
**Firestore:** Real-time NoSQL database.
**Storage:** Cloud storage for storing files.
**Functions:** Serverless functions (Not yet implemented due to associated costs).
**Running the Project**
You can run the project in both development and production environments by setting the ASPNETCORE_ENVIRONMENT variable.

**Running in Development**
To run the project in the development environment:
bash
Copy code
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

**Running in Production**
To run the project in the production environment:

bash
Copy code
set ASPNETCORE_ENVIRONMENT=Production
dotnet run

**Pushing to Firebase Hosting**
Note: Deployment to Firebase Hosting should only be done once the project is complete.

**Steps to Deploy**
Install Firebase CLI (if not already installed):

bash
Copy code
npm install -g firebase-tools

**Login to Firebase:**

bash
Copy code
firebase login

**Build the Project:**

bash
Copy code
dotnet publish -c Release -o ./wwwroot

**Deploy to Firebase Hosting:**

bash
Copy code
firebase deploy

**Access the Deployed Application:**

After deployment, the Firebase CLI will provide a URL where your application is hosted.

**File Locations**
Project Structure
/Controllers: Contains the application’s controllers, managing the flow between the user interface and backend services.
/Models: Holds the data models used throughout the application.
/Views: Contains the Razor views used for rendering the user interface.
/Services: This folder contains all the Firebase services:
AuthService.cs: Manages user authentication with Firebase.
FirestoreService.cs: Handles interactions with Firebase Firestore (database).
StorageService.cs: Manages file storage using Firebase Storage.
FunctionsService.cs: (Not yet implemented) Will handle serverless functions using Firebase Functions.
**Secrets Management**
/Secrets/: This folder contains the Firebase service account keys. Ensure that these keys are not exposed in any public repository.
The Firebase service account key should be named bumble-bee-foundation-firebase-adminsdk.json and placed in this folder.
Generating a Firebase Key and Connecting Firebase
To generate a Firebase service account key and connect it to your project:

**Generate Firebase Service Account Key:**

Go to your Firebase project settings.
Navigate to the Service Accounts tab.
Click on Generate new private key.
Download the JSON key file.

**Store the Key Securely:**

Rename the downloaded JSON file to bumble-bee-foundation-firebase-adminsdk.json.
Move the file to the /Secrets folder within your project directory.
Firebase Project Sharing:

The Firebase project details will be shared with the group shortly, enabling collaborative development.
Notes
Environment Configuration: Ensure the environment is correctly configured for your deployment scenario (development or production).
**Firebase Functions:** Firebase Functions have not been implemented yet due to the costs involved. Future implementation may include this feature as needed.
