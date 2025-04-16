# ImageManagement.API
The **ImageManagement API** is a robust solution for managing image uploads, processing, and retrieval. It supports storing images, extracting metadata (Exif data), resizing images, and serving them efficiently in file system.

## Features
- **Image Upload**: Upload and store images securely with different sizes (phone,tablet,desktop).
- **Metadata Extraction**: Extract and store Exif data from images.
- **Image Resizing**: Retrieve resized versions of images for different device sizes.
- **Retreive Image Metadat**: Retreive extracted Images info.
- **Error Handling**: Comprehensive error responses for invalid inputs or server issues.
- **Multi-environment configuration**: (Dev/Stage/Production).
- **Support for multiple image formats**: (JPG, PNG, WebP).
- **JWT authentication**

## Technologies
- **.NET 8**
- **C# 12**
- **ASP.NET Core**
- **MSTest** for unit testing
- **Dependency Injection** for modularity and testability.
- **SixLabors** for image processing functionality.
- **User Secrets** for Dev credintial saving.

## Installation
1. .NET 8 SDK.
2.  Visual Studio 2022.
3.  Clone the repository: https://github.com/HamidaElsheimy74/ImageManagement.API.
4.  Navigate to the project directory.
5.  Restore dependencies.
6.  Build the project.


## APIs Description
Endpoint	                Method	  Description
/api/images      	        POST	    Upload new image
/api/images/{id}	        GET	      Get processed image
/api/images/{id}/info	    GET	      Get image metadata
/auth/login	              POST	    Get JWT token

## Configuration
1-Set launch profile to Development & use these credintial to login & get JWT token to be able to use the apis.
{
		"UserName": "dev-User@test.com",
		"Password": "dev@Password123"
	}
-**Note 1**: each Env has a different Credintials that will be found inside each appSettings.ChosenEnv.json.
-**Note 2**: Development JWT Secres  & user Credintials are stored at user secrets that you can find as following: 
          *Windows: %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json.
          *macOS/Linux: ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json.
 

2-Press F5 to start debugging & run the code.

 Access the API at `https://localhost:7012/` (use `https://localhost:7012/swagger/index.html` to get swagger doc for the apis).

### API Endpoints
- **POST /api/images/upload**: Upload images.
- **GET /api/images/{imageId}/info**: Retrieve image metadata.
- **GET /api/images/{imageId}/resized/{size}**: Retrieve resized images.
-**POST /auth/login** :    Get JWT token

  
## Testing
Run the unit tests:
   
   
