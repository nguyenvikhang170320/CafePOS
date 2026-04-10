using CloudinaryDotNet;

using CloudinaryDotNet.Actions;

using Microsoft.Extensions.Options;



public class PhotoService

{

    private readonly Cloudinary _cloudinary;



    public PhotoService(IConfiguration config)

    {

        var acc = new Account(
         config["CloudinarySettings:CloudName"], // Lấy giá trị của CloudName
         config["CloudinarySettings:ApiKey"],    // Lấy giá trị của ApiKey
         config["CloudinarySettings:ApiSecret"]  // Lấy giá trị của ApiSecret
     );

        _cloudinary = new Cloudinary(acc);

    }



    public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file, string folderName)
    {
        var uploadResult = new ImageUploadResult();
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // Khang thêm dòng này nè:
                Folder = folderName,
                Transformation = new Transformation().Height(500).Width(500).Crop("fill")
            };
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }
        return uploadResult;
    }

}