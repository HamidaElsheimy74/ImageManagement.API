namespace ImageManagement.Common.Errors;
public static class ErrorsHandler
{

    public static string Image_Not_Found = "The requested image was not found.";
    public static string Empty_Files_List = "No files uploaded";
    public static string Empty_image_ID = "No imageID provided";
    public static string Empty_image_Size = "No image size provided";
    public static string Image_Processing_Failed = "An error occurred while processing the image.";
    public static string Image_Storage_Failed = "An error occurred while storing the image.";
    public static string Internal_Server_Error = "An error occurred while processing your request.";
    public static string Not_Found = " resource not found.";
    public static string Invalid_File_Type = "Invalid file type. Only JPG, PNG, and WebP are allowed.";
    public static string File_Too_Large = "File too large. Maximum size is 2MB.";
    public static string Invalid_Size = "Invalid size parameter. Must be 'phone', 'tablet', 'desktop', or 'original'";
    public static string Invalid_Credentials = "Invalid credentials";
    public static string Invalid_Username = "Invalid username";
    public static string Invalid_Password = "Invalid password";
    public static string Invalid_LoginModel = "Invalid login model";
    public static string Invalid_UploadModel = "Invalid upload model";

}
